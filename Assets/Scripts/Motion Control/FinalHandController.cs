using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalHandController : MonoBehaviour
{
    [Header("References")]
    public Transform neutralAxes;
    public Transform targetWristOnEpeeAxes;
    public Transform epeeTip;
    public Transform targetEpeeTip;
    public List<Transform> pointToTargets;  // index 0 must be 'Center PointTo'
    public Transform moveTargetRoot;
    public Transform moveTarget;
    public Transform internalPointToTarget;

    [Header("Control Target")]
    public Transform externalPointToTarget; // actually is controlled by BladeworkController
    public Transform epeeTarget;

    [Header("Control Parameters")] 
    public float suppinationMax;
    public float suppinationMin;
    public float rotationRadius;
    public int maxIteration;

    [Header("Tweaked (kinda static) Parameters")]
    public float angularVelocity;
    public float recheckPerDegrees;
    public float maxRotationError;
    public float velocity;
    public float maxTranslationError;
    public float moveTargetDistance;
    public Vector3 moveTargetDistanceOffset;

    [Header("General")]
    public bool debug;
    public bool showEpeePath;
    
    private bool _rotating;
    private bool _moving;
    private Vector3 _prevEpeeTipPos;
    private int _supIndex;
    private float _curRotationAngle;
    
    private void Awake()
    {
        _prevEpeeTipPos = epeeTip.position;
        _rotating = false;
        _moving = false;
        _supIndex = 0;
        InitializeRotationRadius();
        _enabled = true;
    }

    private void InitializeRotationRadius()
    {
        var centerLocalPos = pointToTargets[0].localPosition;
        var pointTosParent = pointToTargets[0].parent.parent;  // wrist clone
        var localMagnitude = centerLocalPos.magnitude;
        var localScale = pointTosParent.InverseTransformVector(Vector3.up).magnitude;
        var offsetAt45Deg = Mathf.Sqrt(rotationRadius * rotationRadius / 2);

        var upVector = rotationRadius * localScale * pointTosParent.up;
        var rightVector = rotationRadius * localScale * pointTosParent.right;
        var halfUpVector = offsetAt45Deg * localScale * pointTosParent.up;
        var halfRightVector = offsetAt45Deg * localScale * pointTosParent.right;
        pointToTargets[1].localPosition = (centerLocalPos + rightVector).normalized * localMagnitude;
        pointToTargets[2].localPosition = (centerLocalPos + halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[3].localPosition = (centerLocalPos - upVector).normalized * localMagnitude;
        pointToTargets[4].localPosition = (centerLocalPos - halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[5].localPosition = (centerLocalPos - rightVector).normalized * localMagnitude;
        pointToTargets[6].localPosition = (centerLocalPos - halfRightVector + halfUpVector).normalized * localMagnitude;
        pointToTargets[7].localPosition = (centerLocalPos + upVector).normalized * localMagnitude;
        pointToTargets[8].localPosition = (centerLocalPos + halfRightVector + halfUpVector).normalized * localMagnitude;

        // with this small radius will have less oscillation when doing wrist rotation
        upVector /= 2;
        rightVector /= 2;
        halfUpVector /= 2;
        halfRightVector /= 2;
        pointToTargets[9].localPosition = (centerLocalPos + rightVector).normalized * localMagnitude;
        pointToTargets[10].localPosition = (centerLocalPos + halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[11].localPosition = (centerLocalPos - upVector).normalized * localMagnitude;
        pointToTargets[12].localPosition = (centerLocalPos - halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[13].localPosition = (centerLocalPos - rightVector).normalized * localMagnitude;
        pointToTargets[14].localPosition = (centerLocalPos - halfRightVector + halfUpVector).normalized * localMagnitude;
        pointToTargets[15].localPosition = (centerLocalPos + upVector).normalized * localMagnitude;
        pointToTargets[16].localPosition = (centerLocalPos + halfRightVector + halfUpVector).normalized * localMagnitude;
    }
    
    private float GetNewSuppination(Quaternion newWristRotation)
    {
        var neutralRotation = neutralAxes.rotation;

        var aligningRotation = Quaternion.FromToRotation(
            newWristRotation * Vector3.forward, 
            neutralRotation * Vector3.forward);
        var alignedNewRotation = aligningRotation * newWristRotation;
        var angle = Vector3.SignedAngle(
            neutralRotation * Vector3.right, 
            alignedNewRotation * Vector3.right, 
            neutralAxes.forward);

        if (debug)
        {
            Debug.DrawRay(neutralAxes.position, neutralRotation * Vector3.right * 50, Color.red, 1f);
            Debug.DrawRay(neutralAxes.position, alignedNewRotation * Vector3.right * 50, Color.magenta, 1f);
        }

        return angle;
    }

    private Quaternion GetSuppinationRotation(float newSuppinationValue)
    {
        var neutralRotation = neutralAxes.rotation;
        var currentRotation = targetWristOnEpeeAxes.rotation;

        // align neutral axes on actual axes, get suppination angle
        var aligningRotation = Quaternion.FromToRotation(
            neutralRotation * Vector3.forward, 
            currentRotation * Vector3.forward);
        var alignedNeutralRotation = aligningRotation * neutralRotation;
        var suppinationAngle = Vector3.SignedAngle(
            alignedNeutralRotation * Vector3.right, 
            currentRotation * Vector3.right, 
            targetWristOnEpeeAxes.forward);
        
        if (newSuppinationValue - suppinationAngle < 0.01f && newSuppinationValue - suppinationAngle > -0.01f)
        {
            return Quaternion.identity;
        }
        // not sure why but apparantly it only rotates half of the angle now...
        // will result in some errors in final result but still acceptable
        var suppinationRotation = Quaternion.AngleAxis(
            (newSuppinationValue - suppinationAngle) * 2,
            targetWristOnEpeeAxes.forward);
        
        if (debug)
        {
            var neutralAxesPos = neutralAxes.position;
            Debug.DrawRay(neutralAxesPos, neutralRotation * Vector3.right * 50, Color.red);
            Debug.DrawRay(neutralAxesPos, currentRotation * Vector3.right * 50, Color.red);
            Debug.DrawRay(neutralAxesPos, alignedNeutralRotation * Vector3.right * 50, Color.magenta);
            Debug.DrawRay(neutralAxesPos, suppinationRotation * currentRotation * Vector3.right * 50, Color.green);

            // var newSuppinationAngle = Vector3.SignedAngle(
            //     alignedNeutralRotation * Vector3.right, 
            //     suppinationRotation * actualRotation * Vector3.right, 
            //     wristOnEpeeAxes.forward);
            // Debug.Log($"suppinationAngle: {suppinationAngle}, newSuppinationAngle: {newSuppinationAngle}");
        }

        return suppinationRotation;
    }
    
    private Quaternion GetLookAtRotation(Vector3 targetPos, Quaternion suppinationRotation)
    {
        // as wristOnEpeeAxes' Z axis doesn't align with epee forward
        // (makes sense, else rotating wrist won't change epee vector orientation, which is weird), 
        // there's a offset from current tip position to wristOnEpeeAxes' Z axis
        // apply this offset on the target and rotate Z axis to look at the offset-ed target. 

        var wristPos = targetWristOnEpeeAxes.position;
        var rotatedEpeeTipVector = suppinationRotation * (targetEpeeTip.position - wristPos);

        // calculate the epeeTip offset from wrist z axis
        var epeeTipDotWristForward = Vector3.Dot(
            rotatedEpeeTipVector,               // (Doesn't matter whether rotated or not. Dot product will be the same)
            targetWristOnEpeeAxes.forward);
        var closestPointToEpeeTipFromZ = wristPos + epeeTipDotWristForward * targetWristOnEpeeAxes.forward;
        var rotatedEpeeTipOffset = (wristPos + rotatedEpeeTipVector) - closestPointToEpeeTipFromZ;

        // apply the offset on targetPos, this is where the forward axis should look at
        var offsetTargetPos = targetPos - rotatedEpeeTipOffset;
        
        var afterPointToRotation = Quaternion.LookRotation(
            offsetTargetPos - wristPos, 
            suppinationRotation * targetWristOnEpeeAxes.up);
        var pointToRotation = Quaternion.FromToRotation(
            suppinationRotation * targetWristOnEpeeAxes.rotation * Vector3.forward, 
            afterPointToRotation * Vector3.forward);

        if (debug)
        {
            // wrist forward
            Debug.DrawLine(wristPos, closestPointToEpeeTipFromZ, Color.blue, 5f);
            // rotated epee tip -> wrist forward
            Debug.DrawLine(closestPointToEpeeTipFromZ, wristPos + rotatedEpeeTipVector, Color.red, 5f);
            // wrist -> new epee tip
            Debug.DrawRay(wristPos, pointToRotation * rotatedEpeeTipVector, Color.green, 5f);
            // target -> new wrist forward
            Debug.DrawLine(targetPos, offsetTargetPos, Color.magenta, 5f);

            debug = false;
            // ideally shouldn't have too big difference in suppination...
            var curSuppination = GetNewSuppination(suppinationRotation * targetWristOnEpeeAxes.rotation);
            var newSuppination = GetNewSuppination(pointToRotation * suppinationRotation * targetWristOnEpeeAxes.rotation);
            Debug.Log($"curSuppination: {curSuppination}, newSuppination: {newSuppination}");
            debug = true;
        }

        return pointToRotation;
    }
    
    private IEnumerator ApplyWristRotation(Quaternion rotationToApply)
    {
        // rotate epeeTarget around wrist at a constant velocity
        
        rotationToApply.ToAngleAxis(out var angle, out var axis0);
        var angleRotated = 0f;
        
        var axis = axis0;

        while (angleRotated < angle)
        {
            // time to refresh rotationToApply! 
            if (angleRotated > recheckPerDegrees) break;

            var angleToRotate = angularVelocity * Time.deltaTime;
            if (angleRotated + angleToRotate > angle)
            {
                angleToRotate = angle - angleRotated;
            }
            
            epeeTarget.RotateAround(targetWristOnEpeeAxes.position, axis, angleToRotate);

            if (debug)
            {
                Debug.DrawLine(targetWristOnEpeeAxes.position - 30 * axis, targetWristOnEpeeAxes.position + 30 * axis, Color.white);
            }

            angleRotated += angleToRotate;
            yield return null;
        }
    }

    // private void MaintainInternalPointTo()
    // {
    //     // internalPointToTarget will tend towards externalPointTo, while remaining inside the reach cone (the area circled by pointToTargets)
    //     var maxRadius = (pointToTargets[1].position - pointToTargets[0].position).magnitude;
    //     var centerToInternalPointTo = internalPointToTarget.position - pointToTargets[0].position;
    //     if (centerToInternalPointTo.magnitude > maxRadius)
    //     {
    //         internalPointToTarget.position = pointToTargets[0].position + 
    //                                          (externalPointToTarget.position - pointToTargets[0].position).normalized * maxRadius;
    //     }
    // }

    private IEnumerator RotateToTarget()
    {
        _rotating = true;

        float suppination;
        if (_supIndex == 1)
        {
            suppination = suppinationMax;
        } 
        else if (_supIndex == -1)
        {
            suppination = suppinationMin;
        }
        else
        {
            suppination = 0;
        }

        var iteration = 0;
        var angle = 0f;
        do
        {
            // So can rotate to another target even still haven't managed to reach this one
            if (iteration == maxIteration) break;

            var supRot = GetSuppinationRotation(suppination);
            Quaternion lookRot = GetLookAtRotation(internalPointToTarget.position, supRot);
            var rotationToApply = lookRot * supRot;

            rotationToApply.ToAngleAxis(out angle, out _);
            _curRotationAngle = angle;      // later used to check if reached rotation target
            if (angle < maxRotationError) break;

            yield return StartCoroutine(ApplyWristRotation(rotationToApply));
            iteration += 1;
        } 
        while (true);

        _rotating = false;
    }

    // public float maxEpeeToSpineDistance;
    
    private IEnumerator MoveToTarget()
    {
        // Still have issues of can't reach target at one shot (usually when the distance a bit far)
        // Not gonna solve this now. 
        
        _moving = true;
        
        // var actualEpee = wristOnEpeeAxes.parent.parent;
        var startEpeeTargetPos = epeeTarget.position;
        var epeeToSpine = startEpeeTargetPos - moveTargetRoot.position; // by right should use wristToSpine but anyway this should work
        if (epeeToSpine.magnitude > moveTargetDistance)
        {
            startEpeeTargetPos =
                moveTargetRoot.position + epeeToSpine * (moveTargetDistance / epeeToSpine.magnitude);
        }
        var epeeToWristOffset = targetWristOnEpeeAxes.position - epeeTarget.position;   // this will change when moving

        // first get localMoveVector (do Slerp in local space so won't change even if moved globally)
        var startLocalTargetPos = moveTargetRoot.InverseTransformPoint(moveTarget.position);
        var localWristStartPos = moveTargetRoot.InverseTransformPoint(startEpeeTargetPos + epeeToWristOffset);
        var localMoveVector = startLocalTargetPos - localWristStartPos;
        
        // translate localMoveVector to global space and calculate the duration
        var moveVector = moveTargetRoot.TransformVector(localMoveVector);       // this will change when moving
        var duration = moveVector.magnitude / velocity;                     // this will change when moving
        
        var prevEpeeTargetPos = startEpeeTargetPos;
        var localTargetPos = startLocalTargetPos;
        var timeElapsed = 0f;
        while (timeElapsed < duration / 2)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / duration;
            var progress = Mathf.Sqrt(t) * 1.5f;    // will reach end position at t = 4/9, so 'timeElapsed < duration / 2'

            // Slerp in local space (target pos will not change much even if the avatar move in world)
            // Then translate to world position
            var newLocalEpeeVector = Vector3.Slerp(
                localWristStartPos, localTargetPos, progress);
            var newEpeePosition = moveTargetRoot.position + moveTargetRoot.TransformVector(newLocalEpeeVector);

            // Compare with the epee position of previous frame
            // Add the difference to epeeTarget (rotation may also move epee position so don't overwrite)
            var toMove = newEpeePosition - prevEpeeTargetPos;
            epeeTarget.position += toMove;
            prevEpeeTargetPos += toMove;
            
            yield return null;

            Debug.DrawLine(moveTargetRoot.position, moveTargetRoot.TransformPoint(localWristStartPos), Color.yellow);
            Debug.DrawLine(moveTargetRoot.position, moveTargetRoot.TransformPoint(localTargetPos), Color.yellow);
            Debug.DrawRay(moveTargetRoot.TransformPoint(localWristStartPos), moveTargetRoot.TransformVector(localMoveVector), Color.green);

            // // MIGHT NOT NEED THIS. 
            // // Update the targetPos so wristRotation induced epee-to-wrist offset change is taken into account 
            // // Update the duration so the additional distance moved is taken into account
            // var curEpeeToWristOffset = wristOnEpeeAxes.position - actualEpee.position;
            // Debug.DrawRay(wristOnEpeeAxes.position, epeeToWristOffset, Color.blue);
            // Debug.DrawLine(wristOnEpeeAxes.position, actualEpee.position, Color.red);
            // var offsetDiff = curEpeeToWristOffset - epeeToWristOffset;
            // if (offsetDiff.magnitude > 0.01f)
            // {
            //     var newLocalTargetPos = startLocalTargetPos + moveTargetRoot.InverseTransformVector(offsetDiff);
            //     var originalRemainingVector = moveTargetRoot.TransformPoint(localTargetPos) - prevEpeeTargetPos;
            //     var curRemainingVector = moveTargetRoot.TransformPoint(newLocalTargetPos) - prevEpeeTargetPos;
            //     duration += (curRemainingVector - originalRemainingVector).magnitude / velocity;
            //     Debug.DrawLine(moveTargetRoot.TransformPoint(localTargetPos), moveTargetRoot.TransformPoint(newLocalTargetPos), Color.magenta, 5f);
            //     localTargetPos = newLocalTargetPos;
            // }
        }

        _moving = false;
    }

    
    
    // ----- below is exposed to BladeworkController ----- //
    
    public void SetNextSuppination(int supIndex)
    {
        _supIndex = supIndex;
    }

    private void SetWorldPointTo()
    {
        var pointTo = externalPointToTarget.position;

        var centerFromRoot = pointToTargets[0].position - neutralAxes.position;
        var pointToFromRoot = (pointTo - neutralAxes.position).normalized * centerFromRoot.magnitude;
        if ((pointToFromRoot - centerFromRoot).magnitude > rotationRadius)
        {
            pointTo = pointToTargets[0].position + (pointToFromRoot - centerFromRoot).normalized * rotationRadius;
        }
        else
        {
            pointTo = neutralAxes.position + pointToFromRoot;
        }
        Debug.DrawLine(neutralAxes.position, externalPointToTarget.position, Color.white);
        Debug.DrawLine(neutralAxes.position, pointTo, Color.green);
        internalPointToTarget.position = pointTo;
    }

    public void SetMoveToTargetPosition(int forward, int rightward, int upward, bool extended)
    {
        // each int can be -1, 0, 1. forward can only be 0 or 1. 
        // there are 2 * 3 * 3 * 2 -2 = 34 combinations

        if (forward == 0 && rightward == 0 && upward == 0)
            return;
        
        var distanceFromRoot = extended ? moveTargetDistance : moveTargetDistance / 4 * 3;
        var moveToVector = new Vector3(rightward, upward, forward).normalized * distanceFromRoot + moveTargetDistanceOffset;
        moveTarget.position = moveTargetRoot.position + moveToVector.normalized * distanceFromRoot;
    }
    
    public bool ReachedRotationTarget(float errorTolerance)
    {
        return _curRotationAngle < errorTolerance;
    }
    
    public bool ReachedMoveTarget()
    {
        return (epeeTarget.position - moveTarget.position).magnitude <= maxTranslationError;
    }

    // ----- above is exposed to BladeworkController ----- //

    
    
    // ----- below is exposed to FollowFootwork ----- //
    
    private bool _enabled;
    
    public void DisableControl()
    {
        _enabled = false;
    }

    public void EnableControl()
    {
        _enabled = true;
    }
    
    // ----- above is exposed to FollowFootwork ----- //

    
    
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log($"angle to rotate: {_curRotationAngle}, distance to move: {(epeeTarget.position - moveTarget.position).magnitude}");
        }
        
        if (!_enabled) return;
        
        SetWorldPointTo();

        # region direct input
        // float suppination = 0f;
        // if (Input.GetKey(KeyCode.Alpha2))
        // {
        //     suppination = suppinationMin;
        // }
        // else if (Input.GetKey(KeyCode.Alpha3))
        // {
        //     suppination = suppinationMax;
        // }
        //
        // int pointToIndex = 0;
        // if (Input.GetKey(KeyCode.Alpha4))
        // {
        //     pointToIndex = 1;
        // }
        // else if (Input.GetKey(KeyCode.Alpha5))
        // {
        //     pointToIndex = 2;
        // }
        // else if (Input.GetKey(KeyCode.Alpha6))
        // {
        //     pointToIndex = 3;
        // }
        // else if (Input.GetKey(KeyCode.Alpha7))
        // {
        //     pointToIndex = 4;
        // }
        
        // var moveToIndex = 0;
        // if (Input.GetKey(KeyCode.Q))
        // {
        //     moveToIndex = 1;
        // } 
        // else if (Input.GetKey(KeyCode.W))
        // {
        //     moveToIndex = 2;
        // }
        // else if (Input.GetKey(KeyCode.E))
        // {
        //     moveToIndex = 3;
        // }
        // else if (Input.GetKey(KeyCode.R))
        // {
        //     moveToIndex = 4;
        // }
        // else if (Input.GetKey(KeyCode.T))
        // {
        //     moveToIndex = 5;
        // }
        # endregion

        if (!_rotating)
        {
            StartCoroutine(RotateToTarget());
        }

        if (!_moving)
        {
            StartCoroutine(MoveToTarget());
        }

        if (showEpeePath)
        {
            Debug.DrawLine(_prevEpeeTipPos, epeeTip.position, Color.blue, 5f);
        }
        _prevEpeeTipPos = epeeTip.position;

    }

    private void OnDrawGizmos()
    {
        foreach (var pt in pointToTargets)
        {
            Gizmos.DrawWireSphere(pt.position, 3f);
        }
        
        Gizmos.DrawWireCube(moveTarget.position, Vector3.one * 3);
    }
}
