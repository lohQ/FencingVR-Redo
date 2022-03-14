using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewHitDetector : MonoBehaviour
{
    public MoveInAdvance moveInAdvance;

    private void OnCollisionStay()
    {
        moveInAdvance.RegisterCollision();
    }
}
