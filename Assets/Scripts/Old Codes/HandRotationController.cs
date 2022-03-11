using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class HandRotationController : MonoBehaviour
{
    // orientation of its transform is NOT set to same as wrist's neutral orientation
    // z as suppination axis, x and y as pointTo axis
    // child of elbow, will follow the wrist around
    // don't ever rotate this transform!

    public Transform wrist;
    public Transform epeeTip;
    public Transform wristTarget;
    public Transform epeeTarget;
    
    public float forwardOffset = 0.1f;
    public float upRadius;      // +z
    public float downRadius;    // -z
    public float leftRadius;    // +x
    public float rightRadius;   // -x
    public float suppinationMax;    // ?y
    public float suppinationMin;    // ?y
    public float avgRotationDegreesMin;
    public float avgRotationDegreesMax;
    
    private Vector3 _localToCenterOffset;

    public void SetCenter()
    {
        _localToCenterOffset = transform.InverseTransformPoint(epeeTip.position);
        Debug.Log("Set _localToCenterOffset.");
    }

    public float GetAvgDegrees(int index)
    {
        return avgRotationDegreesMin + (avgRotationDegreesMax - avgRotationDegreesMin) * index / 2;
    }
    
    public Vector3 GetPosition(int index)
    {
        Vector3 pointTo;
        var sqrt2 = Mathf.Sqrt(2);
        var upVector = upRadius * transform.forward;
        var downVector = downRadius * -transform.forward;
        var leftVector = leftRadius * transform.right;
        var rightVector = rightRadius * -transform.right;
        var _center = transform.TransformPoint(_localToCenterOffset);

        switch (index)
        {
            case 0:
                pointTo = _center;
                break;
            case 1:
                pointTo = _center + upVector;
                break;
            case 2:
                pointTo = _center + upVector / sqrt2 + rightVector / sqrt2;
                break;
            case 3:
                pointTo = _center + rightVector;
                break;
            case 4: 
                pointTo = _center + downVector / sqrt2 + rightVector / sqrt2;
                break;
            case 5:
                pointTo = _center + downVector;
                break;
            case 6:
                pointTo = _center + downVector / sqrt2 + leftVector / sqrt2;
                break;
            case 7:
                pointTo = _center + leftVector;
                break;
            case 8:
                pointTo = _center + upVector / sqrt2 + leftVector / sqrt2;
                break;
            default:
                pointTo = _center;
                break;
        }

        return pointTo;
    }

    public float GetSuppination(int index)
    {
        // index: [0,8] -> [-4,4]
        return (suppinationMax + suppinationMin) / 2 + (suppinationMax - suppinationMin) / 8 * (index-4);
    }

    public int iterationCount = 5;
    
    public Quaternion GetRotationToApply(Vector3 pointToTarget, float suppination, bool debug)
    {
        // assumption: pointToTarget and suppination are checked already

        // TODO: find out the way to have correct suppination after pointTo rotation. 
        // why does the angle switches between negative and positive?
        // try iteratively do pointTo with no rotation around y axis?
        // try allow pointTo to deviate due to suppination

        var from0 = epeeTip.position - wristTarget.position;
        var to0 = pointToTarget - wristTarget.position;
        if (debug) Debug.DrawRay(wristTarget.position, from0, Color.blue);
        if (debug) Debug.DrawRay(wristTarget.position, to0, Color.red);
        
        var pointToRotation = Quaternion.FromToRotation(from0, to0);
        var r1SuppinationAngle = Vector3.SignedAngle(transform.right, pointToRotation * wristTarget.right, transform.up);
        if (debug) Debug.Log($"pointToRotation: {pointToRotation.eulerAngles}");

        Quaternion suppinationRotation = Quaternion.identity;
        Quaternion combinedRotation = Quaternion.identity;

        for (int i = 0; i < iterationCount; i++)
        {
            var curSuppinationAngle = Vector3.SignedAngle(transform.right, pointToRotation * wristTarget.right, transform.up);
            suppinationRotation = Quaternion.AngleAxis(suppination - curSuppinationAngle, transform.up);
            // var r2SuppinationAngle = Vector3.SignedAngle(transform.right, suppinationRotation * wristTarget.right, transform.up);
            // if (debug) Debug.DrawRay(wristTarget.position, transform.right * 50, Color.red);
            // if (debug) Debug.DrawRay(wristTarget.position, wristTarget.right * 50, Color.magenta);
            // if (debug) Debug.Log($"suppinationRotation: {suppinationRotation.eulerAngles}");
            
            combinedRotation = pointToRotation * suppinationRotation;   // order of this still not sure
            var r3SuppinationAngle = Vector3.SignedAngle(transform.right, combinedRotation * wristTarget.right, transform.up);
            // if (debug) Debug.Log($"cur: {curSuppinationAngle}, r1: {r1SuppinationAngle}, r2: {r2SuppinationAngle}, r3: {r3SuppinationAngle}");
            // if (debug) Debug.Log($"combinedRotation: {combinedRotation.eulerAngles}");

            var to = combinedRotation * from0;
            var right = combinedRotation * wristTarget.right;
            Debug.DrawRay(wristTarget.position, to, Color.green);
            Debug.DrawRay(wristTarget.position, right, Color.magenta);
            pointToRotation = Quaternion.FromToRotation(to, to0) * pointToRotation;
            // if (debug) Debug.Log($"pointToRotation: {pointToRotation.eulerAngles}");
        }

        return pointToRotation * suppinationRotation;
    }
    
    public Tuple<Vector3, bool> CapPointTarget(Vector3 desiredPointTarget)
    {
        // transform to local space
        var localDesiredPointTarget = transform.InverseTransformPoint(desiredPointTarget);
        // Debug.Log("localDesiredPointTarget: " + localDesiredPointTarget);
        
        // scale to end of cone with y of 'forwardOffset'
        // WAIT IS FORWARD OFFSET IN LOCAL SPACE?
        var scaledPointTarget = (forwardOffset / localDesiredPointTarget.y) * localDesiredPointTarget;

        // same level as the defined radius, can compare now
        // var diffFromCenter = scaledPointTarget - _center;
        var maxX = scaledPointTarget.x > 0 ? leftRadius : rightRadius;
        var maxZ = scaledPointTarget.z > 0 ? upRadius : downRadius;
        var desiredRadiusFromCenter = Mathf.Sqrt(scaledPointTarget.x * scaledPointTarget.x + scaledPointTarget.z * scaledPointTarget.z);
        var maxRadius = Mathf.Sqrt(maxX * maxX + maxZ * maxZ);
        
        // cap & transform back to global space
        if (desiredRadiusFromCenter > maxRadius)
        {
            Debug.Log($"desiredRadiusFromCenter: {desiredRadiusFromCenter}, maxRadius: {maxRadius}");
            var _center = transform.TransformPoint(_localToCenterOffset);
            var capped = _center + (new Vector3(scaledPointTarget.x, 0, scaledPointTarget.z).normalized * maxRadius);
            return Tuple.Create(transform.TransformPoint(capped), true);
        }

        // no cap, just return the passed in target
        return Tuple.Create(desiredPointTarget, false);
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < 9; i++)
        {
            var pointTo = GetPosition(i);
            Gizmos.DrawWireSphere(pointTo, 3f);
        }

        // var pos = transform.position;
        // Debug.DrawRay(pos, transform.forward * 50, Color.blue);
        // Debug.DrawRay(pos, transform.right, Color.red);
        // Debug.DrawRay(pos, transform.up, Color.green);
    }
}
