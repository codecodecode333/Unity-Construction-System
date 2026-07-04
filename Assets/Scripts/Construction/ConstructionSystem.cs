using UnityEngine;

public class ConstructionSystem : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float rayDistance = 100f;

    [Header("Grid")]
    [SerializeField] private float cellSize = 1f;

    private Vector3 currentSnapPosition;
    private Vector3 currentHitPoint;
    private Vector2Int currentCell;
    private bool hasHit;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        Debug.Log(mainCamera);
    }

    private void Update()
    {
        UpdateMouseWorldPosition();
    }

    private void UpdateMouseWorldPosition()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        hasHit = Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask);

        if (hasHit)
        {
            currentHitPoint = hit.point;
            currentCell = WorldToCell(currentHitPoint);
            currentSnapPosition = CellToWorld(currentCell);

            Debug.Log($"Hit : {currentHitPoint}");
            Debug.Log($"Cell : {currentCell}");
            Debug.Log($"Snap : {currentSnapPosition}");
        }
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

    private void OnDrawGizmos()
    {
        if (!hasHit)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentHitPoint, 0.15f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentSnapPosition, 0.15f);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(currentSnapPosition, new Vector3(cellSize, 0.1f, cellSize));
    }
}