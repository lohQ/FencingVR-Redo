using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewHitDetector : MonoBehaviour
{
    public FollowFootwork followFootwork;
    public float impulseThreshold;
    public float hitAngleThreshold;

    private void OnCollisionStay(Collision collision)
    {
        var midContact = collision.contacts[collision.contactCount / 2];
        Debug.DrawRay(midContact.point, collision.impulse, Color.blue, 0.1f);
        followFootwork.RegisterCollision();
        
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
    }

}
