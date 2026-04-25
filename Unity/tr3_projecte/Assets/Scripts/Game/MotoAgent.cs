using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MotoAgent : Agent
{
    private PlayerController playerController;
    private Vector2 startingPos;
    private Quaternion startingRot;

    public override void Initialize()
    {
        playerController = GetComponent<PlayerController>();
        startingPos = transform.localPosition;
        startingRot = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // 1. Tornem la moto a la seva posició inicial
        transform.localPosition = startingPos;
        transform.localRotation = startingRot;

        // 2. Avisem al PlayerController perquè esborri les esteles velles
        playerController.ResetForTraining();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Afegim la nostra direcció actual com a observació extra (a part dels Raycasts)
        Vector2 dir = playerController.GetCurrentDirection();
        sensor.AddObservation(dir.x);
        sensor.AddObservation(dir.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 0 = Seguir recte, 1 = Girar Esquerra Relatiu, 2 = Girar Dreta Relatiu
        int action = actions.DiscreteActions[0];

        Vector2 currentDir = playerController.GetCurrentDirection();
        Vector2 nextDir = currentDir;

        if (action == 1) // Girar 90 graus a l'esquerra respecte on miro
        {
            if (currentDir == Vector2.up) nextDir = Vector2.left;
            else if (currentDir == Vector2.left) nextDir = Vector2.down;
            else if (currentDir == Vector2.down) nextDir = Vector2.right;
            else if (currentDir == Vector2.right) nextDir = Vector2.up;
        }
        else if (action == 2) // Girar 90 graus a la dreta respecte on miro
        {
            if (currentDir == Vector2.up) nextDir = Vector2.right;
            else if (currentDir == Vector2.right) nextDir = Vector2.down;
            else if (currentDir == Vector2.down) nextDir = Vector2.left;
            else if (currentDir == Vector2.left) nextDir = Vector2.up;
        }

        // Apliquem la decisió
        playerController.SetDirectionFromAI(nextDir);

        // Petita recompensa per cada pas que sobreviu
        AddReward(0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        
        Vector2 currentDir = playerController.GetCurrentDirection();
        
        // Simulem com jugaria un humà per comprovar si l'entorn funciona bé
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (currentDir == Vector2.up) discreteActionsOut[0] = 1;
            else if (currentDir == Vector2.down) discreteActionsOut[0] = 2;
            else if (currentDir == Vector2.right) discreteActionsOut[0] = 1; // Simplificació
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            if (currentDir == Vector2.up) discreteActionsOut[0] = 2;
            else if (currentDir == Vector2.down) discreteActionsOut[0] = 1;
            else if (currentDir == Vector2.left) discreteActionsOut[0] = 1; // Simplificació
        }
    }

    public void RegisterCrash()
    {
        // Quan la moto mor, rep una gran penalització
        SetReward(-1f);
        EndEpisode();
    }
}
