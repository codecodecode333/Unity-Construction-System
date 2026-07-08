using UnityEngine;

[CreateAssetMenu(
    menuName = "Construction/Building Definition",
    fileName = "BD_NewBuilding")]

public class BuildingDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject buildingPrefab;

    public GameObject BuildingPrefab => buildingPrefab;
}
