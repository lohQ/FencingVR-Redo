using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum FencerColor
{
    Green = 0,
    Red = 1
}

public class NewAgentFencer : Agent
{
    public NewGameController gameController;
    public Observer observer;

    // offensive fencer get punished every timestep, but get rewarded for tip's closeness to target
    // defensive fencer get reward every timestep, but get punished for opponent's tip's closeness to self 
    public float timeStepReward;
    public float oppTipClosenessReward;
    public float selfTipClosenessReward;
    
    private BladeworkController _bladeworkController;
    private Animator _animator;
    private FollowFootwork _followFootwork;
    private int _stepHash;

    private EnvironmentParameters _resetParams;
    private BufferSensorComponent _bufferSensor;
    private int _fencerNum;
    private float _prevSelfTipToOppTarget;
    private float _prevOppTipToSelfTarget;
    private float _distanceThreshold;

    private StatsRecorder _statsRecorder;

    
    private void Start()
    {
        _animator = GetComponent<Animator>();
        _bladeworkController = GetComponent<BladeworkController>();
        _followFootwork = GetComponent<FollowFootwork>();
        _stepHash = Animator.StringToHash("step");

        _resetParams = Academy.Instance.EnvironmentParameters;
        _bufferSensor = GetComponent<BufferSensorComponent>();
        _statsRecorder = Academy.Instance.StatsRecorder;

        _distanceThreshold = observer.tipClosenessThreshold;
        _prevSelfTipToOppTarget = _distanceThreshold;
        _prevOppTipToSelfTarget = _distanceThreshold;
    }

