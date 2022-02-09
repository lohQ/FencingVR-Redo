using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

// TODO: adjust ikTarget position based on epee actual position!

public class AgentFencer : Agent
{
    public IkTargetController ikTargetController;
    public AvatarController avatarController;
    public Bout bout;
    public FencerColor fencerColor;

    public Transform self, selfEpee, selfEpeeTip, oppEpee, oppEpeeTip;
    public List<Transform> oppBodyTargets;

    private bool _epeeCollided;
    
    public KeyCode additionalKey = KeyCode.None;
    public IkTargetController opponentIkTargetController;
    public bool log;

    public void RegisterWeaponCollision()
    {
        _epeeCollided = true;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        var selfPos = self.position;
        var selfEpeePos = selfEpee.position;
        sensor.AddObservation(selfEpeePos - selfPos);
        sensor.AddObservation(selfEpee.localRotation);
        sensor.AddObservation(selfEpeeTip.position - selfEpee.position);
        sensor.AddObservation(oppEpeeTip.position - selfPos);
        sensor.AddObservation(oppEpeeTip.position - oppEpee.position);
        sensor.AddObservation(_epeeCollided);
        _epeeCollided = false;
        
        foreach (var t in oppBodyTargets)
        {
            sensor.AddObservation(t.position - selfEpeePos);
        }
        
        sensor.AddObservation(ikTargetController.TipDistanceFromOpponent());
        sensor.AddObservation(opponentIkTargetController.TipDistanceFromOpponent());
        
        sensor.AddObservation(bout.points[(int)fencerColor]);
        sensor.AddObservation(bout.GetRemainingTime());
        if (fencerColor == FencerColor.Green)
        {
            sensor.AddObservation(bout.points[(int)FencerColor.Red]);
        }
        else
        {
            sensor.AddObservation(bout.points[(int)FencerColor.Green]);
        }
    }
    
    public override void OnEpisodeBegin()
    {
        bout.SignalStartRound();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        // var continuousActionsOut = actionsOut.ContinuousActions;

        // continuousActionsOut[0] = 0.5f;

        if (additionalKey != KeyCode.None && !Input.GetKey(additionalKey))
        {
            discreteActionsOut[0] = 2;
            discreteActionsOut[1] = 2;
            discreteActionsOut[2] = 2;
            discreteActionsOut[3] = 2;
            discreteActionsOut[4] = 2;
            discreteActionsOut[5] = 2;
            // discreteActionsOut[6] = 1;
            // discreteActionsOut[7] = 1;
            // discreteActionsOut[8] = 1;
            // discreteActionsOut[9] = 0;
            // discreteActionsOut[10] = 1;
            // discreteActionsOut[11] = 1;
            return;
        }

        // 0: translate hand (x axis)
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 0;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 4;
        }
        else
        {
            discreteActionsOut[0] = 2;
        }
        
