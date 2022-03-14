using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewHitDetector : MonoBehaviour
{
    public FollowFootwork followFootwork;

    private void OnCollisionStay()
    {
        followFootwork.RegisterCollision();
    }
}
