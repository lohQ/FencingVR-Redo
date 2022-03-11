using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalHandController : MonoBehaviour
{
    [Header("References")]
    public Transform neutralAxes;
    public Transform wristOnEpeeAxes;
    public Transform epeeTip;
    public List<Transform> pointToTargets;  // index 0 must be 'Center PointTo'
    public Transform moveTargetRoot;
    public Transform moveTarget;
    public Transform worldPointToTarget;

    [Header("Control Target")]
    public Transform epeeTarget;

    [Header("Control Parameters")] 
    public float suppinationMax;
    public float suppinationMin;
    public float rotationRadius;
    public float angularVelocity;
    public float recheckPerDegrees;
    public float maxRotationError;
    public float velocity;
    public float maxTranslationError;
    public float moveTargetLocalDistance;
    public Vector3 moveTargetLocalDistanceOffset;

    [Header("General")]
    public bool debug;
    public bool showEpeePath;
    public int maxIteration;
    
    private bool _rotating;
    private bool _moving;
    private Vector3 _prevEpeeTipPos;
    private int _supIndex;
    private int _pointToIndex;
    private float _curRotationAngle;

    public bool rotatingByDefault;
    public bool movingByDefault;
    public bool rotateToWorldPoint;

    private void Start()
    {
        _prevEpeeTipPos = epeeTip.position;
        _rotating = false;
        _moving = false;
        _supIndex = 0;
        _pointToIndex = 0;
        rotatingByDefault = true;
        movingByDefault = true;

        var centerLocalPos = pointToTargets[0].localPosition;
        var pointTosParent = pointToTargets[0].parent.parent;  // should be wrist clone
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
    }

    public float GetCurSuppination()
    {
        // rotating neutral to actual then compare z rotation, will get the same result as rotating actual to neutral then compare
        var neutralRotation = neutralAxes.rotation;
        var actualRotation = wristOnEpeeAxes.rotation;
        
        var aligningRotation = Quaternion.FromToRotation(
            actualRotation * Vector3.forward, 
            neutralRotation * Vector3.forward);
        var alignedActualRotation = aligningRotation * actualRotation;
        var angle = Vector3.SignedAngle(
            neutralRotation * Vector3.right, 
            alignedActualRotation * Vector3.right, 
            neutralAxes.forward);

        if (debug)
        {
            Debug.DrawRay(neutralAxes.position, neutralRotation * Vector3.right * 50, Color.red, 1f);
            Debug.DrawRay(neutralAxes.position, alignedActualRotation * Vector3.right * 50, Color.magenta, 1f);
        }

        return angle;
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
        // var curSuppination = GetCurSuppination();
        // var suppinationChange = newSuppinationValue - curSuppination;
        var neutralRotation = neutralAxes.rotation;
        var actualRotation = wristOnEpeeAxes.rotation;

        // align neutral axes on actual axes
        var aligningRotation = Quaternion.FromToRotation(
            neutralRotation * Vector3.forward, 
            actualRotation * Vector3.forward);
        var alignedNeutralRotation = aligningRotation * neutralRotation;
        var suppinationAngle = Vector3.SignedAngle(
            alignedNeutralRotation * Vector3.right, 
            actualRotation * Vector3.right, 
            wristOnEpeeAxes.forward);
        
        // not sure why but apparantly it only rotates half of the angle now...
        // will result in some errors in final result but still acceptable
        if (newSuppinationValue - suppinationAngle < 0.01f && newSuppinationValue - suppinationAngle > -0.01f)
        {
            return Quaternion.identity;
        }
        var suppinationRotation = Quaternion.AngleAxis(
            (newSuppinationValue - suppinationAngle) * 2,
            wristOnEpeeAxes.forward);
        
        if (debug)
        {
            Debug.DrawRay(neutralAxes.position, neutralRotation * Vector3.right * 50, Color.red, 5f);
            Debug.DrawRay(neutralAxes.position, actualRotation * Vector3.right * 50, Color.red, 5f);
            Debug.DrawRay(neutralAxes.position, alignedNeutralRotation * Vector3.right * 50, Color.magenta, 5f);
            Debug.DrawRay(neutralAxes.position, suppinationRotation * actualRotation * Vector3.right * 50, Color.green, 5f);

            var newSuppinationAngle = Vector3.SignedAngle(
                alignedNeutralRotation * Vector3.right, 
                suppinationRotation * actualRotation * Vector3.right, 
                wristOnEpeeAxes.forward);
            Debug.Log($"suppinationAngle: {suppinationAngle}, newSuppinationAngle: {newSuppinationAngle}");
        }

        return suppinationRotation;
    }

    private Quaternion GetLookAtRotation(Vector3 targetPos, Quaternion suppinationRotation)
    {
        // TODO: this overshoot too much. need to fix. 
        
        var wristPos = wristOnEpeeAxes.position;
        var rotatedEpeeTipVector = suppinationRotation * (epeeTip.position - wristPos);

        // calculate the epeeTip offset from wrist z axis
        var epeeTipDotWristForward = Vector3.Dot(
            rotatedEpeeTipVector,   // change this just now. TODO: test difference
            wristOnEpeeAxes.forward);
        var closestPointToEpeeTipFromZ = wristPos + epeeTipDotWristForward * wristOnEpeeAxes.forward;
        var rotatedEpeeTipOffset = (wristPos + rotatedEpeeTipVector) - closestPointToEpeeTipFromZ;

        // apply the offset on targetPos, this is where the forward axis should look at
        var offsetTargetPos = targetPos - rotatedEpeeTipOffset;
        
        var afterPointToRotation = Quaternion.LookRotation(
            offsetTargetPos - wristPos, 
            suppinationRotation * wristOnEpeeAxes.up);
        // var pointToRotation0 = Quaternion.Inverse(suppinationRotation * wristOnEpeeAxes.rotation) * afterPointToRotation;
        var pointToRotation = Quaternion.FromToRotation(
            suppinationRotation * wristOnEpeeAxes.rotation * Vector3.forward, 
            afterPointToRotation * Vector3.forward);

        if (debug)
        {
            debug = false;

            // the latest debug lines
            Debug.DrawLine(wristPos, closestPointToEpeeTipFromZ, Color.blue, 5f);
            Debug.DrawLine(closestPointToEpeeTipFromZ, wristPos + rotatedEpeeTipVector, Color.red, 5f);
            Debug.DrawLine(targetPos, offsetTargetPos, Color.magenta, 5f);
            Debug.DrawRay(wristPos, pointToRotation * rotatedEpeeTipVector, Color.green, 5f);

            // these two lines should be parallel to each other, second line should end at targetPos
            var afterRotationClosestPoint = wristPos +
                                            (pointToRotation * (closestPointToEpeeTipFromZ - wristPos));
            Debug.DrawLine(closestPointToEpeeTipFromZ, epeeTip.position, Color.green, 5f);
            Debug.DrawLine(afterRotationClosestPoint, afterRotationClosestPoint + rotatedEpeeTipOffset, Color.yellow, 5f);
            
            // ideally shouldn't have too big difference in suppination...
            var curSuppination = GetNewSuppination(suppinationRotation * wristOnEpeeAxes.rotation);
            var newSuppination = GetNewSuppination(pointToRotation * suppinationRotation * wristOnEpeeAxes.rotation);
            Debug.Log($"curSuppination: {curSuppination}, newSuppination: {newSuppination}");

            // ideally should end up at targetPos
            var finalEpeeTipVector = pointToRotation * rotatedEpeeTipVector;
            Debug.DrawRay(wristPos, rotatedEpeeTipVector, Color.red, 5f);
            Debug.DrawRay(wristPos, afterPointToRotation * Vector3.forward * 50, Color.blue, 5f);
            Debug.DrawRay(wristPos, finalEpeeTipVector, Color.magenta, 5f);

            debug = true;
        }

        return pointToRotation;
    }
    
    private IEnumerator ApplyWristRotation(Quaternion rotationToApply)
    {
        // why doesn't this do the rotation completely?
        // suspect: axis not changing along with wristRotation?
        
        rotationToApply.ToAngleAxis(out var angle, out var axis0);
        var angleRotated = 0f;
        
        // // local space: so if hand moved and neutral axes changed, can follow accordingly
        // but THIS DOESN'T WORK. NOT GONNA SOLVE THIS NOW.
        // var axisInLocalSpace = neutralAxes.InverseTransformDirection(axis0.normalized);
        // var axis = neutralAxes.TransformDirection(axisInLocalSpace);
        var axis = axis0;

        while (angleRotated < angle)
        {
            if (angleRotated > recheckPerDegrees)
            {
                // maxAngle determines how frequent to re-check (and change) rotation direction
                break;
            }

            var angleToRotate = angularVelocity * Time.deltaTime;
            if (angleRotated + angleToRotate > angle)
            {
                angleToRotate = angle - angleRotated;
            }
            
            epeeTarget.RotateAround(wristOnEpeeAxes.position, axis, angleToRotate);

            if (debug)
            {
                Debug.DrawLine(wristOnEpeeAxes.position - 30 * axis, wristOnEpeeAxes.position + 30 * axis, Color.green);
                Debug.DrawRay(epeeTarget.position, epeeTarget.right * 50, Color.black, 0.5f);
                Debug.DrawRay(wristOnEpeeAxes.position, wristOnEpeeAxes.right * 50, Color.blue, 0.5f);
            }

            // axis = neutralAxes.TransformDirection(axisInLocalSpace);
            angleRotated += angleToRotate;
            yield return null;
        }
    }

    public IEnumerator RotateToTarget()
    {
        _rotating = true;
        var suppination = TranslateSupIndexToSup(_supIndex);

        var iteration = 0;
        var angle = 0f;
        do
        {
            if (iteration == maxIteration)
            {
                break;
            }

            var supRot = GetSuppinationRotation(suppination);
            Quaternion lookRot;
            if (!rotateToWorldPoint)
            {
                lookRot = GetLookAtRotation(pointToTargets[_pointToIndex].position, supRot);
            }
            else
            {
                // cap the pointTo, make sure wrist won't rotate too weird
                var pointTo = worldPointToTarget.position;
                var centerFromRoot = pointToTargets[0].position - neutralAxes.position;
                var maxRadius = (pointToTargets[1].position - pointToTargets[0].position).magnitude;
                var pointToFromRoot = (pointTo - neutralAxes.position).normalized * centerFromRoot.magnitude;
                if ((pointToFromRoot - centerFromRoot).magnitude > maxRadius)
                {
                    pointTo = pointToTargets[0].position + (pointToFromRoot - centerFromRoot).normalized * maxRadius;
                    Debug.DrawLine(centerFromRoot, pointTo, Color.blue, 5f);
                }
                Debug.DrawRay(neutralAxes.position, centerFromRoot, Color.yellow, 5f);
                Debug.DrawRay(neutralAxes.position, pointTo, Color.green, 5f);
                lookRot = GetLookAtRotation(pointTo, supRot);
            }
            var rotationToApply = lookRot * supRot;
            rotationToApply.ToAngleAxis(out angle, out _);
            _curRotationAngle = angle;  // later used to check if reached rotation target
            if (angle < maxRotationError)
            {
                break;
            }

            if (debug) Debug.Log($"At iteration {iteration}");
            yield return StartCoroutine(ApplyWristRotation(rotationToApply));
            iteration += 1;
        } 
        while (true);

        _rotating = false;
    }

    public bool updateTranslationSourceDestInside;
    
    public IEnumerator MoveToTarget()
    {
        _moving = true;
        
        var actualEpee = wristOnEpeeAxes.parent.parent;
        var startEpeeTargetPos = epeeTarget.position;
        var epeeToWristOffset = wristOnEpeeAxes.position - actualEpee.position;     // this will change when moving

        // first get localMoveVector (do Slerp in local space so won't change even if moved globally)
        var startLocalTargetPos = moveTargetRoot.InverseTransformPoint(moveTarget.position);
        var localWristStartPos = moveTargetRoot.InverseTransformPoint(startEpeeTargetPos + epeeToWristOffset);
        var localMoveVector = startLocalTargetPos - localWristStartPos;
        
        // translate localMoveVector to global space and calculate the duration
        var moveVector = moveTargetRoot.TransformVector(localMoveVector);       // this will change when moving
        var duration = moveVector.magnitude / velocity;                     // this will change when moving
        
        var prevEpeeTargetPos = epeeTarget.position;
        var localTargetPos = startLocalTargetPos;
        var timeElapsed = 0f;
        while (timeElapsed < duration / 2)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / duration;
            var progress = Mathf.Sqrt(t) * 1.5f;    // will reach end position with t = 4/9, so 'timeElapsed < duration / 2'

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

            // MIGHT NOT NEED THIS. 
            // Update the targetPos so wristRotation induced epee-to-wrist offset change is taken into account 
            // Update the duration so the additional distance moved is taken into account
            var curEpeeToWristOffset = wristOnEpeeAxes.position - actualEpee.position;
            Debug.DrawRay(wristOnEpeeAxes.position, epeeToWristOffset, Color.blue);
            Debug.DrawLine(wristOnEpeeAxes.position, actualEpee.position, Color.red);
            var offsetDiff = curEpeeToWristOffset - epeeToWristOffset;
            if (offsetDiff.magnitude > 0.01f)
            {
                var newLocalTargetPos = startLocalTargetPos + moveTargetRoot.InverseTransformVector(offsetDiff);
                var originalRemainingVector = moveTargetRoot.TransformPoint(localTargetPos) - prevEpeeTargetPos;
                var curRemainingVector = moveTargetRoot.TransformPoint(newLocalTargetPos) - prevEpeeTargetPos;
                duration += (curRemainingVector - originalRemainingVector).magnitude / velocity;
                Debug.DrawLine(moveTargetRoot.TransformPoint(localTargetPos), moveTargetRoot.TransformPoint(newLocalTargetPos), Color.magenta, 5f);
                localTargetPos = newLocalTargetPos;
            }
        }

        _moving = false;
    }

    public float TranslateSupIndexToSup(int supIndex)
    {
        if (supIndex == 1)
        {
            return suppinationMax;
        }
        if (supIndex == -1)
        {
            return suppinationMin;
        } 
        return 0;
    }
    
    public void SetNextRotation(int supIndex, int pointToIndex)
    {
        // TODO: change from pointToIndex to upwards and rightwards
        _pointToIndex = pointToIndex;
        _supIndex = supIndex;
    }

    public void SetMoveToTargetPosition(int forward, int rightward, int upward, bool extended)
    {
        // forward is -x, right is -z, up is -y
        // each int can be -1, 0, 1. forward can only be 0 or 1. 
        // there are 2 * 3 * 3 * 2 -2 = 34 combinations

        if (forward == 0 && rightward == 0 && upward == 0)
            return;
        
        var distanceFromRoot = extended ? moveTargetLocalDistance : moveTargetLocalDistance / 4 * 3;
        var newLocalPosition = new Vector3(-forward, -upward, -rightward) + moveTargetLocalDistanceOffset;
        newLocalPosition = newLocalPosition.normalized * distanceFromRoot;
        moveTarget.localPosition = newLocalPosition;
    }

    public bool Rotating()
    {
        return _rotating;
    }

    public bool ReachedRotationTarget(float errorTolerance)
    {
        return _curRotationAngle < errorTolerance;
    }
    
    public bool ReachedMoveTarget()
    {
        return (epeeTarget.position - moveTarget.position).magnitude <= maxTranslationError;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            GetCurSuppination();
        }
        
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

        if (rotatingByDefault && !_rotating)
        {
            StartCoroutine(RotateToTarget());
        }

        if (movingByDefault && !_moving)
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
        
        Gizmos.DrawWireSphere(moveTarget.position, 3f);
    }
}
