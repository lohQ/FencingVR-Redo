using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class TestPhysics : MonoBehaviour
{
    // imagine a horizontal bar, 
    public Transform jointRoot;
    public Transform epeeRoot;
    public Transform epeeTip;
    public Blockage blockage;
    private Vector3 _blockagePos;
    
    // the position to rotate from
    public float _jointFromX = 0.3f;
    public float _jointToX = -0.3f;
    public float _jointFromY = 0.4f;
    public float _jointToY = -0.4f;
    public float _jointFromZ = -0.1f;
    public float _jointToZ = -0.4f;
    public float _jointXStep = -0.05f;
    public float _jointYStep = -0.05f;
    public float _jointZStep = -0.05f;

    // the excess angle to rotate (is like additional force to apply)
    public float excessAngle = 45;
    public float maxRotationAngle = 360;
    
    private Dictionary<Vector3, List<Vector3>> _passedThroughLines = new Dictionary<Vector3, List<Vector3>>();
    public bool debug;
    public bool loop;

    
    public bool onAllGizmos;
    private IEnumerator<KeyValuePair<Vector3, List<Vector3>>> _passedThroughLinesEnum;
    private KeyValuePair<Vector3, List<Vector3>> _curPair;


    private void Start()
    {
        _passedThroughLinesEnum = _passedThroughLines.GetEnumerator();
        _blockagePos = blockage.center.position;
        var initialJointPos = _blockagePos + _jointFromY * Vector3.up + _jointFromZ * Vector3.forward;
        if (debug) Debug.Log($"BlockagePos: {_blockagePos}; JointPos: {initialJointPos}");
        
        StartCoroutine(TestCollision());
    }

    IEnumerator<Vector3> GetNextJointPos()
    {
        float curJointY = _jointFromY;
        while (curJointY >= _jointToY)
        {
            float curJointZ = _jointFromZ;
            while (curJointZ >= _jointToZ)
            {
                float curJointX = _jointFromX;
                while (curJointX >= _jointToX)
                {
                    if (debug) Debug.Log($"curJointY: {curJointY}; curJointZ: {curJointZ}; curJointX: {curJointX}");
                    yield return _blockagePos + curJointY * Vector3.up + curJointZ * Vector3.forward + curJointX * Vector3.right;
                    curJointX += _jointXStep;
                }
                curJointZ += _jointZStep;
            }
            curJointY += _jointYStep;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (onAllGizmos)
        {
            foreach (var pair in _passedThroughLines)
            {
                var from = pair.Key;
                foreach (var to in pair.Value)
                {
                    Gizmos.DrawLine(from, to);
                }
            }
        }
        else
        {
            if (_curPair.Key == Vector3.zero) return;

            var from = _curPair.Key;
            Debug.Log($"curPairValue.Value: {_curPair.Value}");
            foreach (var to in _curPair.Value)
            {
                Gizmos.DrawLine(from, to);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return))
        {
            onAllGizmos = !onAllGizmos;
            if (!onAllGizmos)
            {
                _passedThroughLinesEnum = _passedThroughLines.GetEnumerator();
                _curPair = _passedThroughLinesEnum.Current;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (_passedThroughLinesEnum.MoveNext())
            {
                _curPair = _passedThroughLinesEnum.Current;
            }
            else
            {
                _passedThroughLinesEnum = _passedThroughLines.GetEnumerator();
                _curPair = _passedThroughLinesEnum.Current;
            }
        }

        if (Input.GetKeyUp(KeyCode.L))
        {
            loop = !loop;
        }
    }
    
    IEnumerator TestCollision()
    {
        var nextJointPosEnum = GetNextJointPos();
        while(nextJointPosEnum.MoveNext())
        {
            var newJointPos = nextJointPosEnum.Current;

            var nextBlockageLookAtEnum = blockage.GetNextBlockagePos();
            while(nextBlockageLookAtEnum.MoveNext())
            {
                do
                {

                    jointRoot.position = newJointPos; // later change to interpolate?
                    jointRoot.rotation = Quaternion.Euler(85, 0, 0); // rotate z down so blade points up
                    epeeRoot.position = newJointPos;
                    epeeRoot.rotation = Quaternion.Euler(85, 0, 0);

                    var newLookAtPos = nextBlockageLookAtEnum.Current;

                    // get rotation axis
                    var vect1 = epeeTip.position - epeeRoot.position;
                    var vect2 = newLookAtPos - epeeRoot.position;
                    var rotationAxis = Vector3.Cross(vect1, vect2);

                    // get angle
                    var angle = Vector3.SignedAngle(vect1, vect2, rotationAxis);
                    var angleToRotate = angle + excessAngle;
                    angleToRotate =
                        angleToRotate > 179 ? 179 : angleToRotate; // so that it wouldn't rotate the other direction
                    if (debug) Debug.Log("angleToRotate: " + angleToRotate);

                    // apply rotation
                    var rot = Quaternion.AngleAxis(angleToRotate, rotationAxis);
                    float timeElapsed = 0;
                    Quaternion startRot = jointRoot.rotation;
                    Quaternion targetRot = rot * startRot;
                    var rotateDuration = angleToRotate / maxRotationAngle;
                    while (timeElapsed / rotateDuration <= 3)
                    {
                        var newRot = Quaternion.Slerp(startRot, targetRot, timeElapsed / rotateDuration);
                        jointRoot.rotation = newRot;
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }

                    // if pass-through, save the jointPos and lookAtPoint
                    var planeNormal = Vector3.Cross(blockage.GetCurBlockage() - epeeRoot.position, blockage.Vector());
                    var curEpeeVector = epeeTip.position - epeeRoot.position;
                    var projected = Vector3.ProjectOnPlane(curEpeeVector, planeNormal);
                    var planeToEpeeTip = curEpeeVector - projected;
                    var angleWithNormal = Vector3.Angle(planeToEpeeTip, planeNormal);

                    if (angleWithNormal > 90)
                    {
                        if (_passedThroughLines.ContainsKey(newJointPos))
                        {
                            _passedThroughLines[newJointPos].Add(newLookAtPos);
                        }
                        else
                        {
                            _passedThroughLines[newJointPos] = new List<Vector3>() {newLookAtPos};
                        }

                        if (debug) Debug.Log("Pass-through saved");
                    }
                } while (loop);
                
            }
            
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Vector3 avgContactPoint = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            avgContactPoint += contact.point;
            Debug.DrawRay(contact.point, contact.normal, Color.blue);
        }
        Debug.DrawRay(avgContactPoint / collision.contactCount, collision.impulse * 10, Color.red);
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 avgContactPoint = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            avgContactPoint += contact.point;
            Debug.DrawRay(contact.point, contact.normal, Color.blue);
        }
        Debug.DrawRay(avgContactPoint / collision.contactCount, collision.impulse * 10, Color.red);
    }

    private void OnCollisionExit(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            Debug.DrawRay(contact.point, contact.normal, Color.blue);
        }
    }
}
