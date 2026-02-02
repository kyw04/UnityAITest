using UnityEngine;

public class PlacementBounds : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public Vector3 size = Vector3.one;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
