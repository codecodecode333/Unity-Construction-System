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

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    private BuildingDefinition selectedBuilding;
    private ConstructionInputActions constructionInputActions;

    private Vector3 currentSnapPosition;
    private Vector3 currentHitPoint;
    private Vector2Int currentCell;
    private bool hasHit;

    

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

        constructionInputActions.Construction.Enable();
    }

    private void OnDisable()
    {
        if (constructionInputActions == null)
            return;

        constructionInputActions.Construction.SelectBasicBuilding.performed -= OnSelectBasicBuilding;
        constructionInputActions.Construction.CancelConstruction.performed -= OnCancelConstruction;
        constructionInputActions.Construction.PlaceBuilding.performed -= OnPlaceBuilding;

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
        TryPlaceCurrentBuilding();
    }

    private void SelectBuilding(BuildingDefinition definition)
    {
        if (definition == null)
            return;

        selectedBuilding = definition;
        UpdatePreviewVisibility();
    }

    private void CancelBuildingSelection()
    {
        selectedBuilding = null;
        UpdatePreviewVisibility();
    }

    private void TryPlaceCurrentBuilding()
    {
        if (selectedBuilding == null)
            return;

        if (!hasHit)
            return;

        if (selectedBuilding.BuildingPrefab == null)
            return;

        if (occupiedCells.Contains(currentCell))
            return;

        Instantiate(
            selectedBuilding.BuildingPrefab,
            currentSnapPosition,
            Quaternion.identity,
            placedBuildingsParent
        );

        occupiedCells.Add(currentCell);
    }

    private void UpdatePreviewVisibility()
    {
        if (!hasHit || selectedBuilding == null)
        {
            HidePreview();
            return;
        }

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
