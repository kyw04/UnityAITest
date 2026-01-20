using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class SimpleMoveAgent : Agent
{
    public Transform target;
    public float moveSpeed = 5f;
    public float rotateSpeed = 120f; // degrees per second
    Rigidbody rb;

    // New Input System: 인스펙터에서 할당 (Input Actions asset의 Action 참조)
    public InputActionReference moveAction; // Vector2 (x: 좌우, y: 앞뒤)

    public override void OnEpisodeBegin()
    {
        rb = GetComponent<Rigidbody>();
        transform.localPosition = new Vector3(Random.Range(-20f, 20f), 0.5f, Random.Range(-20f, 20f));
        target.localPosition = new Vector3(Random.Range(-20f, 20f), 0.5f, Random.Range(-20f, 20f));
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector3 relPos = target.localPosition - transform.localPosition;
        sensor.AddObservation(relPos.x);
        sensor.AddObservation(relPos.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        Vector3 moveDir = Vector3.zero;
        float turn = 0f;

        switch (action)
        {
            case 0: // no-op
                break;
            case 1: // 전진
                moveDir = transform.forward;
                break;
            case 2: // 후진
                moveDir = -transform.forward;
                break;
            case 3: // 우회전
                turn = 1f;
                break;
            case 4: // 좌회전
                turn = -1f;
                break;
            default:
                break;
        }

        rb.AddForce(moveDir * 0.5f, ForceMode.VelocityChange);
        
        if (Mathf.Abs(turn) > 0f)
        {
            transform.Rotate(Vector3.up, turn * rotateSpeed * Time.deltaTime, Space.Self);
        }

        if (moveDir != Vector3.zero)
        {
            rb.AddForce(moveDir.normalized * moveSpeed * Time.deltaTime, ForceMode.VelocityChange);
        }

        if (target != null)
        {
            float dist = Vector3.Distance(transform.localPosition, target.localPosition);
            float reward = -dist * 0.001f;
            AddReward(reward);

            if (dist < 0.5f)
            {
                AddReward(5.0f);
                EndEpisode();
            }
        }

        if (Vector3.Distance(transform.position, Vector3.zero) > 21f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    // New Input System 기반 Heuristic
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        int act = 0; // 기본 no-op

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
