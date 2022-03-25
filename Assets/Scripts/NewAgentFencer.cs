using System;
using TMPro;
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
    }

    public void SetFencerNum(FencerColor color)
    {
        _fencerNum = (int)color + 1;
    }

    public override void OnEpisodeBegin()
    {
        timeStepReward = _resetParams.GetWithDefault("timestep_reward", timeStepReward);
        _distanceThreshold = _resetParams.GetWithDefault("tip_raycast_reward_threshold", _distanceThreshold);
        _followFootwork.ResetCoroutines();
        _bladeworkController.ResetCoroutines();
        gameController.StartGame();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observer.CollectObservations(sensor, _bufferSensor, _fencerNum);

        var selfTipToOppTarget = observer.SelfTipRaycastHitDistance(_fencerNum);
        if (selfTipToOppTarget > 0)
        {
            AddReward((1 - (selfTipToOppTarget / _distanceThreshold)) * selfTipClosenessReward / MaxStep);
        }
        
        var oppTipToSelfTarget = observer.OppTipRaycastHitDistance(_fencerNum);
        if (selfTipToOppTarget > 0)
        {
            AddReward((1 - (oppTipToSelfTarget / _distanceThreshold)) * oppTipClosenessReward / MaxStep);
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
            
            if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[6] = 0;      // pointToTargets[8]
            }
            else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[6] = 1;      // pointToTargets[6]
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                discreteActionsOut[6] = 2;      // pointToTargets[4]
            }
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                discreteActionsOut[6] = 3;      // pointToTargets[2]
            }
            else if (Input.GetKey(KeyCode.Alpha7))
            {
                discreteActionsOut[6] = 4;      // pointToTargets[0]
            }
            else if (Input.GetKey(KeyCode.Alpha8))
            {
                discreteActionsOut[6] = 5;      // worldPointToTargets[0]
            }
            else if (Input.GetKey(KeyCode.Alpha9))
            {
                discreteActionsOut[6] = 6;      // worldPointToTargets[1]
            }
            else if (Input.GetKey(KeyCode.Alpha0))
            {
                discreteActionsOut[6] = 7;      // worldPointToTargets[2]
            }
            else if (Input.GetKey(KeyCode.O))
            {
                discreteActionsOut[6] = 8;      // worldPointToTargets[3]
            }
            else if (Input.GetKey(KeyCode.I))
            {
                discreteActionsOut[6] = 9;      // worldPointToTargets[4]
            }
            else
            {
                discreteActionsOut[6] = 4;      // pointToTargets[0]
            }
        }
        else
        {
            discreteActionsOut[5] = 1;  // supp 0
            discreteActionsOut[6] = 4;  // point to center
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
            discreteActionsOut[6] = 4;
            discreteActionsOut[7] = 3;
        }

        if (!_bladeworkController.CanRotateWrist())
        {
            discreteActionsOut[5] = 1;
            discreteActionsOut[6] = 4;
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
        actionMask.SetActionEnabled(6, 0, false);
        actionMask.SetActionEnabled(6, 1, false);
        actionMask.SetActionEnabled(6, 2, false);
        actionMask.SetActionEnabled(6, 3, false);
        // actionMask.SetActionEnabled(6, 4, false);
        actionMask.SetActionEnabled(6, 5, false);
        actionMask.SetActionEnabled(6, 6, false);
        actionMask.SetActionEnabled(6, 7, false);
        actionMask.SetActionEnabled(6, 8, false);
        actionMask.SetActionEnabled(6, 9, false);
    }

    private static void DisableWristTranslation(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(0, 0, false);
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

    private static void DisableLargeStep(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(7, 0, false);
        actionMask.SetActionEnabled(7, 6, false);
        actionMask.SetActionEnabled(7, 7, false);
    }
    
    private static void DisablePullArmBack(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(0, 0, false);
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
        // } else if (_resetParams.GetWithDefault("wrist_z_enabled", 0) == 0)
        // {
        //     DisablePullArmBack(actionMask);
        }

        if (!ReadyForFootwork())
        {
            DisableFootwork(actionMask);
        } else if (_resetParams.GetWithDefault("footwork_enabled", 1) == 0)
        {
            DisableFootwork(actionMask);
        // } else if (_resetParams.GetWithDefault("lunge_enabled", 0) == 0)
        // {
        //     DisableLargeStep(actionMask);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        _bladeworkController.DoWristTranslation(
            discreteActions[0], discreteActions[1] - 1, discreteActions[2] - 1, 
            discreteActions[3] == 1, discreteActions[4] - 1);
        _bladeworkController.DoWristRotation(discreteActions[5] - 1, discreteActions[6] - 4);
        
        if (discreteActions[7] - 3 != 0)
        {
            // double layer of check!!! 
            if (_resetParams.GetWithDefault("footwork_enabled", 1) > 0)
            {
                _animator.SetInteger(_stepHash, discreteActions[7] - 3);
            } 
        }
        
        if (gameController.Started())
        {
            AddReward(timeStepReward / MaxStep);
        }
    }
    
}
