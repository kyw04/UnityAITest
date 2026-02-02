using UnityEngine;

[CreateAssetMenu(menuName = "RoomDecor/Decor Item Definition")]
public class DecorItemDefinition : ScriptableObject
{
    public string itemId = "item";
    public DecorCategory category = DecorCategory.Prop;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Placement")]
    public SocketType[] allowedSocketTypes = { SocketType.Floor };
    public bool allowRotateY = true;
}
