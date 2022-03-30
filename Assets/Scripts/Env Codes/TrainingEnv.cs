using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrainingEnv : NewGameController
{
    [Serializable]
    public struct Fencer
    {
        public NewAgentFencer agent;
        public BladeworkController bladeworkController;
        public NewHitDetector hitDetector;
        public Transform fencerTransform;
        public Transform epeeTransform;
        public Transform epeeTargetTransform;
        public List<Transform> selfTargetAreas;
        public Transform startPoint;
        public Transform nearerStartPoint;

        // public float fencerStartZVariation;
        // public float fencerStartXVariation;
    }

    public Fencer fencerOne;
    public Fencer fencerTwo;
    public Transform boundXPositive;
    public Transform boundXNegative;
    public Transform boundZPositive;
    public Transform boundZNegative;
    public MeshRenderer floorMesh;
    public Material defaultColor;
    public Material fencerOneColor;
    public Material fencerTwoColor;
    public Material outOfBoundColor;

    public float startWaitForTime;
    public float xRand;
    public float zRand;

    private int _startCount;
    private bool _inGame;
    private bool _outOfBound;

    private void Start()
    {
        // linking all together
        fencerOne.bladeworkController.worldPointToTargets = fencerTwo.selfTargetAreas;
        fencerTwo.bladeworkController.worldPointToTargets = fencerOne.selfTargetAreas;

        fencerOne.hitDetector.Initialize(1, 2, this);
        fencerTwo.hitDetector.Initialize(2, 1, this);
        
        fencerOne.agent.SetFencerNum(1);
        fencerTwo.agent.SetFencerNum(2);

        _outOfBound = false;
    }

    private IEnumerator ResetFencersPosition()
    {
        _inGame = false;
        var xRandCoef = Academy.Instance.EnvironmentParameters.GetWithDefault("start_point_x_rand_coef", 0);
        var zRandCoef = Academy.Instance.EnvironmentParameters.GetWithDefault("start_point_z_rand_coef", 0);
        var footworkEnabled = Academy.Instance.EnvironmentParameters.GetWithDefault("footwork_enabled", 1) > 0;
            
        var rand = UnityEngine.Random.insideUnitSphere;
        rand.x *= xRand * xRandCoef;
        rand.z *= zRand * zRandCoef;
        var startPoint = footworkEnabled ? fencerOne.startPoint.position : fencerOne.nearerStartPoint.position;
        var positionDiff = startPoint - fencerOne.fencerTransform.position + rand;
        var rotationDiff = Quaternion.Inverse(fencerOne.fencerTransform.rotation) * fencerOne.startPoint.rotation;
        fencerOne.fencerTransform.position += positionDiff;
        fencerOne.epeeTransform.position += rotationDiff * positionDiff;
        fencerOne.epeeTargetTransform.position += rotationDiff * positionDiff;
        fencerOne.fencerTransform.rotation = fencerOne.startPoint.rotation;

        rand = UnityEngine.Random.insideUnitSphere;
        rand.x *= xRand * xRandCoef;
        rand.z *= zRand * zRandCoef;
        startPoint = footworkEnabled ? fencerTwo.startPoint.position : fencerTwo.nearerStartPoint.position;
        positionDiff = startPoint - fencerTwo.fencerTransform.position + rand;
        rotationDiff = Quaternion.Inverse(fencerTwo.fencerTransform.rotation) * fencerTwo.startPoint.rotation;
        fencerTwo.fencerTransform.position += positionDiff;
        fencerTwo.epeeTransform.position += rotationDiff * positionDiff;
        fencerTwo.epeeTargetTransform.position += rotationDiff * positionDiff;
        fencerTwo.fencerTransform.rotation = fencerTwo.startPoint.rotation;

        yield return StartCountDown();
        _inGame = true;
    }

    public override void StartGame()
    {
        _inGame = false;
        _startCount += 1;
        if (_startCount >= 2)
        {
            StopAllCoroutines();    // in case ResetFencersPosition is called before timeout
            Academy.Instance.StatsRecorder.Add("Environment/WinLoseCount", 0, StatAggregationMethod.Sum);
            StartCoroutine(ResetFencersPosition());

            _startCount = 0;
        }
        else
        {
            _inGame = false;
        }
    }

    private IEnumerator StartCountDown()
    {
        yield return new WaitForSeconds(startWaitForTime);
        floorMesh.material = defaultColor;
    }

    public override bool Started()
    {
        return _inGame;
    }

    public override void EndGame()
    {
        fencerOne.agent.EndEpisode();
        fencerTwo.agent.EndEpisode();
        _inGame = false;
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

        // check if fencers still facing each other
        var fencerOneStartPos = fencerOne.startPoint.position;
        var fencerTwoStartPos = fencerTwo.startPoint.position;
        var startPosZDiff = fencerTwoStartPos.z - fencerOneStartPos.z;
        var curPosZDiff = fencerTwoPosition.z - fencerOnePosition.z;
        if (startPosZDiff > 0 && curPosZDiff < 0)
        {
            _outOfBound = true;
        } else if (startPosZDiff < 0 && curPosZDiff > 0)
        {
            _outOfBound = true;
        }
    }

    private void FixedUpdate()
    {
        // place all agent-related functions in fixed update
        if (_outOfBound)
        {
            fencerOne.agent.AddReward(-0.5f);
            fencerTwo.agent.AddReward(-0.5f);
            floorMesh.material = outOfBoundColor;
            _outOfBound = false;
            StartCoroutine(ResetFencersPosition());
        }
    }

    
    public override void RegisterHit(int fencerNum)
    {
        if (!_inGame) return;
        
        if (fencerNum == 1)
        {
            fencerOne.agent.SetReward(1);
            fencerTwo.agent.SetReward(-1);
            floorMesh.material = fencerOneColor;
        }
        else
        {
            fencerTwo.agent.SetReward(1);
            fencerOne.agent.SetReward(-1);
            floorMesh.material = fencerTwoColor;
        }

        fencerOne.agent.EndEpisode();
        fencerTwo.agent.EndEpisode();
        _inGame = false;
    }
}
