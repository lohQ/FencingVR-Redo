using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetector : MonoBehaviour
{
    // public Bout bout;
    // public AgentFencer agentFencer;
    public AgentFencerSettings agentFencerSettings;
    public HandEffectorController handEffectorController;
    private FencerColor _fencerColor;
    private int _otherFencerBodyLayer, _otherFencerWeaponLayer;

    public float impulseThreshold;  // TODO: check if need to scale the impact
    public float hitAngleThreshold;
    public bool debug;
    private int _frame;

    private void Start()
    {
        _fencerColor = agentFencerSettings.fencerColor;
        var otherFencerColor = PhysicsEnvSettings.GetOther(_fencerColor);
        _otherFencerBodyLayer = LayerMask.NameToLayer(PhysicsEnvSettings.GetFencerBodyLayer(otherFencerColor));
        _otherFencerWeaponLayer = LayerMask.NameToLayer(PhysicsEnvSettings.GetFencerWeaponLayer(otherFencerColor));
        _frame = 0;
    }

    void FixedUpdate()
    {
        _frame += 1;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // if (log) Debug.Log("impulse.magnitude: " + collision.impulse.magnitude);
        // if (collision.impulse.magnitude < impulseThreshold) return;
        //
        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     var contact = collision.GetContact(i);
        //     var hitAngle = Vector3.Angle(-transform.forward, contact.normal);
        //     if (
        //         contact.thisCollider.CompareTag(PhysicsEnvSettings.EpeeTipTag) 
        //         && contact.otherCollider.CompareTag(PhysicsEnvSettings.TargetAreaTag)
        //         && (contact.otherCollider.gameObject.layer == _otherFencerBodyLayer
        //             || contact.otherCollider.gameObject.layer == _otherFencerWeaponLayer)
        //         && hitAngle < hitAngleThreshold)
        //     {
        //         bout.RegisterHit(_fencerColor, contact.point, contact.normal);
        //         break;
        //     }
        // }

        // only register collision on blade
        if (debug) Debug.Log($"[Frame {_frame}] OnCollisionEnter with " + collision.collider.name);
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).thisCollider.name == "Guard") return;
            handEffectorController.RegisterCollision(collision);
            return;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (debug) Debug.Log($"[Frame {_frame}] OnCollisionExit with " + collision.collider.name);
        handEffectorController.UnregisterCollision(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     var contact = collision.GetContact(i);
        //     if (
        //         !contact.thisCollider.CompareTag(PhysicsEnvSettings.EpeeTipTag) 
        //         && !contact.otherCollider.CompareTag(PhysicsEnvSettings.TargetAreaTag)
        //         && contact.otherCollider.gameObject.layer == _otherFencerWeaponLayer)
        //     {
        //         break;
        //     }
        // }

        // only register collision on blade
        if (debug) Debug.Log($"[Frame {_frame}] OnCollisionStay with " + collision.collider.name);
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).thisCollider.name == "Guard") return;
            handEffectorController.RegisterCollision(collision);
            return;
        }
    }
    
}
