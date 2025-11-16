using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BalanceAgent : Agent
{
    [SerializeField] public GameController controller;
    [SerializeField] public Rigidbody ball;

    public override void OnActionReceived(ActionBuffers actions)
    {
        var a = actions.ContinuousActions;
        controller.ApplyInput(a[0], a[1], a[2]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(controller.xDeg / controller.maxAngle);
        sensor.AddObservation(controller.zDeg / controller.maxAngle);

        sensor.AddObservation(controller.bob.transform.localPosition.x / controller.moveLimit);

        Vector3 ballPos = controller.ball.transform.localPosition;
        Vector3 platformPos = controller.bob.transform.localPosition;

        sensor.AddObservation(ballPos - platformPos);

        sensor.AddObservation(ball.linearVelocity);

        Vector3 targetPos = controller.target.transform.localPosition;

        sensor.AddObservation(targetPos - ballPos);

        //12 megfigyelés
    }
}
