using UnityEngine;

public enum FieldType
{
    Floor,
    Wall
}

public class DecorationField : MonoBehaviour
{
    public  FieldType fieldType;
    public Bounds area;
    public Color32 color;
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(area.center + transform.position, area.size);
    }
}
