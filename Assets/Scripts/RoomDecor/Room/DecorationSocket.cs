using UnityEngine;

public class DecorationSocket : MonoBehaviour
{
    public SocketType socketType = SocketType.Floor;

    [Tooltip("문 앞/통로 등, 막으면 안 되는 자리면 체크")]
    public bool isCorridorCritical = false;

    [HideInInspector] public bool occupied;
    [HideInInspector] public GameObject currentInstance;

    public void SetOccupied(GameObject instance)
    {
        occupied = instance != null;
        currentInstance = instance;
    }

    public void Clear()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
        occupied = false;
    }
}
