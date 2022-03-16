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
    public AvatarController avatarController;
    public HandEffectorController handEffectorController;
    [SerializeReference]
    public GameController gameController;
    public FencerColor fencerColor;

    public Transform self, selfEpee, selfEpeeTip;
    // public Transform oppEpee, oppEpeeTip;
    // public List<Transform> oppBodyTargets;
    
    public KeyCode additionalKey = KeyCode.None;
    // public HandEffectorController opponentHandController;
    public bool log;

    private BufferSensorComponent _bufferSensor;
    
    void Start()
    {
        _bufferSensor = GetComponent<BufferSensorComponent>();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // TODO: determine the observations later
        // var selfPos = self.position;
        var selfEpeePos = selfEpee.position;
        sensor.AddObservation(self.InverseTransformPoint(selfEpeePos)); // divide by arm length?
        sensor.AddObservation(selfEpee.localRotation);
        sensor.AddObservation(self.InverseTransformVector(selfEpeeTip.position - selfEpee.position));
        // sensor.AddObservation(self.InverseTransformPoint(oppEpeeTip.position)); // divide by piste length?
        // sensor.AddObservation(self.InverseTransformVector(oppEpeeTip.position - oppEpee.position));

        // var selfEpeeTipPos = selfEpeeTip.position;
        // foreach (var t in oppBodyTargets)
        // {
        //     sensor.AddObservation(self.InverseTransformVector(t.position - selfEpeeTipPos));
        // }
        
        // sensor.AddObservation(handController.TipDistanceFromOpponent());
        // sensor.AddObservation(opponentHandController.TipDistanceFromOpponent());
        //
        // var collisionObservations = handController.GetCollisionFloats();
        // for (int i = 0; i < collisionObservations.Length / 6; i++)
        // {
        //     _bufferSensor.AppendObservation(collisionObservations[(i * 6)..(i * 6 + 6)]);
        // }
    }
    
    public override void OnEpisodeBegin()
    {
        gameController.StartGame(this);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        if (additionalKey != KeyCode.None && !Input.GetKey(additionalKey))
        {
            discreteActionsOut[0] = 3;
            discreteActionsOut[1] = 0;
            discreteActionsOut[2] = 0;
            discreteActionsOut[3] = 0;
            return;
        }

        // step forward / backward
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[0] = 4;
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[0] = 6;
            } else if (Input.GetKey(KeyCode.Alpha0))
            {
                discreteActionsOut[0] = 7;  // lunge
            }
            else
            {
                discreteActionsOut[0] = 5;
            }
        } 
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[0] = 2;
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[0] = 0;
            }
            else
            {
                discreteActionsOut[0] = 1;
            }
        }
        else
        {
            discreteActionsOut[0] = 3;
        }

        // move arm towards target
        if (Input.GetKey(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[1] = 1;
            } else if (Input.GetKey(KeyCode.Alpha2))
            {
                discreteActionsOut[1] = 2;
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[1] = 3;
            } else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[1] = 4;
            } else if (Input.GetKey(KeyCode.Alpha5))
            {
                discreteActionsOut[1] = 5;
            }
        }
        else
        {
            discreteActionsOut[1] = 0;
        }
        
        // rotate arm to point at target
        if (Input.GetKey(KeyCode.Backspace))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[2] = 1;
            } else if (Input.GetKey(KeyCode.Alpha2))
            {
                discreteActionsOut[2] = 2;
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[2] = 3;
            } else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[2] = 4;
            } else if (Input.GetKey(KeyCode.Alpha5))
            {
                discreteActionsOut[2] = 5;
            } else if (Input.GetKey(KeyCode.Alpha6))
            {
                discreteActionsOut[2] = 6;
            } else if (Input.GetKey(KeyCode.Alpha7))
            {
                discreteActionsOut[2] = 7;
            } else if (Input.GetKey(KeyCode.Alpha8))
            {
                discreteActionsOut[2] = 8;
            }
            else
            {
                discreteActionsOut[2] = 0;
            }
        }
        else
        {
            discreteActionsOut[2] = 0;
        }

        // hand pronation / suppination (palm facing up / down)
        if (Input.GetKey(KeyCode.Backslash))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                discreteActionsOut[3] = 1;
            } else if (Input.GetKey(KeyCode.Alpha2))
            {
                discreteActionsOut[3] = 2;
            } else if (Input.GetKey(KeyCode.Alpha3))
            {
                discreteActionsOut[3] = 3;
            } else if (Input.GetKey(KeyCode.Alpha4))
            {
                discreteActionsOut[3] = 4;
            }
            else
            {
                discreteActionsOut[3] = 0;
            }
        }
        else
        {
            discreteActionsOut[3] = 0;
        }
        
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (gameController.Started())
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
            actionMask.SetActionEnabled(0, 0, false);
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 2, false);
            actionMask.SetActionEnabled(0, 4, false);
            actionMask.SetActionEnabled(0, 5, false);
            actionMask.SetActionEnabled(0, 6, false);
            actionMask.SetActionEnabled(0, 7, false);

            actionMask.SetActionEnabled(1, 1, false);
            actionMask.SetActionEnabled(1, 2, false);
            actionMask.SetActionEnabled(1, 3, false);
            actionMask.SetActionEnabled(1, 4, false);
            actionMask.SetActionEnabled(1, 5, false);

            actionMask.SetActionEnabled(2, 1, false);
            actionMask.SetActionEnabled(2, 2, false);
            actionMask.SetActionEnabled(2, 3, false);
            actionMask.SetActionEnabled(2, 4, false);
            actionMask.SetActionEnabled(2, 5, false);
            actionMask.SetActionEnabled(2, 6, false);
            actionMask.SetActionEnabled(2, 7, false);
            actionMask.SetActionEnabled(2, 8, false);

            actionMask.SetActionEnabled(3, 1, false);
            actionMask.SetActionEnabled(3, 2, false);
            actionMask.SetActionEnabled(3, 3, false);
            actionMask.SetActionEnabled(3, 4, false);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var discreteActions = actionBuffers.DiscreteActions;
        avatarController.SetNextStep((discreteActions[0] - 3));
        handEffectorController.SetMoveTarget(discreteActions[1]);
        handEffectorController.SetPointTarget(discreteActions[2]);
        handEffectorController.SetPalmTarget(discreteActions[3]);
    }
    
}
