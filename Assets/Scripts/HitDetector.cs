using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetector : MonoBehaviour
{
    public string epeeTipTag = "Epee Tip";
    public string targetAreaTag = "Target Area";
    public string otherFencerBodyLayer = "Player 2 Body Layer";
    public string otherFencerWeaponLayer = "Player 2 Weapon Layer";
    public float impulseThreshold;
    public float hitAngleThreshold;

    public AgentFencer agentFencer;
    
    public FencerColor fencerColor;
    public Bout bout;

    public bool log;
    
    private void OnCollisionEnter(Collision collision)
    {
        // if (log) Debug.Log("on collision enter, contactCount: " + collision.contactCount);

        // detect hit
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            // Debug.DrawRay(contact.point, contact.normal, Color.blue, 5f);
            // if (log) Debug.Log("contact.thisCollider.tag: " + contact.thisCollider.tag);
            // if (log) Debug.Log("contact.otherCollider.tag: " + contact.otherCollider.tag);
            // if (log) Debug.Log("contact.otherCollider.gameObject.layer: " + LayerMask.LayerToName(contact.otherCollider.gameObject.layer));
            // if (log) Debug.Log("collision.impulse.magnitude: " + collision.impulse.magnitude);
            var hitAngle = Vector3.Angle(-transform.forward, contact.normal);
            if (log) Debug.Log("hitAngle: " + hitAngle);
            if (
                contact.thisCollider.CompareTag(epeeTipTag) 
                && contact.otherCollider.CompareTag(targetAreaTag)
                && (contact.otherCollider.gameObject.layer == LayerMask.NameToLayer(otherFencerBodyLayer)
                    || contact.otherCollider.gameObject.layer == LayerMask.NameToLayer(otherFencerWeaponLayer))
                && collision.impulse.magnitude > impulseThreshold
                && hitAngle < hitAngleThreshold)
            {
                bout.RegisterHit(fencerColor);
                break;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (
                !contact.thisCollider.CompareTag(epeeTipTag) 
                && !contact.otherCollider.CompareTag(targetAreaTag)
                && contact.otherCollider.gameObject.layer == LayerMask.NameToLayer(otherFencerWeaponLayer))
            {
                agentFencer.RegisterWeaponCollision();
                break;
            }
        }
    }
    
    
    
    
}
