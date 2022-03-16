using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NewAgentFencer : Agent
{
    public GameController gameController;
    private BladeworkController _bladeworkController;
    private FollowFootwork _followFootwork;
    private Animator _animator;

    private int _stepHash;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _bladeworkController = GetComponent<BladeworkController>();
        _followFootwork = GetComponent<FollowFootwork>();
        Debug.Log($"_animator: {_animator}");
        Debug.Log($"_bladeworkController: {_bladeworkController}");
        Debug.Log($"_followFootwork: {_followFootwork}");
        
        _stepHash = Animator.StringToHash("step");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = Input.GetKey(KeyCode.F) ? 0 : 1;
        discreteActionsOut[1] = Input.GetKey(KeyCode.A) ? 2 : Input.GetKey(KeyCode.D) ? 0 : 1;
        discreteActionsOut[2] = Input.GetKey(KeyCode.W) ? 2 : Input.GetKey(KeyCode.S) ? 0 : 1;
        discreteActionsOut[3] = Input.GetKey(KeyCode.E) ? 1 : 0;
        
        if (Input.GetKey(KeyCode.R))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[4] = 1;  // supp -1
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                discreteActionsOut[4] = 2;  // supp +1
            }
            else
            {
                discreteActionsOut[4] = 0;  // supp 0
            }
            
            if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                discreteActionsOut[5] = 0;      // rotate counterclockwise
            }
            else if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                discreteActionsOut[5] = 1;      // rotate clockwise
            }
            else if (Input.GetKeyUp(KeyCode.Alpha5))
            {
                discreteActionsOut[5] = 2;      // point to center
            }
            else if (Input.GetKeyUp(KeyCode.Alpha6))
            {
                discreteActionsOut[5] = 3;      // world point to 0
            }
            else if (Input.GetKeyUp(KeyCode.Alpha7))
            {
                discreteActionsOut[5] = 4;      // world point to 1
            }
            else if (Input.GetKeyUp(KeyCode.Alpha8))
            {
                discreteActionsOut[5] = 5;      // world point to 2
            }
            else if (Input.GetKeyUp(KeyCode.Alpha9))
            {
                discreteActionsOut[5] = 6;      // world point to 3
            }
            else if (Input.GetKeyUp(KeyCode.Alpha0))
            {
                discreteActionsOut[5] = 7;      // world point to 4
            }
            else
            {
                discreteActionsOut[5] = 2;      // point to center
            }
        }
        else
        {
            discreteActionsOut[4] = 0;  // supp 0
            discreteActionsOut[5] = 2;  // point to center
        }

        if (Input.GetKey(KeyCode.P))
        {
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                discreteActionsOut[6] = 1;  // parry 4
            }
            else if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                discreteActionsOut[6] = 2;  // parry 6
            }
            else if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                discreteActionsOut[6] = 3;  // parry 7
            }
            else if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                discreteActionsOut[6] = 4;  // parry 8
            }
            else
            {
                discreteActionsOut[6] = 0;  // supp 0
            }
        }
        else
        {
            discreteActionsOut[6] = 0;  // supp 0
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[7] = 4;  // small step
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[7] = 6;  // large step
            } else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[7] = 7;  // lunge
            }
            else 
            {
                discreteActionsOut[7] = 5;  // regular step
            }
        } 
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[7] = 2;  // small step
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[7] = 0;  // small step
            }
            else
            {
                discreteActionsOut[7] = 1;  // regular step
            }
        }
        else
        {
            discreteActionsOut[7] = 3;
        }
        
        if (_followFootwork.BladeworkDisabled())
        {
            discreteActionsOut[0] = 1;
            discreteActionsOut[1] = 1;
            discreteActionsOut[2] = 1;
            discreteActionsOut[3] = 0;
            discreteActionsOut[4] = 1;
            discreteActionsOut[5] = 2;
            discreteActionsOut[6] = 0;
        }
        else
        {
            if (!_bladeworkController.CanRotateWrist())
            {
                discreteActionsOut[4] = 1;
                discreteActionsOut[5] = 2;
                discreteActionsOut[6] = 0;
            }
            if (!_bladeworkController.CanTranslateWrist())
            {
                discreteActionsOut[0] = 1;
                discreteActionsOut[1] = 1;
                discreteActionsOut[2] = 1;
                discreteActionsOut[3] = 0;
                discreteActionsOut[6] = 0;
            }
        }

        if (!ReadyForFootwork())
        {
            discreteActionsOut[7] = 3;
        }
    }

    private bool ReadyForFootwork()
    {
        // return _animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde");
        return _followFootwork.ReadyForNewFootwork();
    }

    private static void DisableWristRotation(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(4, 0, false);
        actionMask.SetActionEnabled(4, 2, false);
            
        // depends on how many worldPointToTargets are there
        actionMask.SetActionEnabled(5, 1, false);
        actionMask.SetActionEnabled(5, 2, false);
        actionMask.SetActionEnabled(5, 3, false);
        actionMask.SetActionEnabled(5, 4, false);

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

        actionMask.SetActionEnabled(6, 1, false);
        actionMask.SetActionEnabled(6, 2, false);
        actionMask.SetActionEnabled(6, 3, false);
        actionMask.SetActionEnabled(6, 4, false);
    }

    private static void DisableFootwork(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(7, 0, false);
        actionMask.SetActionEnabled(7, 1, false);
        actionMask.SetActionEnabled(7, 2, false);
        // 3 is do nothing
        actionMask.SetActionEnabled(7, 4, false);
        actionMask.SetActionEnabled(7, 5, false);
        actionMask.SetActionEnabled(7, 6, false);
        actionMask.SetActionEnabled(7, 7, false);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (_followFootwork.BladeworkDisabled())
        {
            DisableWristRotation(actionMask);
            DisableWristTranslation(actionMask);
        }
        else
        {
            if (!_bladeworkController.CanRotateWrist())
            {
                DisableWristRotation(actionMask);
            }
            if (!_bladeworkController.CanTranslateWrist())
            {
                DisableWristTranslation(actionMask);
            }
        }

        if (!ReadyForFootwork())
        {
            DisableFootwork(actionMask);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        _bladeworkController.DoWristTranslation(discreteActions[0], discreteActions[1] - 1, discreteActions[2] - 1, discreteActions[3] == 1);
        _bladeworkController.DoWristRotation(discreteActions[4] - 1, discreteActions[5] - 2);
        _bladeworkController.DoParry(discreteActions[6]);

        if (discreteActions[7] - 3 != 0)
        {
            _animator.SetInteger(_stepHash, discreteActions[7] - 3);
        }
    }
    
}
