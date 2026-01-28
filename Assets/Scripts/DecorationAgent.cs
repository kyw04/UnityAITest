using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DecorationAgent : Agent
{
    public GameObject[] decorations;
    public Bounds area;
    
    public override void Initialize()
    {
        
    }

    public override void OnEpisodeBegin()
    {
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(area.center + transform.position, area.size);
    }
}
