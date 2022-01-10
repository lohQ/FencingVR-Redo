using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class PhysicalFollow : MonoBehaviour
{
    public Transform follow;
    private Rigidbody _rb;

    public float frequency = 50f;
    public float damping = 1f;
    private float _kp;
    private float _kd;

    public bool shouldFollow = true;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _kp = (6f*frequency)*(6f*frequency)* 0.25f;
        _kd = 4.5f*frequency*damping;
    }
    
    void FixedUpdate()
    {
        if (shouldFollow)
        {
            // Source: https://digitalopus.ca/site/pd-controllers/
            MoveToPosition_BackwardPD(follow.position);
            RotateToRotation_BackwardPD(follow.rotation);
        }
    }

    void MoveToPosition_BackwardPD(Vector3 pDes)
    {
        float dt = Time.fixedDeltaTime;
        float g = 1 / (1 + _kd * dt + _kp * dt * dt);
        float ksg = _kp * g;
        float kdg = (_kd + _kp * dt) * g;
        Vector3 Pt0 = transform.position;
        Vector3 Vt0 = _rb.velocity;
        Vector3 F = (pDes - Pt0) * ksg + (Vector3.zero - Vt0) * kdg;
        _rb.AddForce (F);
    }

    void RotateToRotation_BackwardPD(Quaternion desiredRotation)
    {
        Quaternion q = desiredRotation * Quaternion.Inverse(transform.rotation);
        // Q can be the-long-rotation-around-the-sphere eg. 350 degrees
        // We want the equivalant short rotation eg. -10 degrees
        // Check if rotation is greater than 190 degees == q.w is negative
        if (q.w < 0)
        {
            // Convert the quaterion to eqivalent "short way around" quaterion
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
        }
        Vector3 x;
        float xMag;
        q.ToAngleAxis (out xMag, out x);
        x.Normalize ();
        x *= Mathf.Deg2Rad;
        float dt = Time.fixedDeltaTime;
        float g = 1 / (1 + _kd * dt + _kp * dt * dt);
        float ksg = _kp * g;
        float kdg = (_kd + _kp * dt) * g;
        Vector3 pidv = ksg * x * xMag - kdg * _rb.angularVelocity;    // added * fixedDeltaTime
        Quaternion rotInertia2World = _rb.inertiaTensorRotation * transform.rotation;
        pidv = Quaternion.Inverse(rotInertia2World) * pidv;
        pidv.Scale(_rb.inertiaTensor);
        pidv = rotInertia2World * pidv;
        _rb.AddTorque(pidv);
    }

}
