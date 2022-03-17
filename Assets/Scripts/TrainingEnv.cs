using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainingEnv : NewGameController
{
    [Serializable]
    public struct Fencer
    {
        public FencerColor color;
        public NewAgentFencer agent;
        public BladeworkController bladeworkController;
        public NewHitDetector hitDetector;
        public Transform fencerTransform;
        public Transform epeeTransform;
        public Transform epeeTargetTransform;
        public List<Transform> selfTargetAreas;
        public Transform startPoint;

        // public float fencerStartZVariation;
        // public float fencerStartXVariation;
    }

    [Serializable]
    public struct Reward
    {
        public float timeStepReward;
        public float closenessThreshold;
        public float oppTipClosenessReward;
        public float selfTipClosenessReward;
    }

    public Fencer fencerOne;
    public Fencer fencerTwo;
    public Transform boundXPositive;
    public Transform boundXNegative;
    public Transform boundZPositive;
    public Transform boundZNegative;

    private int _startCount;
    private bool _inGame;
    private bool _outOfBound;

    private void Start()
    {
        // linking all together
        fencerOne.bladeworkController.worldPointToTargets = fencerTwo.selfTargetAreas;
        fencerTwo.bladeworkController.worldPointToTargets = fencerOne.selfTargetAreas;

        fencerOne.hitDetector.Initialize(fencerOne.color, fencerTwo.color, this);
        fencerTwo.hitDetector.Initialize(fencerTwo.color, fencerOne.color, this);
        
        fencerOne.agent.SetFencerNum(fencerOne.color);
        fencerTwo.agent.SetFencerNum(fencerTwo.color);

        _outOfBound = false;
    }

    public override void StartGame()
    {
        _inGame = false;
        _startCount += 1;
        if (_startCount >= 2)
        {
            var positionDiff = fencerOne.startPoint.position - fencerOne.fencerTransform.position;
            var rotationDiff = Quaternion.Inverse(fencerOne.fencerTransform.rotation) * fencerOne.startPoint.rotation;
            fencerOne.fencerTransform.position += positionDiff;
            fencerOne.epeeTransform.position += rotationDiff * positionDiff;
            fencerOne.epeeTargetTransform.position += rotationDiff * positionDiff;
            fencerOne.fencerTransform.rotation = fencerOne.startPoint.rotation;

            positionDiff = fencerTwo.startPoint.position - fencerTwo.fencerTransform.position;
            rotationDiff = Quaternion.Inverse(fencerTwo.fencerTransform.rotation) * fencerTwo.startPoint.rotation;
            fencerTwo.fencerTransform.position += positionDiff;
            fencerTwo.epeeTransform.position += rotationDiff * positionDiff;
            fencerTwo.epeeTargetTransform.position += rotationDiff * positionDiff;
            fencerTwo.fencerTransform.rotation = fencerTwo.startPoint.rotation;

            _inGame = true;
            _startCount = 0;
        }
        else
        {
            _inGame = false;
        }
    }

    public override bool Started()
    {
        return _inGame;
    }

    public override void EndGame()
    {
        _inGame = false;
        fencerOne.agent.EndEpisode();
        fencerTwo.agent.EndEpisode();
    }

    private void Update()
    {
        // fencer position is usually changed in Update
        var fencerOnePosition = fencerOne.fencerTransform.position;
        var fencerTwoPosition = fencerTwo.fencerTransform.position;
        if (fencerOnePosition.x > boundXPositive.position.x || fencerTwoPosition.x > boundXPositive.position.x)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.x < boundXNegative.position.x || fencerTwoPosition.x < boundXNegative.position.x)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.z > boundZPositive.position.z || fencerTwoPosition.z > boundZPositive.position.z)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.z < boundZNegative.position.z || fencerTwoPosition.z < boundZNegative.position.z)
        {
            _outOfBound = true;
        }
    }

    private void FixedUpdate()
    {
        // place all agent-related functions should be in fixed update
        if (_outOfBound)
        {
            fencerOne.agent.SetReward(0);
            fencerTwo.agent.SetReward(0);
            EndGame();
            _outOfBound = false;
        }
    }

    public override void RegisterHit(FencerColor fencerColor)
    {
        if (!_inGame) return;
        
        if (fencerColor == fencerOne.color)
        {
            fencerOne.agent.SetReward(1);
            fencerTwo.agent.SetReward(-1);
        }
        else
        {
            fencerTwo.agent.SetReward(1);
            fencerOne.agent.SetReward(-1);
        }

        _inGame = false;
        fencerOne.agent.EndEpisode();
        fencerTwo.agent.EndEpisode();
    }
}