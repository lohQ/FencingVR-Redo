using System;
using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class IkTargetAccController : MonoBehaviour
{
    public Transform controlTarget;
    
    public float maxVelocity;
    public float maxAcceleration;

    private Vector3 _velocity;
    private Vector3 _acceleration;

    void Start()
    {
        _velocity = Vector3.zero;
        _acceleration = Vector3.zero;
    }

    public void SetAccelerationVector(float x, float y, float z)
    {
        _acceleration = new Vector3(x, y, z);
        if (_acceleration.magnitude > maxAcceleration)
        {
            _acceleration = maxAcceleration * _acceleration.normalized;
        }
    }

    void Update()
    {
        float x = 0;
        float y = 0;
        float z = 0;
        if (Input.GetKey(KeyCode.A)) x -= 1;
        if (Input.GetKey(KeyCode.D)) x += 1;
        if (Input.GetKey(KeyCode.S)) y -= 1;
        if (Input.GetKey(KeyCode.W)) y += 1;
        if (Input.GetKey(KeyCode.Q)) z -= 1;
        if (Input.GetKey(KeyCode.E)) z += 1;
        SetAccelerationVector(x, y, z);

        if (Input.GetKey(KeyCode.LeftShift)) _acceleration /= 2;
        
        var newVelocity = _velocity + _acceleration;
        if (newVelocity.magnitude > maxVelocity)
        {
            newVelocity = maxVelocity * newVelocity.normalized;
        }

        _velocity = newVelocity;
        _acceleration = Vector3.zero;

        controlTarget.position += _velocity;
    }
}