    public void SetFencerNum(FencerColor color)
    {
        _fencerNum = (int)color + 1;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log($"[{transform.parent.name}] OnEpisodeBegin, CompletedEpisodes: {CompletedEpisodes}");
        _followFootwork.ResetCoroutines();
        _bladeworkController.ResetCoroutines();
        gameController.StartGame();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observer.CollectObservations(sensor, _bufferSensor, _fencerNum);

        var selfTipToOppTarget = observer.SelfTipRaycastHitDistance(_fencerNum);
        if (selfTipToOppTarget > 0 && selfTipToOppTarget < _distanceThreshold)
        {
            var advanced = _prevSelfTipToOppTarget - selfTipToOppTarget;
            if (advanced > 0)
            {
                AddReward((advanced/_distanceThreshold) * selfTipClosenessReward / MaxStep);
            }
            _prevSelfTipToOppTarget = selfTipToOppTarget;
        }
        else
        {
            _prevOppTipToSelfTarget = _distanceThreshold;
        }

        var oppTipToSelfTarget = observer.OppTipRaycastHitDistance(_fencerNum);
        if (oppTipToSelfTarget > 0 && oppTipToSelfTarget < _distanceThreshold)
        {
            var advanced = _prevOppTipToSelfTarget - oppTipToSelfTarget;
            if (advanced > 0)
            {
                AddReward((advanced/_distanceThreshold) * oppTipClosenessReward / MaxStep);
            }
            _prevOppTipToSelfTarget = oppTipToSelfTarget;
        }
        else
        {
            _prevOppTipToSelfTarget = _distanceThreshold;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = Input.GetKey(KeyCode.F) ? 0 : 1;
        discreteActionsOut[1] = Input.GetKey(KeyCode.A) ? 2 : Input.GetKey(KeyCode.D) ? 0 : 1;
        discreteActionsOut[2] = Input.GetKey(KeyCode.W) ? 2 : Input.GetKey(KeyCode.S) ? 0 : 1;
        discreteActionsOut[3] = Input.GetKey(KeyCode.E) ? 1 : 0;

        discreteActionsOut[4] = Input.GetKey(KeyCode.LeftArrow) ? 0 : Input.GetKey(KeyCode.RightArrow) ? 2 : 1;
        
        if (Input.GetKey(KeyCode.R))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[5] = 0;  // supp -1
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                discreteActionsOut[5] = 2;  // supp +1
            }
            else
            {
                discreteActionsOut[5] = 1;  // supp 0
            }
            
            if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                discreteActionsOut[6] = 0;      // rotate counterclockwise
            }
            else if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                discreteActionsOut[6] = 1;      // rotate clockwise
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                discreteActionsOut[6] = 2;      // point to center
            }
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                discreteActionsOut[6] = 3;      // world point to 0
            }
            else if (Input.GetKey(KeyCode.Alpha7))
            {
                discreteActionsOut[6] = 4;      // world point to 1
            }
            else if (Input.GetKey(KeyCode.Alpha8))
            {
                discreteActionsOut[6] = 5;      // world point to 2
            }
            else if (Input.GetKey(KeyCode.Alpha9))
            {
                discreteActionsOut[6] = 6;      // world point to 3
            }
            else if (Input.GetKey(KeyCode.Alpha0))
            {
                discreteActionsOut[6] = 7;      // world point to 4
            }
            else
            {
                discreteActionsOut[6] = 2;      // point to center
            }
        }
        else
        {
            discreteActionsOut[5] = 1;  // supp 0
            discreteActionsOut[6] = 2;  // point to center
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[7] = 4;  // small step
                // Debug.Log("UpArrow + Alpha1, Small Step");
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[7] = 6;  // large step
                // Debug.Log("UpArrow + Alpha3, Large Step");
            } else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[7] = 7;  // lunge
                // Debug.Log("UpArrow + Alpha4, Lunge");
            }
            else 
            {
                discreteActionsOut[7] = 5;  // regular step
                // Debug.Log("UpArrow, Regular Step");
            }
        } 
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[7] = 2;  // small step
                // Debug.Log("DownArrow + Alpha1, Small Step");
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[7] = 0;  // small step
                // Debug.Log("DownArrow + Alpha3, Large Step");
            }
            else
            {
                discreteActionsOut[7] = 1;  // regular step
                // Debug.Log("DownArrow, Regular Step");
            }
        }
        else
        {
            // Debug.Log("No up nor down arrow, no step");
            discreteActionsOut[7] = 3;
        }

        if (!gameController.Started())
        {
            // Debug.Log("game not started yet.");
            discreteActionsOut[0] = 1;
            discreteActionsOut[1] = 1;
            discreteActionsOut[2] = 1;
            discreteActionsOut[3] = 0;
            discreteActionsOut[4] = 1;
            discreteActionsOut[5] = 1;
            discreteActionsOut[6] = 2;
            discreteActionsOut[7] = 3;
        }

        if (!_bladeworkController.CanRotateWrist())
        {
            discreteActionsOut[5] = 1;
            discreteActionsOut[6] = 2;
        }
        if (!_bladeworkController.CanTranslateWrist())
        {
            discreteActionsOut[0] = 1;
            discreteActionsOut[1] = 1;
            discreteActionsOut[2] = 1;
            discreteActionsOut[3] = 0;
            discreteActionsOut[4] = 1;
        }

        if (!ReadyForFootwork())
        {
            discreteActionsOut[7] = 3;
        }
    }

    private bool ReadyForFootwork()
    {
        return _followFootwork.ReadyForNewFootwork();
    }

    private static void DisableWristRotation(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(5, 0, false);
        actionMask.SetActionEnabled(5, 2, false);
            
        // depends on how many worldPointToTargets are there
        actionMask.SetActionEnabled(6, 1, false);
        actionMask.SetActionEnabled(6, 2, false);
        actionMask.SetActionEnabled(6, 3, false);
        actionMask.SetActionEnabled(6, 4, false);
    }

    private static void DisableWristTranslation(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(0, 1, false);
        actionMask.SetActionEnabled(1, 0, false);
        actionMask.SetActionEnabled(1, 2, false);
        actionMask.SetActionEnabled(2, 0, false);
        actionMask.SetActionEnabled(2, 2, false);
        actionMask.SetActionEnabled(3, 1, false);
        actionMask.SetActionEnabled(4, 0, false);
        actionMask.SetActionEnabled(4, 2, false);
    }

    private static void DisableFootwork(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(7, 0, false);
        actionMask.SetActionEnabled(7, 1, false);
        actionMask.SetActionEnabled(7, 2, false);
        actionMask.SetActionEnabled(7, 4, false);
        actionMask.SetActionEnabled(7, 5, false);
        actionMask.SetActionEnabled(7, 6, false);
        actionMask.SetActionEnabled(7, 7, false);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!gameController.Started())
        {
            DisableFootwork(actionMask);
            DisableWristRotation(actionMask);
            DisableWristTranslation(actionMask);
            return;
        }
        
        if (!_bladeworkController.CanRotateWrist())
        {
            DisableWristRotation(actionMask);
        }
        if (!_bladeworkController.CanTranslateWrist())
        {
            DisableWristTranslation(actionMask);
        }

        if (!ReadyForFootwork())
        {
            DisableFootwork(actionMask);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        _bladeworkController.DoWristTranslation(
            discreteActions[0], discreteActions[1] - 1, discreteActions[2] - 1, 
            discreteActions[3] == 1, discreteActions[4] - 1);
        _bladeworkController.DoWristRotation(discreteActions[5] - 1, discreteActions[6] - 2);
        
        if (discreteActions[7] - 3 != 0)
        {
            _animator.SetInteger(_stepHash, discreteActions[7] - 3);
        }

        if (gameController.Started())
        {
            AddReward(timeStepReward / MaxStep);
        }
    }
    
}
