using UnityEngine;

[CreateAssetMenu(fileName = "Decoration", menuName = "ScriptableObjects/Decoration")]
public class Decoration : ScriptableObject
{
    public string id;
    public GameObject prefab;
    public FieldType placeFieldType;
}
