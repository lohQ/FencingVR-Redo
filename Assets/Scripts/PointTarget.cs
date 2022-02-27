using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointTarget : MonoBehaviour
{
    // y as forward axis (forgotten why set it like that and now am lazy to change)

    public float forwardOffset = 0.1f;
    public float upRadius;      // +z
    public float downRadius;    // -z
    public float leftRadius;    // +x
    public float rightRadius;   // -x
    private Vector3 _center;

    private void Start()
    {
        _center = new Vector3(0, forwardOffset, 0);
    }

    public Vector3 GetPosition(int index)
    {
        Vector3 localPosition;
        var sqrt2 = Mathf.Sqrt(2);
        switch (index)
        {
            case 0:
                localPosition = _center;
                break;
            case 1:
                localPosition = _center + upRadius * Vector3.forward;
                break;
            case 2:
                localPosition = _center + upRadius / sqrt2 * Vector3.forward + rightRadius / sqrt2 * Vector3.right;
                break;
            case 3:
                localPosition = _center + rightRadius * Vector3.right;
                break;
            case 4: 
                localPosition = _center - upRadius / sqrt2 * Vector3.forward + rightRadius / sqrt2 * Vector3.right;
                break;
            case 5:
                localPosition = _center - upRadius * Vector3.forward;
                break;
            case 6:
                localPosition = _center - upRadius / sqrt2 * Vector3.forward - rightRadius / sqrt2 * Vector3.right;
                break;
            case 7:
                localPosition = _center - rightRadius * Vector3.right;
                break;
            case 8:
                localPosition = _center + upRadius / sqrt2 * Vector3.forward - rightRadius / sqrt2 * Vector3.right;
                break;
            default:
                localPosition = _center;
                break;
        }

        return transform.TransformPoint(localPosition);
    }

    public Tuple<Vector3, bool> CapPointTarget(Vector3 desiredPointTarget)
    {
        // transform to local space
        var localDesiredPointTarget = transform.InverseTransformPoint(desiredPointTarget);
        // Debug.Log("localDesiredPointTarget: " + localDesiredPointTarget);
        
        // scale to end of cone with y of 'forwardOffset'
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
            var capped = _center + (new Vector3(scaledPointTarget.x, 0, scaledPointTarget.z).normalized * maxRadius);
            return Tuple.Create(transform.TransformPoint(capped), true);
        }

        // no cap, just return the passed in target
        return Tuple.Create(desiredPointTarget, false);
    }
    
}
