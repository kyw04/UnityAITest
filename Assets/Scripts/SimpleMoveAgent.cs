using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SimpleMoveAgent : Agent
{
    public Transform target;
    public float moveSpeed = 5f;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // 에이전트와 타깃 초기 위치 랜덤화
        transform.localPosition = new Vector3(Random.Range(-3f, 3f), 0.5f, 0f);
        if (target != null)
            target.localPosition = new Vector3(Random.Range(-3f, 3f), 0.5f, 0f);
        rb.linearVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 상대 위치 관측: target - agent (x, z)
        Vector3 relPos = target.localPosition - transform.localPosition;
        sensor.AddObservation(relPos.x);
        sensor.AddObservation(relPos.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 연속형 행동: actions.ContinuousActions[0] in [-1,1]
        float move = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        Vector3 velocity = new Vector3(move * moveSpeed, rb.linearVelocity.y, 0f);
        rb.linearVelocity = velocity;

        // 거리 기반 보상 (작아질수록 양수)
        float dist = Vector3.Distance(transform.localPosition, target.localPosition);
        float reward = -dist * 0.001f;
        AddReward(reward);

        // 목표 도달
        if (dist < 0.5f)
        {
            AddReward(1.0f);
            EndEpisode();
        }

        // 맵 밖으로 나가면 실패
        if (Mathf.Abs(transform.localPosition.x) > 6f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) h = 1f;
        cont[0] = h;
    }
}
