using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewHitDetector : MonoBehaviour
{
    public FollowFootwork followFootwork;

    private void OnCollisionStay(Collision collision)
    {
        var midContact = collision.contacts[collision.contactCount / 2];
        Debug.DrawRay(midContact.point, collision.impulse, Color.blue, 0.1f);
        followFootwork.RegisterCollision();
    }

}
