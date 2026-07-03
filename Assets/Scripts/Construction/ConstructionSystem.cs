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
            currentSnapPosition = SnapToGrid(currentHitPoint);

            Debug.Log($"Hit : {currentHitPoint}");
            Debug.Log($"Snap : {currentSnapPosition}");
        }
    }

    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        float x = Mathf.Round(worldPosition.x / cellSize) * cellSize;
        float z = Mathf.Round(worldPosition.z / cellSize) * cellSize;

        return new Vector3(x, 0f, z);
    }

    private void OnDrawGizmos()
    {
        if (!hasHit)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentHitPoint, 0.15f);
    }
}