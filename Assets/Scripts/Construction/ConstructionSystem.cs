using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ConstructionSystem : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float rayDistance = 100f;

    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;

    [SerializeField] Transform placementPreview;

    [Header("Building Selection")]
    [SerializeField] private BuildingDefinition basicBuildingDefinition;

    [Header("Placement")]
    [SerializeField] private Transform placedBuildingsParent;

    [Header("Preview Validity")]
    [SerializeField] private Renderer placementPreviewRenderer;
    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;

    [Header("Removal Preview")]
    [SerializeField] private Transform removalPreview;

    private readonly Dictionary<Vector2Int, GameObject> placedBuildingsByCell = new Dictionary<Vector2Int, GameObject>();

    private BuildingDefinition selectedBuilding;
    private ConstructionInputActions constructionInputActions;

    private Vector3 currentSnapPosition;
    private Vector3 currentHitPoint;
    private Vector2Int currentCell;
    private bool hasHit;
    private bool isRemoveMode;

    

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (placementPreview == null && transform.parent != null)
        {
            placementPreview = transform.parent.Find("PlacementPreview");
        }

        if (placementPreview != null)
        {
            placementPreview.gameObject.SetActive(false);
        }

        if (removalPreview != null)
        {
            removalPreview.gameObject.SetActive(false);
        }

        constructionInputActions = new ConstructionInputActions();
    }

    private void Update()
    {
        UpdateMouseWorldPosition();  
    }

    private void OnEnable()
    {
        if (constructionInputActions == null)
            return;

        constructionInputActions.Construction.SelectBasicBuilding.performed += OnSelectBasicBuilding;
        constructionInputActions.Construction.CancelConstruction.performed += OnCancelConstruction;
        constructionInputActions.Construction.PlaceBuilding.performed += OnPlaceBuilding;
        constructionInputActions.Construction.ToggleRemoveMode.performed += OnToggleRemoveMode;

        constructionInputActions.Construction.Enable();
    }

    private void OnDisable()
    {
        if (constructionInputActions == null)
            return;

        constructionInputActions.Construction.SelectBasicBuilding.performed -= OnSelectBasicBuilding;
        constructionInputActions.Construction.CancelConstruction.performed -= OnCancelConstruction;
        constructionInputActions.Construction.PlaceBuilding.performed -= OnPlaceBuilding;
        constructionInputActions.Construction.ToggleRemoveMode.performed -= OnToggleRemoveMode;

        constructionInputActions.Construction.Disable();
    }

    private void OnSelectBasicBuilding(InputAction.CallbackContext context)
    {
        SelectBuilding(basicBuildingDefinition);
    }

    private void OnCancelConstruction(InputAction.CallbackContext context)
    {
        CancelBuildingSelection();
    }

    private void OnPlaceBuilding(InputAction.CallbackContext context)
    {
        if (isRemoveMode)
        {
            TryRemoveCurrentBuilding();
            return;
        }

        TryPlaceCurrentBuilding();
    }

    private void TryRemoveCurrentBuilding()
    {
        if (!hasHit)
            return;

        if (!placedBuildingsByCell.TryGetValue(currentCell, out GameObject building))
            return;

        placedBuildingsByCell.Remove(currentCell);

        if (building != null)
        {
            Destroy(building);
        }

        UpdateRemovalPreviewVisibility();
        UpdatePreviewValidityVisual();
    }

    private void OnToggleRemoveMode(InputAction.CallbackContext context)
    {
        EnterRemoveMode();
    }

    private void EnterRemoveMode()
    {
        isRemoveMode = true;
        selectedBuilding = null;
        HidePreview();
        UpdateRemovalPreviewVisibility();
    }

    private void SelectBuilding(BuildingDefinition definition)
    {
        if (definition == null)
            return;

        isRemoveMode = false;
        selectedBuilding = definition;

        HideRemovalPreview();
        UpdatePreviewVisibility();
    }
    

    private void CancelBuildingSelection()
    {
        isRemoveMode = false;
        selectedBuilding = null;

        UpdatePreviewVisibility();
        HideRemovalPreview();
    }

    private void TryPlaceCurrentBuilding()
    {
        if (!CanPlaceAtCurrentCell())
            return;

        GameObject placedBuilding = Instantiate(
            selectedBuilding.BuildingPrefab,
            currentSnapPosition,
            Quaternion.identity,
            placedBuildingsParent
        );

        placedBuildingsByCell.Add(currentCell, placedBuilding);

        UpdatePreviewValidityVisual();
    }

    private void UpdatePreviewVisibility()
    {
        if (!hasHit || selectedBuilding == null)
        {
            HidePreview();
            return;
        }

        UpdatePreviewValidityVisual();
        ShowPreview();
    }

    private void UpdateMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            hasHit = false;
            HidePreview();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        hasHit = Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask);

        if (hasHit)
        {
            currentHitPoint = hit.point;
            currentCell = WorldToCell(currentHitPoint);
            currentSnapPosition = CellToWorld(currentCell);      
        }

        UpdatePreviewVisibility();
        UpdateRemovalPreviewVisibility();
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        float x = cell.x * cellSize;
        float z = cell.y * cellSize;

        return new Vector3(x, 0f, z);
    }

    private Vector2Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPosition.x / cellSize), Mathf.RoundToInt(worldPosition.z / cellSize));
    }

    private void ShowPreview()
    {
        if (placementPreview == null)
            return;

        placementPreview.position = currentSnapPosition;
        placementPreview.gameObject.SetActive(true);
    }

    private void HidePreview()
    {
        if (placementPreview == null)
            return;

        placementPreview.gameObject.SetActive(false);
    }

    private void UpdatePreviewValidityVisual()
    {
        if (placementPreviewRenderer == null)
            return;

        bool canPlace = CanPlaceAtCurrentCell();

        if (canPlace && validPreviewMaterial != null)
        {
            placementPreviewRenderer.sharedMaterial = validPreviewMaterial;
            return;
        }

        if (!canPlace && invalidPreviewMaterial != null)
        {
            placementPreviewRenderer.sharedMaterial = invalidPreviewMaterial;
        }
    }

    private bool CanPlaceAtCurrentCell()
    {
        if (isRemoveMode)
            return false;

        if (selectedBuilding == null)
            return false;

        if (!hasHit)
            return false;

        if (selectedBuilding.BuildingPrefab == null)
            return false;

        if (placedBuildingsByCell.ContainsKey(currentCell))
            return false;

        return true;
    }

    private void ShowRemovalPreview()
    {
        if (removalPreview == null)
            return;

        removalPreview.position = currentSnapPosition;
        removalPreview.gameObject.SetActive(true);
    }

    private void HideRemovalPreview()
    {
        if (removalPreview == null)
            return;

        removalPreview.gameObject.SetActive(false);
    }

    private void UpdateRemovalPreviewVisibility()
    {
        if (!isRemoveMode || !hasHit)
        {
            HideRemovalPreview();
            return;
        }

        if (!placedBuildingsByCell.ContainsKey(currentCell))
        {
            HideRemovalPreview();
            return;
        }

        ShowRemovalPreview();
    }

    private void OnDrawGizmos()
    {
        if (!hasHit)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentHitPoint, 0.15f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentSnapPosition, 0.15f);

        Gizmos.color = Color.blue;
        Vector3 gizmoCenter = currentSnapPosition + Vector3.up * 0.02f;

        Gizmos.DrawWireCube(
            gizmoCenter,
            new Vector3(cellSize, 0.02f, cellSize)
        );
    }

}
