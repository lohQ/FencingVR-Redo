using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainingEnv : NewGameController
{
    [Serializable]
    public enum TrainingMode {
        Attack,
        Defense,
        SelfPlay
    }

    public TrainingMode trainingMode;
    public Transform fencerToTrainStartPoint;
    public Transform trainingDummyPoint;
    public Transform fencerToTrain;
    public Transform trainingDummy;
    public List<Transform> fencerTargetAreas;
    public List<Transform> dummyTargetAreas;
    public BladeworkController fencerBladeworkController;
    public BladeworkController dummyBladeworkController;

    public float fencerStartZVariation;
    public float fencerStartXVariation;
    
    public override void StartGame(NewAgentFencer agentFencer)
    {
        trainingDummy.position = trainingDummyPoint.position;
        trainingDummy.rotation = trainingDummyPoint.rotation;

        var fencerRandomStart = Random.insideUnitSphere;
        fencerRandomStart.y = fencerToTrain.position.y;
        fencerRandomStart.z *= fencerStartZVariation;
        fencerRandomStart.x *= fencerStartXVariation;
        fencerToTrain.position = fencerToTrainStartPoint.position + fencerRandomStart;
        fencerToTrain.rotation = fencerToTrainStartPoint.rotation;

        fencerBladeworkController.worldPointToTargets = dummyTargetAreas;
        dummyBladeworkController.worldPointToTargets = fencerTargetAreas;
    }

    public override bool Started()
    {
        throw new NotImplementedException();
    }

    public override void AddObservations(VectorSensor sensor)
    {
        throw new NotImplementedException();
    }

    public override void EndGame()
    {
        throw new NotImplementedException();
    }
}
