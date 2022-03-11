using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewHandEffectorController : MonoBehaviour
{
    public Transform epeeDestParent;
    public HandRotationController handRotationController;

    public Transform epeeTarget;
    public Transform wristTarget;
    public float maxDistanceDelta;
    public float maxDegreesDelta;

    public Transform wrist;
    public Transform epee;
    public Transform epeeTip;

    public int movementMode;
    private List<Transform> _epeePositionTargets;
    
    void Start()
    {
        _epeePositionTargets = new List<Transform>();
        foreach (Transform child in epeeDestParent)
        {
            _epeePositionTargets.Add(child);
        }

        StartCoroutine(SetHandZAxis());
    }

    IEnumerator SetHandZAxis()
    {
        while ((wristTarget.position - wrist.position).magnitude > 0.01f || Quaternion.Angle(wristTarget.rotation, wrist.rotation) > 0.1)
        {
            yield return null;
        }
        handRotationController.SetCenter();
    }
    
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            movementMode += 1;
            if (movementMode == 3)
            {
                movementMode = 0;
            }
        }

        if (inCoroutine) return;
        
        if (movementMode == 0)
        {
            float suppination = handRotationController.GetSuppination(4);   // neutral
            int pointToIndex = 0;        // neutral
            if (Input.GetKey(KeyCode.Alpha1))
            {
                pointToIndex = 1;
            }
            else if (Input.GetKey(KeyCode.Alpha3))
            {
                pointToIndex = 3;
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                pointToIndex = 5;
            }
            else if (Input.GetKey(KeyCode.Alpha7))
            {
                pointToIndex = 7;
            }
            else if (Input.GetKey(KeyCode.Alpha9))
            {
                pointToIndex = 9;
            }

            if (Input.GetKey(KeyCode.Alpha0))
            {
                suppination = handRotationController.GetSuppination(0);
            } 
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                suppination = handRotationController.GetSuppination(2);
            } 
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                suppination = handRotationController.GetSuppination(6);
            } 
            else if (Input.GetKey(KeyCode.Alpha8))
            {
                suppination = handRotationController.GetSuppination(8);
            }

            var rotationSpeedIndex = Input.GetKey(KeyCode.LeftShift) ? 2 : 0;
            var rotationSpeed = handRotationController.GetAvgDegrees(rotationSpeedIndex);
            
            StartCoroutine(DoBladeWork(epeeTarget.position, pointToIndex, suppination, rotationSpeed));

            // if (Quaternion.Angle(rotationRequired, epeeTarget.rotation) > 0.5f)
            // {
            //     RotateTargetToOrientation(rotationRequired, "Wrist Point Towards");
            //     MoveTargetToPoint(translationRequired, "Wrist Point Towards");
            // }

        } 
        else if (movementMode == 1)
        {
            Vector3 pointTo = handRotationController.GetPosition(0);        // neutral position
            float suppination = handRotationController.GetSuppination(4);    // neutral position
            if (Input.GetKey(KeyCode.Alpha0))
            {
                suppination = handRotationController.GetSuppination(0);
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                suppination = handRotationController.GetSuppination(2);
            }
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                suppination = handRotationController.GetSuppination(6);
            }
            else if (Input.GetKey(KeyCode.Alpha8))
            {
                suppination = handRotationController.GetSuppination(8);
            }
            // var wristRotation = handRotationController.GetRotationToApply(pointTo, suppination, true);
            //
            // var rotatedWristRotation = wristRotation * wrist.rotation;
            // var rotationRequired = rotatedWristRotation * Quaternion.Inverse(wristTarget.localRotation);
            // var toEpeeVector = epee.position - wrist.position;
            // var rotatedToEpeeVector = wristRotation * toEpeeVector;
            // var translationRequired = rotatedToEpeeVector - toEpeeVector;
            // Debug.DrawRay(wrist.position, toEpeeVector, Color.green);
            // Debug.DrawRay(wrist.position, rotatedToEpeeVector, Color.yellow);
            // Debug.DrawRay(epee.position, translationRequired, Color.red);
            // if (rotationRequired.eulerAngles.magnitude > 0.5f)
            // {
            //     RotateTargetToOrientation(rotationRequired, "Wrist Suppination");
            //     MoveTargetToPoint(translationRequired, "Wrist Suppination");
            // }
        }
        else
        {
            var index = 0;
            if (Input.GetKey(KeyCode.Alpha1))
            {
                index = 1;
            }
            else if (Input.GetKey(KeyCode.Alpha3))
            {
                index = 3;
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                index = 5;
            }
            else if (Input.GetKey(KeyCode.Alpha7))
            {
                index = 7;
            }
            else if (Input.GetKey(KeyCode.Alpha9))
            {
                index = 9;
            }
            
            // MoveTargetToPoint(_epeePositionTargets[index].position - epeeTarget.position, "Wrist movement");
            //
            // Vector3 pointTo = handRotationController.GetPosition(0);        // neutral position
            // float suppination = handRotationController.GetSuppination(4);    // neutral position
            // var wristRotation = handRotationController.GetRotationToApply(pointTo, suppination, true);
            // var rotatedWristRotation = wristRotation * wrist.rotation;
            // var rotationRequired = rotatedWristRotation * Quaternion.Inverse(wristTarget.localRotation);
            // var toEpeeVector = epee.position - wrist.position;
            // var rotatedToEpeeVector = wristRotation * toEpeeVector;
            // var translationRequired = rotatedToEpeeVector - toEpeeVector;
            // Debug.DrawRay(wrist.position, toEpeeVector, Color.green);
            // Debug.DrawRay(wrist.position, rotatedToEpeeVector, Color.yellow);
            // Debug.DrawRay(epee.position, translationRequired, Color.red);
            // if (rotationRequired.eulerAngles.magnitude > 0.5f)
            // {
            //     RotateTargetToOrientation(rotationRequired, "Wrist Suppination");
            //     // MoveTargetToPoint(translationRequired, "Wrist Suppination");
            // }
        }
    }

    public bool inCoroutine = false;

    private Quaternion EpeeRotationFromWristRotation(Quaternion wristRot)
    {
        // some cheatsheets
        // var wristTargetRot = wristTarget.parent.rotation * wristTarget.localRotation;
        // var wristTargetParentRot = wristTarget.rotation * Quaternion.Inverse(wristTarget.localRotation);
        
        // wristTarget.localRotation never changes
        return wristRot * Quaternion.Inverse(wristTarget.localRotation);
    }
    
    private IEnumerator DoBladeWork(Vector3 position, int pointToIndex, float suppination, float rotationSpeed)
    {
        // transform pointTo to local space?
        
        inCoroutine = true;
        var corStart = Time.time;
        var pointTo = handRotationController.GetPosition(pointToIndex);
        
        // get initial end rotation (may change after wrist translation)
        var wristRotationToApply = handRotationController.GetRotationToApply(pointTo, suppination, true);
        var endWristRotation = wristRotationToApply * wrist.rotation;
        var epeeEndRotation = EpeeRotationFromWristRotation(endWristRotation);

        var epeeRotationAngle = Quaternion.Angle(epeeTarget.rotation, epeeEndRotation);
        if (epeeRotationAngle > 0.5f || (position - epeeTarget.position).magnitude > 1f)
        {
            var duration = epeeRotationAngle / rotationSpeed;
            Debug.Log($"rotationSpeed: {rotationSpeed}, epeeRotationAngle: {epeeRotationAngle}, duration: {duration}");

            var timeElapsed = 0f;
            var startingRotation = epeeTarget.rotation;
            var startingPosition = epeeTarget.position;

            // debug use
            // var startingTipPosition = epeeTip.position;
            // var startVector = startingTipPosition - startingPosition;
            // var endVector = epeeEndRotation * Quaternion.Inverse(startingRotation) * startVector;
            // var wristStartingPosition = wristTarget.position;
            // var wristStartVector = startingTipPosition - wristStartingPosition;
            // var wristEndVector = Quaternion.Inverse(wristTarget.rotation) * endWristRotation * wristStartVector;

            while (timeElapsed <= duration)
            {
                // wrist rotation induced epee rotation
                pointTo = handRotationController.GetPosition(pointToIndex);
                wristRotationToApply = handRotationController.GetRotationToApply(pointTo, suppination, true);
                endWristRotation = wristRotationToApply * wrist.rotation;
                epeeEndRotation = EpeeRotationFromWristRotation(endWristRotation);
                var newEpeeRotationAngle = Quaternion.Angle(startingRotation, epeeEndRotation);
                if (newEpeeRotationAngle - epeeRotationAngle > 0.01f)
                {
                    duration += Mathf.Max(0, (newEpeeRotationAngle / rotationSpeed) - duration);
                    epeeRotationAngle = newEpeeRotationAngle;
                    Debug.Log($"newEpeeRotationAngle: {newEpeeRotationAngle}, duration: {duration}");
                }

                // note: wristTarget position should stay the same!
                // var toEpeeVector = epee.position - wrist.position;
                // var rotatedToEpeeVector = wristRotationToApply * toEpeeVector;
                // var translationRequired = rotatedToEpeeVector - toEpeeVector;
                // position = startingPosition + translationRequired;
                // Debug.DrawRay(wrist.position, toEpeeVector, Color.green);
                // Debug.DrawRay(wrist.position, rotatedToEpeeVector, Color.yellow);
                // Debug.DrawRay(epee.position, translationRequired, Color.red);
                
                
                var t = timeElapsed / duration;
                var progress = 1 - t * t;
                var rot = Quaternion.Slerp(startingRotation, epeeEndRotation, t);
                var pos = Vector3.Lerp(startingPosition, position, t);
                RotateTargetToOrientation2(rot, "BladeWork Rotation");
                MoveTargetToPoint2(pos, "BladeWork Translation");
                
                // Debug.DrawRay(startingPosition, startVector, Color.blue);
                // Debug.DrawRay(epeeEndPosition, endVector, Color.red);
                // Debug.DrawRay(wristStartingPosition, wristStartVector, Color.blue);
                // Debug.DrawRay(wrist.position, wristEndVector, Color.red);
                
                yield return null;
                timeElapsed += Time.deltaTime;
            }
        }

        var pauseTime = 0.8f;
        var elapsedTime = 0f;
        while (elapsedTime <= pauseTime)
        {
            handRotationController.GetRotationToApply(pointTo, suppination, true);
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        var corEnd = Time.time;
        Debug.Log($"Coroutine ended. Total duration: {corEnd - corStart}");
        inCoroutine = false;
    }

    private void MoveTargetToPoint2(Vector3 position, String description)
    {
        epeeTarget.position = position;
        Debug.Log($"[{description}] move target to {position}");
    }
    
    private void MoveTargetToPoint(Vector3 translation, String description)
    {
        epeeTarget.position = Vector3.MoveTowards(
            epeeTarget.position, epee.position + translation, maxDistanceDelta * Time.deltaTime);
        Debug.Log($"[{description}] move target by {translation}");
    }
    
    private void RotateTargetToOrientation2(Quaternion orientation, String description)
    {
        epeeTarget.rotation = orientation;
        Debug.Log($"[{description}] rotate target from {epeeTarget.rotation.eulerAngles} to {orientation.eulerAngles}");
    }
    
    private void RotateTargetToOrientation(Quaternion orientation, String description)
    {
        epeeTarget.rotation = Quaternion.RotateTowards(
            epeeTarget.rotation, orientation, maxDegreesDelta * Time.deltaTime);
        Debug.Log($"[{description}] rotate target from {epeeTarget.rotation.eulerAngles} to {orientation.eulerAngles}");
    }
    
}
