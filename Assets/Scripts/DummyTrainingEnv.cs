using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DummyTrainingEnv : NewGameController
{
    [Serializable]
    public struct Dummy
    {
        public FencerColor color;
        public BladeworkController bladeworkController;
        public NewHitDetector hitDetector;
        public Transform fencerTransform;
        public Transform epeeTransform;
        public Transform epeeTargetTransform;
        public List<Transform> selfTargetAreas;
        public Transform startPoint;
        public Transform epeeTipRaycastTransform;
    }

    public float startWaitForTime;
    public float xRand;
    public float zRand;
    
    public TrainingEnv.Fencer fencerOne;
    public Dummy dummyTwo;
    
    public Transform boundXPositive;
    public Transform boundXNegative;
    public Transform boundZPositive;
    public Transform boundZNegative;
    public MeshRenderer floorMesh;
    public Material defaultColor;
    public Material fencerOneColor;
    public Material dummyTwoColor;
    
    private bool _outOfBound;
    private bool _inGame;
    private Vector3 _initialEpeePosition;
    private float _dummyOffsetX;
    private float _dummyOffsetZ;
    
    private void Start()
    {
        // linking all together
        fencerOne.bladeworkController.worldPointToTargets = dummyTwo.selfTargetAreas;
        dummyTwo.bladeworkController.worldPointToTargets = fencerOne.selfTargetAreas;

        fencerOne.hitDetector.Initialize(fencerOne.color, dummyTwo.color, this);
        dummyTwo.hitDetector.Initialize(dummyTwo.color, fencerOne.color, this);
        
        fencerOne.agent.SetFencerNum(fencerOne.color);
        // dummyTwo.agent.SetFencerNum(fencerTwo.color);

        var observer = GetComponent<Observer>();
        observer.colorOne = fencerOne.color;
        observer.colorTwo = dummyTwo.color;
        observer.raycastTransformOne = fencerOne.epeeTipRaycastTransform;
        observer.raycastTransformTwo = dummyTwo.epeeTipRaycastTransform;
        
        _outOfBound = false;
    }

    public override void StartGame()
    {
        if (_inGame) return;

        var positionDiff = fencerOne.startPoint.position - fencerOne.fencerTransform.position;
        var rotationDiff = Quaternion.Inverse(fencerOne.fencerTransform.rotation) * fencerOne.startPoint.rotation;
        fencerOne.fencerTransform.position += positionDiff;
        fencerOne.epeeTransform.position += rotationDiff * positionDiff;
        fencerOne.epeeTargetTransform.position += rotationDiff * positionDiff;
        fencerOne.fencerTransform.rotation = fencerOne.startPoint.rotation;

        var rand = UnityEngine.Random.insideUnitSphere;
        rand.x *= xRand;
        rand.z *= zRand;
        _dummyOffsetX = rand.x;
        _dummyOffsetZ = rand.z;

        positionDiff = dummyTwo.startPoint.position - dummyTwo.fencerTransform.position + rand;
        rotationDiff = Quaternion.Inverse(dummyTwo.fencerTransform.rotation) * dummyTwo.startPoint.rotation;
        dummyTwo.fencerTransform.position += positionDiff;
        dummyTwo.epeeTransform.position += rotationDiff * positionDiff;
        dummyTwo.epeeTargetTransform.position += rotationDiff * positionDiff;
        dummyTwo.fencerTransform.rotation = dummyTwo.startPoint.rotation;

        StartCoroutine(StartCountDown());
    }
    
    private IEnumerator StartCountDown()
    {
        yield return new WaitForSeconds(startWaitForTime);
        floorMesh.material = defaultColor;
        _inGame = true;
        _passedFrameCount = 0;
    }

    public override bool Started()
    {
        return _inGame;
    }

    public override void EndGame()
    {
        _inGame = false;
        fencerOne.agent.EndEpisode();
        _passedFrameCount = 0;
    }

    public override void RegisterHit(FencerColor fencerColor)
    {
        if (!_inGame) return;
        
        
        if (fencerColor == fencerOne.color)
        {
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/win_x", _dummyOffsetX, StatAggregationMethod.Histogram);
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/win_z", _dummyOffsetZ, StatAggregationMethod.Histogram);
            fencerOne.agent.SetReward(1);
            floorMesh.material = fencerOneColor;
        }
        else
        {
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/lose_x", _dummyOffsetX, StatAggregationMethod.Histogram);
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/lose_z", _dummyOffsetZ, StatAggregationMethod.Histogram);
            fencerOne.agent.SetReward(-1);
            floorMesh.material = dummyTwoColor;
        }

        _inGame = false;
        fencerOne.agent.EndEpisode();
        _passedFrameCount = 0;
    }
    
    
    private void Update()
    {
        if (!_inGame) return;

        // fencer position is usually changed in Update
        var fencerOnePosition = fencerOne.fencerTransform.position;
        if (fencerOnePosition.x > boundXPositive.position.x)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.x < boundXNegative.position.x)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.z > boundZPositive.position.z)
        {
            _outOfBound = true;
        } else if (fencerOnePosition.z < boundZNegative.position.z)
        {
            _outOfBound = true;
        }

        // check if fencers still facing each other
        var fencerOneStartPos = fencerOne.startPoint.position;
        var dummyTwoStartPos = dummyTwo.startPoint.position;
        var startPosZDiff = dummyTwoStartPos.z - fencerOneStartPos.z;
        var curPosZDiff = dummyTwo.fencerTransform.position.z - fencerOnePosition.z;
        if (startPosZDiff > 0 && curPosZDiff < 0)
        {
            _outOfBound = true;
        } else if (startPosZDiff < 0 && curPosZDiff > 0)
        {
            _outOfBound = true;
        }
    }
    
    public int dummyActionInterval = 1000;
    private int _passedFrameCount = 0;
    private int _wristX;
    private int _wristY;
    private int _wristZ;
    private int _pointTo;
    private bool _extended;
    
    private void FixedUpdate()
    {
        if (!_inGame) return;

        if (_passedFrameCount == 0)
        {
            var envParams = Academy.Instance.EnvironmentParameters;
            _wristX = Mathf.RoundToInt(envParams.GetWithDefault("wrist_x", 0));
            _wristY = Mathf.RoundToInt(envParams.GetWithDefault("wrist_y", 0));
            _wristZ = Mathf.RoundToInt(envParams.GetWithDefault("wrist_z", 1));
            _pointTo = Mathf.RoundToInt(envParams.GetWithDefault("point_to", 0));
            _extended = Mathf.RoundToInt(envParams.GetWithDefault("extended", 0)) == 1;
        }

        _passedFrameCount += 1;
        if (_passedFrameCount == dummyActionInterval)
        {
            _passedFrameCount = 0;
        }

        dummyTwo.bladeworkController.DoWristTranslation(_wristZ, _wristX, _wristY, _extended, 0);
        dummyTwo.bladeworkController.DoWristRotation(0, _pointTo);

        
        // place all agent-related functions in fixed update
        if (_outOfBound)
        {
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/miss_x", _dummyOffsetX, StatAggregationMethod.Histogram);
            Academy.Instance.StatsRecorder.Add("DummyPositionResult/miss_z", _dummyOffsetZ, StatAggregationMethod.Histogram);
            fencerOne.agent.SetReward(-1);
            EndGame();
            _outOfBound = false;
        }
    }
}