        // 1: translate hand (y axis)
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[1] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[1] = 0;
        }
        else
        {
            discreteActionsOut[1] = 2;
        }
        
        // 2: translate hand (z axis)
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 4;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 0;
        }
        else
        {
            discreteActionsOut[2] = 2;
        }
        
        // 3: rotate hand (x axis)
        if (Input.GetKey(KeyCode.I))
        {
            discreteActionsOut[3] = 4;
        }
        else if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[3] = 0;
        }
        else
        {
            discreteActionsOut[3] = 2;
        }
        
        // 4: rotate hand (y axis)
        if (Input.GetKey(KeyCode.J))
        {
            discreteActionsOut[4] = 4;
        }
        else if (Input.GetKey(KeyCode.L))
        {
            discreteActionsOut[4] = 0;
        }
        else
        {
            discreteActionsOut[4] = 2;
        }

        // 5: rotate hand (z axis)
        if (Input.GetKey(KeyCode.O))
        {
            discreteActionsOut[5] = 0;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            discreteActionsOut[5] = 4;
        }
        else
        {
            discreteActionsOut[5] = 2;
        }

        // discreteActionsOut[6] = Input.GetKey(KeyCode.RightShift) ? 1 : 0;

        // 7: move head target (along x axis)
        // if (Input.GetKey(KeyCode.H))
        // {
        //     discreteActionsOut[7] = 2;
        // } 
        // else if (Input.GetKey(KeyCode.F))
        // {
        //     discreteActionsOut[7] = 0;
        // }
        // else
        // {
        //     discreteActionsOut[7] = 1;
        // }
        
        // 8: move head target (along y axis)
        // if (Input.GetKey(KeyCode.T))
        // {
        //     discreteActionsOut[8] = 2;
        // } 
        // else if (Input.GetKey(KeyCode.G))
        // {
        //     discreteActionsOut[8] = 0;
        // }
        // else
        // {
        //     discreteActionsOut[8] = 1;
        // }
        
        // step forward / backward
        // if (Input.GetKeyUp(KeyCode.UpArrow))
        // {
        //     discreteActionsOut[9] = 2;
        // } 
        // else if (Input.GetKeyUp(KeyCode.DownArrow))
        // {
        //     discreteActionsOut[9] = 0;
        // }
        // else
        // {
        //     discreteActionsOut[9] = 1;
        // }

        // lunge
        // if (Input.GetKeyUp(KeyCode.Return))
        // {
        //     discreteActionsOut[10] = 1;
        // }
        // else
        // {
        //     discreteActionsOut[10] = 0;
        // }
        
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (bout.withinRound)
        {
            // if (avatarController.curStateInt != -1)
            // {
            //     if (log) Debug.Log("cur state int is not -1, it's " + avatarController.curStateInt + ". Action masked. ");
            //     actionMask.SetActionEnabled(9, 0, false);
            //     actionMask.SetActionEnabled(9, 2, false);
            //     // actionMask.SetActionEnabled(10, 1, false);
            // }
        }
        else
        {
            // if (log) Debug.Log("not within round. Actions masked. ");
            // ik hand
            actionMask.SetActionEnabled(0, 0, false);
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 3, false);
            actionMask.SetActionEnabled(0, 4, false);
            actionMask.SetActionEnabled(1, 0, false);
            actionMask.SetActionEnabled(1, 1, false);
            actionMask.SetActionEnabled(1, 3, false);
            actionMask.SetActionEnabled(1, 4, false);
            actionMask.SetActionEnabled(2, 0, false);
            actionMask.SetActionEnabled(2, 1, false);
            actionMask.SetActionEnabled(2, 3, false);
            actionMask.SetActionEnabled(2, 4, false);

            actionMask.SetActionEnabled(3, 0, false);
            actionMask.SetActionEnabled(3, 1, false);
            actionMask.SetActionEnabled(3, 3, false);
            actionMask.SetActionEnabled(3, 4, false);
            actionMask.SetActionEnabled(4, 0, false);
            actionMask.SetActionEnabled(4, 1, false);
            actionMask.SetActionEnabled(4, 3, false);
            actionMask.SetActionEnabled(4, 4, false);
            actionMask.SetActionEnabled(5, 0, false);
            actionMask.SetActionEnabled(5, 1, false);
            actionMask.SetActionEnabled(5, 3, false);
            actionMask.SetActionEnabled(5, 4, false);

            // ik head
            // actionMask.SetActionEnabled(7, 0, false);
            // actionMask.SetActionEnabled(7, 2, false);
            // actionMask.SetActionEnabled(8, 0, false);
            // actionMask.SetActionEnabled(8, 2, false);

            // animation
            // actionMask.SetActionEnabled(9, 0, false);
            // actionMask.SetActionEnabled(9, 2, false);
            // actionMask.SetActionEnabled(10, 1, false);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        // var speedFactor = Mathf.Clamp(actionBuffers.ContinuousActions[0], 0f, 1f);
        
        // var tipDistanceFromOpp = ikTargetController.TipDistanceFromOpponent();
        // if (tipDistanceFromOpp > 0)
        // {
        //     var reward = Math.Max(70 - tipDistanceFromOpp, 0)/70 * Math.Max(70 - tipDistanceFromOpp, 0)/70 * 0.01f;
        //     AddReward(reward);
        // }
        //
        // var oppTipDistanceFromSelf = opponentIkTargetController.TipDistanceFromOpponent();
        // if (oppTipDistanceFromSelf > 0)
        // {
        //     var reward = Math.Max(70 - oppTipDistanceFromSelf, 0)/70 * Math.Max(70 - oppTipDistanceFromSelf, 0)/70 * 0.01f;
        //     AddReward(-reward);
        // }
        //
        // AddReward(-1f/MaxStep);

        ikTargetController.SetMoveVector(discreteActions[0] - 2, discreteActions[1] - 2, discreteActions[2] - 2);
        ikTargetController.SetRotationToApply(discreteActions[3] - 2, discreteActions[4] - 2, discreteActions[5] - 2);

        // if (log) Debug.Log("discreteActions[0]: " + discreteActions[0]);
        // if (log) Debug.Log("discreteActions[1]: " + discreteActions[1]);
        // if (log) Debug.Log("discreteActions[2]: " + discreteActions[2]);
        // if (log) Debug.Log("discreteActions[3]: " + discreteActions[3]);
        // if (log) Debug.Log("discreteActions[8]: " + discreteActions[8]);
        // if (discreteActions[10] == 1)
        // {
        //     avatarController.Lunge();
        // }
        // else
        // {
        //     avatarController.SetNextStep((discreteActions[9] - 1));
        // }
    }
    
}
