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
    public Transform shoulder;

    [Header("Control Target")]
    public Transform externalPointToTarget; // actually is controlled by BladeworkController
    public Transform epeeTarget;
    public Transform handIkHint;

    [Header("Control Parameters")] 
    public float suppinationMax;
    public float suppinationMin;
    public float rotationRadius;
    public int maxIteration;

    [Header("Tweaked (kinda static) Parameters")]
    public float angularVelocity;
    public float recheckPerDegrees;
    public float maxRotationError;
    public float maxSuppinationError = 0.01f;
    public float velocity;
    public float maxTranslationError;
    public float moveTargetDistance;
    public Vector3 moveTargetDistanceOffset;
    public float ikHintRadius;
    public float ikHintVelocity;

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
    }

    private void InitializeRotationRadius()
    {
        var centerLocalPos = pointToTargets[0].localPosition;
        var pointTosParent = pointToTargets[0].parent.parent;  // wrist clone
        var localMagnitude = centerLocalPos.magnitude;
        var localScale = pointTosParent.InverseTransformVector(Vector3.up).magnitude;
        var offsetAt45Deg = Mathf.Sqrt(rotationRadius * rotationRadius / 2);
        
        var upVector = rotationRadius * localScale * Vector3.down;
        var rightVector = rotationRadius * localScale * Vector3.right;
        var halfUpVector = offsetAt45Deg * localScale * Vector3.down;
        var halfRightVector = offsetAt45Deg * localScale * Vector3.right;
        pointToTargets[1].localPosition = (centerLocalPos + rightVector).normalized * localMagnitude;
        pointToTargets[2].localPosition = (centerLocalPos + halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[3].localPosition = (centerLocalPos - upVector).normalized * localMagnitude;
        pointToTargets[4].localPosition = (centerLocalPos - halfRightVector - halfUpVector).normalized * localMagnitude;
        pointToTargets[5].localPosition = (centerLocalPos - rightVector).normalized * localMagnitude;
        pointToTargets[6].localPosition = (centerLocalPos - halfRightVector + halfUpVector).normalized * localMagnitude;
        pointToTargets[7].localPosition = (centerLocalPos + upVector).normalized * localMagnitude;
        pointToTargets[8].localPosition = (centerLocalPos + halfRightVector + halfUpVector).normalized * localMagnitude;
    }
    
    private float GetSuppinationOfRotation(Quaternion wristRotation)
    {
        var neutralRotation = neutralAxes.rotation;

        var aligningRotation = Quaternion.FromToRotation(
            wristRotation * Vector3.forward, 
            neutralRotation * Vector3.forward);
        var alignedNewRotation = aligningRotation * wristRotation;
        var angle = Vector3.SignedAngle(
            neutralRotation * Vector3.right, 
            alignedNewRotation * Vector3.right, 
            neutralAxes.forward);

        return angle;
    }

    private Quaternion GetSuppinationRotation(float newSuppinationValue)
    {
        var suppinationAngle = GetSuppinationOfRotation(targetWristOnEpeeAxes.rotation);

        if (newSuppinationValue - suppinationAngle < maxSuppinationError 
            && newSuppinationValue - suppinationAngle > -maxSuppinationError)
        {
            return Quaternion.identity;
        }

        // not sure why but apparantly it only rotates half of the angle now...
        // will result in some errors in final result but still acceptable
        var suppinationRotation = Quaternion.AngleAxis(
            (newSuppinationValue - suppinationAngle) * 2,
            targetWristOnEpeeAxes.forward);
        
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

            var angleToRotate = angularVelocity * Time.fixedDeltaTime;
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
            yield return new WaitForFixedUpdate();
        }
    }
    
    private IEnumerator RotateToTarget()
    {
        _rotating = true;

        var suppination = _supIndex == 1 ? suppinationMax : _supIndex == -1 ? suppinationMin : 0;

        // So can rotate to another target even still haven't managed to reach this one
        var iteration = 0;
        do
        {
            var supRot = GetSuppinationRotation(suppination);
            Quaternion lookRot = GetLookAtRotation(internalPointToTarget.position, supRot);
            var rotationToApply = lookRot * supRot;

            rotationToApply.ToAngleAxis(out _curRotationAngle, out _);
            if (_curRotationAngle < maxRotationError) break;

            yield return StartCoroutine(ApplyWristRotation(rotationToApply));
            iteration += 1;
        } 
        while (iteration < maxIteration);

        _rotating = false;
    }
    
    private IEnumerator MoveToTarget()
    {
        // Still have issues of can't reach target at one shot (usually when the distance a bit far)
        // Not gonna solve this now. 
        
        _moving = true;
        
        // var actualEpee = wristOnEpeeAxes.parent.parent;
        var startEpeeTargetPos = epeeTarget.position;
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
            timeElapsed += Time.fixedDeltaTime;
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

            yield return new WaitForFixedUpdate();

            if (debug)
            {
                Debug.DrawLine(moveTargetRoot.position, moveTargetRoot.TransformPoint(localWristStartPos), Color.yellow);
                Debug.DrawLine(moveTargetRoot.position, moveTargetRoot.TransformPoint(localTargetPos), Color.yellow);
                Debug.DrawRay(moveTargetRoot.TransformPoint(localWristStartPos), moveTargetRoot.TransformVector(localMoveVector), Color.green);
            }
        }

        _moving = false;
    }



    public Vector3 GetMoveToTargetPosition(int forward, int rightward, int upward, bool extended)
    {
        var distanceFromRoot = extended ? moveTargetDistance : moveTargetDistance / 4 * 3;
        // var moveToVector = new Vector3(rightward, upward, forward).normalized * distanceFromRoot + moveTargetDistanceOffset;
        var moveToVector =
            (transform.right * rightward + transform.up * upward + transform.forward * forward).normalized 
            * distanceFromRoot
            + (moveTargetDistanceOffset.x * transform.right + moveTargetDistanceOffset.y * transform.up + moveTargetDistanceOffset.z * transform.forward);
        return moveTargetRoot.position + moveToVector.normalized * distanceFromRoot;
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

        if (debug)
        {
            Debug.DrawLine(neutralAxes.position, externalPointToTarget.position, Color.white);
            Debug.DrawLine(neutralAxes.position, pointTo, Color.green);
        }
        internalPointToTarget.position = pointTo;
    }

    public void SetMoveToTargetPosition(int forward, int rightward, int upward, bool extended)
    {
        // each int can be -1, 0, 1. forward can only be 0 or 1. 
        // there are 2 * 3 * 3 * 2 -2 = 34 combinations

        if (forward == 0 && rightward == 0 && upward == 0)
            return;

        moveTarget.position = GetMoveToTargetPosition(forward, rightward, upward, extended);
    }

    public void SetHintPosition(int x)
    {
        // x can be -1, 0, 1 (left-below, below, right-below)
        // is the lower half-circle of elbow
        
        var shoulderToMoveTarget = moveTarget.position - shoulder.position;
        var xDir = Vector3.Cross(shoulderToMoveTarget.normalized, Vector3.up);
        if (x > 0)
        {
            xDir *= 0.4f;   // coz is right arm, shouldn't be able to twist left that much
        }
        var hintOffset = (x * xDir - Vector3.up).normalized * ikHintRadius;
        var hintRootPos = shoulder.position + shoulderToMoveTarget / 4;
        // Debug.DrawRay(hintRootPos, xDir * 10, Color.red);
        // Debug.DrawRay(hintRootPos, hintOffset, Color.red);
        var newHintPosition = hintRootPos + hintOffset;

        newHintPosition = Vector3.MoveTowards(handIkHint.position, newHintPosition, ikHintVelocity * Time.fixedDeltaTime);
        Debug.DrawLine(handIkHint.position, newHintPosition, Color.yellow, 0.5f);
        
        handIkHint.position = newHintPosition;
    }
    
    public bool ReachedRotationTarget(float errorTolerance)
    {
        return _curRotationAngle < errorTolerance;
    }
    
    public bool ReachedMoveTarget()
    {
        return (epeeTarget.position - moveTarget.position).magnitude <= maxTranslationError;
    }

    public void ResetCoroutines()
    {
        StopAllCoroutines();

        _moving = false;
        _rotating = false;

        SetMoveToTargetPosition(1, 0, 0, false);
        SetHintPosition(0);
        SetNextSuppination(0);
        externalPointToTarget.position = pointToTargets[0].position;
    }

    // ----- above is exposed to BladeworkController ----- //
    

    private void CapEpeeTargetPos()
    {
        // many things can update epeeTarget position together and there's no check done on whether the new position is valid
        if ((epeeTarget.position - moveTargetRoot.position).magnitude > moveTargetDistance)
        {
            epeeTarget.position = moveTargetRoot.position +
                                  (epeeTarget.position - moveTargetRoot.position).normalized * moveTargetDistance;
        }
    }
    

    private void FixedUpdate()
    {
        SetWorldPointTo();
        CapEpeeTargetPos(); // to solve epee flying out of hand issue
        
        if (!_rotating)
        {
            StartCoroutine(RotateToTarget());
        }

        if (!_moving && !ReachedMoveTarget())
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
            Gizmos.DrawWireSphere(pt.position, 2f);
        }
        
        // Gizmos.DrawWireCube(moveTarget.position, Vector3.one * 3);

        // for (int i = -1; i < 2; i++)
        // {
        //     for (int j = -1; j < 2; j++)
        //     {
        //         for (int k = -1; k < 2; k++)
        //         {
        //             for (int l = 0; l < 2; l++)
        //             {
        //                 Gizmos.DrawWireCube(GetMoveToTargetPosition(i, j, k, l == 1), Vector3.one * 3);
        //             }
        //         }
        //     }
        // }
    }

}
