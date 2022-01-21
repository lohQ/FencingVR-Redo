using System;
using System.Collections;
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

    public Transform piste, self, selfEpee, selfEpeeTip, oppEpee, oppEpeeTip, oppPosition;

    private bool _epeeCollided;
    private bool _thisFrameEpeeCollided;
    
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
        sensor.AddObservation(selfPos - piste.position);
        sensor.AddObservation(selfEpee.position - selfPos);
        sensor.AddObservation(selfEpee.localRotation);
        sensor.AddObservation(selfEpeeTip.position - selfPos);
        sensor.AddObservation(selfEpeeTip.position - selfEpee.position);
        sensor.AddObservation(oppEpeeTip.position - selfPos);
        sensor.AddObservation(oppEpeeTip.position - oppEpee.position);
        sensor.AddObservation(oppPosition.position - selfPos);
        sensor.AddObservation(_epeeCollided);
        _thisFrameEpeeCollided = _epeeCollided;
        _epeeCollided = false;
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
        // if (!bout.withinRound)
        // {
        //     discreteActionsOut[0] = 1;
        //     discreteActionsOut[1] = 1;
        //     discreteActionsOut[2] = 1;
        //     discreteActionsOut[4] = 1;
        //     discreteActionsOut[5] = 1;
        //     discreteActionsOut[6] = 1;
        //     discreteActionsOut[8] = 1;
        //     discreteActionsOut[9] = 0;
        //     discreteActionsOut[10] = 1;
        //     discreteActionsOut[11] = 1;
        //     return;
        // }
        
        if (additionalKey != KeyCode.None && !Input.GetKey(additionalKey))
        {
            discreteActionsOut[0] = 1;
            discreteActionsOut[1] = 1;
            discreteActionsOut[2] = 1;
            discreteActionsOut[4] = 1;
            discreteActionsOut[5] = 1;
            discreteActionsOut[6] = 1;
            discreteActionsOut[8] = 1;
            // discreteActionsOut[9] = 0;
            // discreteActionsOut[10] = 1;
            // discreteActionsOut[11] = 1;
            return;
        }

        // translate hand (x axis)
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 0;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 2;
        }
        else
        {
            discreteActionsOut[0] = 1;
        }
        
        // translate hand (y axis)
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[1] = 2;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[1] = 0;
        }
        else
        {
            discreteActionsOut[1] = 1;
        }
        
        // translate hand (z axis)
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 2;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 0;
        }
        else
        {
            discreteActionsOut[2] = 1;
        }

        discreteActionsOut[3] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;

        // rotate hand (x axis)
        if (Input.GetKey(KeyCode.I))
        {
            discreteActionsOut[4] = 2;
        }
        else if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[4] = 0;
        }
        else
        {
            discreteActionsOut[4] = 1;
        }
        
        // rotate hand (y axis)
        if (Input.GetKey(KeyCode.J))
        {
            discreteActionsOut[5] = 2;
        }
        else if (Input.GetKey(KeyCode.L))
        {
            discreteActionsOut[5] = 0;
        }
        else
        {
            discreteActionsOut[5] = 1;
        }

        // rotate hand (z axis)
        if (Input.GetKey(KeyCode.O))
        {
            discreteActionsOut[6] = 0;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            discreteActionsOut[6] = 2;
        }
        else
        {
            discreteActionsOut[6] = 1;
        }

        discreteActionsOut[7] = Input.GetKey(KeyCode.RightShift) ? 1 : 0;

        // step forward / backward
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            discreteActionsOut[8] = 2;
        } 
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            discreteActionsOut[8] = 0;
        }
        else
        {
            discreteActionsOut[8] = 1;
        }

        // lunge
        // if (Input.GetKeyUp(KeyCode.Return))
        // {
        //     discreteActionsOut[9] = 1;
        // }
        // else
        // {
        //     discreteActionsOut[9] = 0;
        // }

        // move head target (along x axis)
        // if (Input.GetKey(KeyCode.H))
        // {
        //     discreteActionsOut[10] = 2;
        // } else if (Input.GetKey(KeyCode.F))
        // {
        //     discreteActionsOut[10] = 0;
        // }
        // else
        // {
        //     discreteActionsOut[10] = 1;
        // }
        
        // move head target (along y axis)
        // if (Input.GetKey(KeyCode.T))
        // {
        //     discreteActionsOut[11] = 2;
        // } 
        // else if (Input.GetKey(KeyCode.G))
        // {
        //     discreteActionsOut[11] = 0;
        // }
        // else
        // {
        //     discreteActionsOut[11] = 1;
        // }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (bout.withinRound)
        {
            if (avatarController.curStateInt != -1)
            {
                actionMask.SetActionEnabled(8, 0, false);
                actionMask.SetActionEnabled(8, 2, false);
                // actionMask.SetActionEnabled(9, 1, false);
            }
        }
        else
        {
            // ik hand
            actionMask.SetActionEnabled(0, 0, false);
            actionMask.SetActionEnabled(0, 2, false);
            actionMask.SetActionEnabled(1, 0, false);
            actionMask.SetActionEnabled(1, 2, false);
            actionMask.SetActionEnabled(2, 0, false);
            actionMask.SetActionEnabled(2, 2, false);
            actionMask.SetActionEnabled(3, 1, false);

            actionMask.SetActionEnabled(4, 0, false);
            actionMask.SetActionEnabled(4, 2, false);
            actionMask.SetActionEnabled(5, 0, false);
            actionMask.SetActionEnabled(5, 2, false);
            actionMask.SetActionEnabled(6, 0, false);
            actionMask.SetActionEnabled(6, 2, false);
            actionMask.SetActionEnabled(7, 0, false);

            // animation
            actionMask.SetActionEnabled(8, 0, false);
            actionMask.SetActionEnabled(8, 2, false);
            // actionMask.SetActionEnabled(9, 1, false);
            
            // ik head
            // actionMask.SetActionEnabled(10, 0, false);
            // actionMask.SetActionEnabled(10, 2, false);
            // actionMask.SetActionEnabled(11, 0, false);
            // actionMask.SetActionEnabled(11, 2, false);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        // var tipDistanceFromOpp = ikTargetController.TipDistanceFromOpponent();
        // if (tipDistanceFromOpp > 0)
        // {
        //     var reward = Math.Max(1 - tipDistanceFromOpp, 0) * Math.Max(1 - tipDistanceFromOpp, 0) * 0.01f;
        //     AddReward(reward);
        // }
        
        // if (_thisFrameEpeeCollided && opponentIkTargetController.TipDistanceFromOpponent() < 0)
        // {
        //     AddReward(0.01f);
        // }

        // ikTargetController.useHandAsBasePosition = avatarController.curStateInt == 9;
        ikTargetController.SetMoveVector(discreteActions[0], discreteActions[1], discreteActions[2], discreteActions[3] > 0);
        ikTargetController.SetRotationToApply(discreteActions[4], discreteActions[5], discreteActions[6], discreteActions[7] > 0);
        // ikTargetController.SetHeadTargetMoveVector(discreteActions[10] - 1, discreteActions[11] - 1);

        // Debug.Log("discreteActions[9]: " + discreteActions[9]);
        // if (discreteActions[9] == 1)
        // {
        //     avatarController.Lunge();
        // }
        // else
        // {
            avatarController.SetNextStep((discreteActions[8] - 1));
        // }
    }
    
}
