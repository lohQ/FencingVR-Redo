using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineAim : MonoBehaviour
{
    public Transform center;
    public Transform frontFoot;
    public Transform backFoot;

    public float relativeAngleFromBackToFront = 0.3f;
    public float yOffset = -0.1f;

    private float _backToFrontTheta;
    private float _xzRadius;

    void Start()
    {
        _xzRadius = (float)Math.Sqrt(Math.Pow((transform.position.x - center.position.x), 2) +
                                     Math.Pow((transform.position.z - center.position.z), 2));
    }
    
    public void SetPosition()
    {
        var centerPos = center.position;
        var centerXZPos = new Vector3(centerPos.x, 0, centerPos.z);
        var frontFootXZPos = new Vector3(frontFoot.position.x, 0, frontFoot.position.z);
        var backFootXZPos = new Vector3(backFoot.position.x, 0, backFoot.position.z);
        
        var toFrontFoot = (frontFootXZPos - centerXZPos).normalized;
        var toBackFoot = (backFootXZPos - centerXZPos).normalized;
        var crossProduct = Vector3.Cross(toFrontFoot, toBackFoot);
        // (180 - theta) because Asin only returns acute angle, but we are fairly sure that in En Garde position the angle should be obtuse
        _backToFrontTheta = 180 - (float) (Math.Asin(crossProduct.magnitude) / Math.PI * 180);
        // Debug.Log("_backToFrontTheta: " + _backToFrontTheta);
        
        var backToAimTheta = _backToFrontTheta * relativeAngleFromBackToFront;
        // Debug.Log("backToAimTheta: " + backToAimTheta);
        var backAsForwardRotation = Quaternion.LookRotation(backFootXZPos - centerXZPos);
        var frontAsForwardRotation = Quaternion.LookRotation(frontFootXZPos - centerXZPos);
        var aimAsForwardRotation = Quaternion.RotateTowards(
            backAsForwardRotation, frontAsForwardRotation, backToAimTheta);
        var aimAsForwardVector = (aimAsForwardRotation * Vector3.forward).normalized;
        var newAimPosition = centerPos + aimAsForwardVector * _xzRadius + Vector3.up * yOffset;
        transform.position = newAimPosition;
    }
}
