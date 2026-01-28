using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class SimpleMoveAgent : Agent
{
    public Transform target;
    public float moveSpeed = 3f;         // "VelocityChange" 기준이면 너무 크지 않게
    public float rotateSpeed = 120f;     // deg/sec
    public float dis = 5f;

    private Rigidbody rb;
    private float prevDist;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-dis, dis), 0.5f, Random.Range(-dis, dis));
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (target != null)
            target.localPosition = new Vector3(Random.Range(-dis, dis), 0.5f, Random.Range(-dis, dis));

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        prevDist = (target != null) ? Vector3.Distance(transform.position, target.position) : 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target == null)
        {
            sensor.AddObservation(Vector3.zero); // rel pos
            sensor.AddObservation(Vector2.zero); // forward xz
            return;
        }

        Vector3 rel = target.position - transform.position;
        sensor.AddObservation(rel.x);
        sensor.AddObservation(rel.z);

        // 내 바라보는 방향(회전 상태)도 같이 넣어주면 학습 빨라짐
        Vector3 f = transform.forward;
        sensor.AddObservation(f.x);
        sensor.AddObservation(f.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (target == null) return;

        int action = actions.DiscreteActions[0];

        float turn = 0f;
        float move = 0f;

        switch (action)
        {
            case 0: break;        // no-op
            case 1: move = 1f; break;  // forward
            case 2: move = -1f; break; // backward
            case 3: turn = 1f; break;  // right
            case 4: turn = -1f; break; // left
        }

        // 회전: Rigidbody로
        if (Mathf.Abs(turn) > 0f)
        {
            Quaternion delta = Quaternion.Euler(0f, turn * rotateSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * delta);
        }

        // 이동: 한 번만, 일관되게
        if (Mathf.Abs(move) > 0f)
        {
            Vector3 vChange = transform.forward * (move * moveSpeed);
            rb.AddForce(vChange, ForceMode.VelocityChange);
        }

        // 보상: "거리 감소" 기반 shaping (안정적)
        float dist = Vector3.Distance(transform.position, target.position);
        float distDelta = prevDist - dist;   // 가까워지면 +
        AddReward(distDelta * 0.5f);

        // 작은 시간 패널티(빨리 끝내기)
        AddReward(-0.001f);

        prevDist = dist;

        if (dist < 0.25f)
        {
            AddReward(1.0f);
            EndEpisode();
        }

        if (Vector3.Distance(transform.position, Vector3.zero) > dis + 5f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        int act = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) act = 1;
            else if (Keyboard.current.downArrowKey.isPressed) act = 2;
            else if (Keyboard.current.rightArrowKey.isPressed) act = 3;
            else if (Keyboard.current.leftArrowKey.isPressed) act = 4;
        }

        discrete[0] = act;
    }
}
