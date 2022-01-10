using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// TODO: use RotationLimit.Apply() at correct place.

public class ProceduralBladework : MonoBehaviour
{
    /* BladeWork = The control of right hand position relative to fencer's shoulder */
    
    // change epee pivot to hand hold position
    // set that as right hand target
    // epee follow is parented by fencer?
    // 
    
    enum BladeAction
    {
        IdlePoint,   // can point up or down
        Attack,     // can select target from Chest, UpperArm, LowerArm, Thigh, or Foot
        Parry4,
        Parry6,
        CircularParry4,
        CircularParry6,
        Riposte,
        DisengageClockwise,
        DisengageCounterclockwise,
    }
    
    [Serializable]
    public struct DegreeOfFreedom
    {
        // https://www.researchgate.net/figure/Three-degrees-of-freedom-DOF-of-the-healthy-human-wrist_fig1_277332478
        // https://pubmed.ncbi.nlm.nih.gov/24322647/
        
        [SerializeField] public Vector3 neutralRotation;
        [SerializeField] public float radialDeviation, ulnarDeviation;
        [SerializeField] public float flexion, extension;
        [SerializeField] public float pronation, suppination;
    }

    [Serializable]
    public struct Arm
    {
        public Transform root;
        // public DegreeOfFreedom rootDegreeOfFreedom;
        public Transform elbow;
        // public DegreeOfFreedom elbowDegreeOfFreedom;
        public Transform hand;
        public DegreeOfFreedom handDegreeOfFreedom;
    }

    public Arm arm;
    // iniaitlize to epeeTip position
    public Transform rightHandTarget;
    public Transform epeeTarget;
    public Transform bladeTarget;
    public Transform fencer;
    public Transform fencerHip;
    public Transform epeeTip;

    public float rotationRadius = 0.1f;
    public float tipMoveDistancePerFrame = 0.05f;
    
    private double _epeeLength;
    private bool _engarding;
    private float _progress;
    private float _progressStep;
    private bool _rotating;
    private List<Quaternion> _lookAts;
    private bool _moving;
    private List<Vector3> _linePoints;
    private List<float> _progressThresholds;

    private Quaternion _neutralHandLocalRotation;
    private Vector3 _neutralHandXAxis;
    private Vector3 _neutralHandYAxis;
    private Vector3 _neutralHandZAxis;
    private Quaternion _curLocalRotation;


    void Start()
    {
        _lookAts = new List<Quaternion>();
        _linePoints = new List<Vector3>();
        _epeeLength = (epeeTip.position - transform.position).magnitude;
        // EnterEnGarde();
        _neutralHandLocalRotation = arm.hand.localRotation;
        _neutralHandXAxis = arm.hand.right;
        _neutralHandYAxis = arm.hand.up;
        _neutralHandZAxis = arm.hand.forward;
        _curLocalRotation = _neutralHandLocalRotation;
        _rotating = false;
        // Test();
    }

    void Update()
    {
        // if (_rotating)
        // {
        //     DoRotation();
        //     return; // for now just testing so do one at a time
        // }
        //
        // if (_moving)
        // {
        //     DoMoveWhilePointTo();
        //     return;
        // }
        //
        // if (Input.GetKeyUp(KeyCode.R))
        // {
        //     InitRotateAround(rotationRadius);
        // }
        // else if (Input.GetKeyUp(KeyCode.M))
        // {
        //     InitMoveWhilePointTo();
        // }

        if (!_rotating)
        {
            if (Input.GetKeyUp(KeyCode.Q))
            {
                StartCoroutine(RotateToLimit(-1, 0, 0));
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                StartCoroutine(RotateToLimit(1, 0, 0));
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                StartCoroutine(RotateToLimit(0, -1, 0));
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                StartCoroutine(RotateToLimit(0, 1, 0));
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                StartCoroutine(RotateToLimit(0, 0, -1));
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                StartCoroutine(RotateToLimit(0, 0, 1));
            }

            if (Input.GetKeyUp(KeyCode.M))
            {
                StartCoroutine(RotateWrist(wristRotateDegrees, wristRotateClockwise, wristRotateStopAtPercentage));
            }
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            // C for Check
            var cappedRotation = CapRotation(arm.hand.localRotation, true);
            // _curLocalRotation = cappedRotation;
        }
        
        if (Input.GetKeyUp(KeyCode.P))
        {
            // P for Point
            PointToTarget();
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            // R for reset
            ResetLocalRotation();
        }

        if (!_moving)
        {
            if (Input.GetKeyUp(KeyCode.H))
            {
                StartCoroutine(HitTarget());
            }
        }

        // rightHandTarget.rotation = arm.elbow.rotation * _curLocalRotation;

    }

    // to validate RotateWrist
    public float wristRotateDegrees = 5f;
    public bool wristRotateClockwise = true;
    public float wristRotateStopAtPercentage = 1f;
    public float allCompletionTime = 1f;
    // to validate CapRotation
    public float xRotation;
    public float yRotation;
    public float zRotation;

    void ResetLocalRotation()
    {
        Debug.Log("localRotation: " + arm.hand.localRotation.eulerAngles);
        _neutralHandLocalRotation = arm.hand.localRotation;
        _neutralHandXAxis = arm.hand.right;
        _neutralHandYAxis = arm.hand.up;
        _neutralHandZAxis = arm.hand.forward;
        var handPos = arm.hand.position;
        Debug.DrawLine(handPos, handPos + _neutralHandLocalRotation * Vector3.up, Color.green, 5f);
        Debug.DrawLine(handPos, handPos + _neutralHandLocalRotation * Vector3.right, Color.red, 5f);
        Debug.DrawLine(handPos, handPos + _neutralHandLocalRotation * Vector3.forward, Color.blue, 5f);
    }

    IEnumerator RotateToLimit(int xRadialDeviation, int yExtension, int zSuppination)
    {
        _rotating = true;
        
        Quaternion rotationToApply = Quaternion.identity;
        if (xRadialDeviation < 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.ulnarDeviation, -_neutralHandXAxis);
        } else if (xRadialDeviation > 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.radialDeviation, _neutralHandXAxis);
        }
        
        if (yExtension < 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.flexion, -_neutralHandYAxis);
        } else if (yExtension > 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.extension, _neutralHandYAxis);
        }
        
        if (zSuppination < 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.pronation, -_neutralHandZAxis);
        } else if (zSuppination > 0)
        {
            rotationToApply *= Quaternion.AngleAxis(arm.handDegreeOfFreedom.suppination, _neutralHandZAxis);
        }

        var completionTime = 1f;
        var timeElapsed = 0f;
        var startRotation = arm.hand.localRotation;
        Debug.Log("rotationToApply: " + rotationToApply);
        var endRotation = CapRotation(rotationToApply * _neutralHandLocalRotation, true);
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            _curLocalRotation = Quaternion.Lerp(
                startRotation,
                endRotation, 
                t);
            Debug.DrawRay(arm.hand.position, arm.elbow.rotation * _curLocalRotation * Vector3.up, Color.cyan, 5f);
            yield return null;
        }
        
        _rotating = false;
    }

    Quaternion CapRotation(Quaternion newLocalRotation, bool log=false)
    {
        # region old codes
        
        // var zRadialDeviation = Vector3.SignedAngle(_neutralHandLocalRotation * Vector3.up, newLocalRotation * Vector3.up, _neutralHandXAxis);
        // if (log) Debug.Log("zRadialDeviation: " + zRadialDeviation);
        // if (Math.Abs(zRadialDeviation) > 0.000001)
        // {
        //     // var curXFromNeutral = Vector3.SignedAngle(
        //     //     arm.hand.localRotation * Vector3.up, _neutralHandLocalRotation * Vector3.up, _neutralHandXAxis);
        //     // if (log) Debug.Log("curXFromNeutral: " + curXFromNeutral);
        //     if (zRadialDeviation < 0 && zRadialDeviation < -arm.handDegreeOfFreedom.ulnarDeviation)
        //     {
        //         zRadialDeviation = -arm.handDegreeOfFreedom.ulnarDeviation;
        //     } else if (zRadialDeviation > 0 && zRadialDeviation > arm.handDegreeOfFreedom.radialDeviation)
        //     {
        //         zRadialDeviation = arm.handDegreeOfFreedom.radialDeviation;
        //     }
        // }
        //
        // // cappedX = Quaternion.FromToRotation(arm.hand.localRotation, Quaternion.AngleAxis(xRadialDeviation, _neutralHandXAxis) * _neutralHandLocalRotation);
        //
        // var yExtension = Vector3.SignedAngle(_neutralHandLocalRotation * Vector3.forward, newLocalRotation * Vector3.forward, -_neutralHandYAxis);
        // if (log) Debug.Log("yExtension: " + yExtension);
        // if (Math.Abs(yExtension) > 0.000001)
        // {
        //     // var curYFromNeutral = Vector3.SignedAngle(
        //     //     arm.hand.localRotation * Vector3.right, _neutralHandLocalRotation * Vector3.right, -_neutralHandYAxis);
        //     // if (log) Debug.Log("curYFromNeutral: " + curYFromNeutral);
        //     if (yExtension < 0 && yExtension < -arm.handDegreeOfFreedom.flexion)
        //     {
        //         yExtension = -arm.handDegreeOfFreedom.flexion;
        //     } else if (yExtension > 0 && yExtension > arm.handDegreeOfFreedom.extension)
        //     {
        //         yExtension = arm.handDegreeOfFreedom.extension;
        //     }
        // }
        //
        // var xSuppination = Vector3.SignedAngle(_neutralHandLocalRotation * Vector3.one, newLocalRotation * Vector3.one, -_neutralHandZAxis);
        // if (log) Debug.Log("xSuppination: " + xSuppination);
        // if (Math.Abs(xSuppination) > 0.000001)
        // {
        //     // var curZFromNeutral = Vector3.SignedAngle(
        //     //     arm.hand.localRotation * Vector3.up, _neutralHandLocalRotation * Vector3.up, -_neutralHandZAxis);
        //     // if (log) Debug.Log("curZFromNeutral: " + curZFromNeutral);
        //     if (xSuppination < 0 && xSuppination < -arm.handDegreeOfFreedom.pronation)
        //     {
        //         xSuppination = -arm.handDegreeOfFreedom.pronation;
        //     } else if (xSuppination > 0 && xSuppination > arm.handDegreeOfFreedom.suppination)
        //     {
        //         xSuppination = arm.handDegreeOfFreedom.suppination;
        //     }
        // }
        // if (log) Debug.Log("capped rotation: " + zRadialDeviation + ", " + yExtension + ", " + xSuppination);
        //
        // return Quaternion.Euler(zRadialDeviation, yExtension, xSuppination) * _neutralHandLocalRotation;
        
        # endregion

        Debug.Log("newLocalRotation.eulerAngles: " + newLocalRotation.eulerAngles);
        
        Vector3 inputRotationEuler = newLocalRotation.eulerAngles - _neutralHandLocalRotation.eulerAngles;
        Vector3 cappedRotationInEuler = inputRotationEuler;
        if (inputRotationEuler.z < 0 && inputRotationEuler.z < -arm.handDegreeOfFreedom.ulnarDeviation)
        {
            cappedRotationInEuler.z = -arm.handDegreeOfFreedom.ulnarDeviation;
        } else if (inputRotationEuler.z > 0 && inputRotationEuler.z > arm.handDegreeOfFreedom.radialDeviation)
        {
            cappedRotationInEuler.z = arm.handDegreeOfFreedom.radialDeviation;
        } 
        
        if (inputRotationEuler.y < 0 && inputRotationEuler.y < -arm.handDegreeOfFreedom.flexion)
        {
            cappedRotationInEuler.y = -arm.handDegreeOfFreedom.flexion;
        } else if (inputRotationEuler.y > 0 && inputRotationEuler.y > arm.handDegreeOfFreedom.extension)
        {
            cappedRotationInEuler.y = arm.handDegreeOfFreedom.extension;
        } 
        
        if (inputRotationEuler.x < 0 && inputRotationEuler.x < -arm.handDegreeOfFreedom.pronation)
        {
            cappedRotationInEuler.x = -arm.handDegreeOfFreedom.pronation;
        } else if (inputRotationEuler.x > 0 && inputRotationEuler.x > arm.handDegreeOfFreedom.suppination)
        {
            cappedRotationInEuler.x = arm.handDegreeOfFreedom.suppination;
        }

        cappedRotationInEuler += _neutralHandLocalRotation.eulerAngles;
        Debug.Log("resultRotation: " + cappedRotationInEuler);

        return Quaternion.Euler(cappedRotationInEuler.x, cappedRotationInEuler.y, cappedRotationInEuler.z);
    }

    void Test()
    {
        var baseRotation = Quaternion.Euler(10, 5, 0);
        var rotateAroundX = Quaternion.AngleAxis(10, Vector3.right);
        var rotateAroundY = Quaternion.AngleAxis(10, Vector3.up);
        var rotateAroundZ = Quaternion.AngleAxis(10, Vector3.forward);
        var signedAngleX = Vector3.SignedAngle(
            baseRotation * Vector3.up, rotateAroundX * baseRotation * Vector3.up, Vector3.right);
        var signedAngleY = Vector3.SignedAngle(
            baseRotation * Vector3.forward, rotateAroundY * baseRotation * Vector3.forward, Vector3.up);
        var signedAngleZ = Vector3.SignedAngle(
            baseRotation * Vector3.right, rotateAroundZ * baseRotation * Vector3.right, Vector3.forward);
        Debug.Log("signedAngleX: " + signedAngleX);
        Debug.Log("signedAngleY: " + signedAngleY);
        Debug.Log("signedAngleZ: " + signedAngleZ);
        
        
        // var rotationToApply = Quaternion.AngleAxis(10, baseRotation * Vector3.right);
        // var test1 = rotationToApply * baseRotation;
        // var test2 = baseRotation * rotationToApply;
        // Debug.Log("test1: " + test1.eulerAngles);
        // Debug.Log("test2: " + test2.eulerAngles);
        // var test3 = Quaternion.Inverse(baseRotation) * test1;
        // var test4 = test1 * Quaternion.Inverse(baseRotation);
        // Debug.Log("test3: " + test3.eulerAngles);
        // Debug.Log("test4: " + test4.eulerAngles);

        // var rotationToApply2 = Quaternion.AngleAxis(10, baseRotation * Vector3.up);
        // var test3 = rotationToApply2 * baseRotation;
        // var test4 = baseRotation * rotationToApply2;
        // Debug.Log("test3: " + test3.eulerAngles);
        // Debug.Log("test4: " + test4.eulerAngles);
        // var rotationToApply3 = Quaternion.AngleAxis(10, baseRotation * Vector3.forward);
        // var test5 = rotationToApply3 * baseRotation;
        // var test6 = baseRotation * rotationToApply3;
        // Debug.Log("test5: " + test5.eulerAngles);
        // Debug.Log("test6: " + test6.eulerAngles);
    }
    
    IEnumerator RotateWrist(float degrees, bool clockwise, float stopAtPercentage)
    {
        // TODO: don't let animation affect elbow <- create a holding animation that doesn't move the arm
        _rotating = true;
        
        // cap the rotation degree so the hand won't rotate like crazy
        // var relativeToX = Vector3.SignedAngle(
        //     arm.hand.localRotation * Vector3.up, _neutralHandLocalRotation * Vector3.up, _neutralHandXAxis);
        // var maxDownDegrees = CapRotation(degrees * 2 + relativeToX, 0, 0).Item1;
        // var relativeToY = Vector3.SignedAngle(
        //     arm.hand.localRotation * Vector3.right, _neutralHandLocalRotation * Vector3.right, -_neutralHandYAxis);
        // var maxLeftDegrees = CapRotation(0, relativeToY + degrees, 0).Item2;
        // var maxRightDegrees = CapRotation(0, relativeToY - degrees, 0).Item2;
        // Debug.Log("maxDownDegrees: " + maxDownDegrees + 
        //           ", leftDegrees: " + maxLeftDegrees + 
        //           ", rightDegrees" + maxRightDegrees);

        // current hand rotation is the up rotation
        var localRotInverse = Quaternion.Inverse(arm.hand.localRotation);
        // Debug.Log("arm.hand.localRotation.eulerAngles: " + arm.hand.localRotation.eulerAngles);
        var rotateTwoDown = Quaternion.AngleAxis(360 - (degrees * 2), Vector3.right) * arm.hand.localRotation;
        // Debug.Log("rotateTwoDown: " + rotateTwoDown.eulerAngles);
        rotateTwoDown = localRotInverse * CapRotation(rotateTwoDown);
        // Debug.Log("rotateTwoDown: " + rotateTwoDown.eulerAngles);
        var rotateDown =  Quaternion.Lerp(Quaternion.identity, rotateTwoDown, 0.5f);
        // Debug.Log("rotateDown: " + rotateDown.eulerAngles);
        Quaternion rotateSideWay1;
        Quaternion rotateSideWay2;
        if (clockwise)
        {
            // rotateSideWay1 = Quaternion.AngleAxis(maxLeftDegrees - relativeToY, _neutralHandYAxis);
            // rotateSideWay2 = Quaternion.AngleAxis(maxRightDegrees - relativeToY, _neutralHandYAxis);
            rotateSideWay1 = Quaternion.AngleAxis(degrees, Vector3.forward) * arm.hand.localRotation;
            rotateSideWay1 = localRotInverse * rotateSideWay1;
            // rotateSideWay1 = localRotInverse * CapRotation(rotateSideWay1);
            rotateSideWay2 = Quaternion.AngleAxis(360 - degrees, Vector3.forward) * arm.hand.localRotation;
            rotateSideWay2 = localRotInverse * rotateSideWay2;
            // rotateSideWay2 = localRotInverse * CapRotation(rotateSideWay2);
        }
        else
        {
            rotateSideWay1 = Quaternion.AngleAxis(360 - degrees, Vector3.forward) * arm.hand.localRotation;
            rotateSideWay1 = localRotInverse * rotateSideWay1;
            // rotateSideWay1 = localRotInverse * CapRotation(rotateSideWay1);
            rotateSideWay2 = Quaternion.AngleAxis(degrees, Vector3.forward) * arm.hand.localRotation;
            rotateSideWay2 = localRotInverse * rotateSideWay2;
            // rotateSideWay2 = localRotInverse * CapRotation(rotateSideWay2);
            // rotateSideWay1 = Quaternion.AngleAxis(maxRightDegrees - relativeToY, _neutralHandYAxis);
            // rotateSideWay2 = Quaternion.AngleAxis(maxLeftDegrees - relativeToY, _neutralHandYAxis);
        }
        Debug.Log("rotateSideWay1: " + rotateSideWay1.eulerAngles);
        Debug.Log("rotateSideWay2: " + rotateSideWay2.eulerAngles);
        // Debug.Log("(maxDownDegrees - relativeToX) / 2: " + (maxDownDegrees - relativeToX) / 2);
        // Debug.Log("maxLeftDegrees - relativeToY: " + (maxLeftDegrees - relativeToY));
        // Debug.Log("maxRightDegrees - relativeToY: " + (maxRightDegrees - relativeToY));

        // rotateSideWay1 = Quaternion.identity;
        // rotateSideWay2 = Quaternion.identity;
        rotateDown = Quaternion.identity;

        // down & sideway1
        var startLocalRotation = arm.hand.localRotation;
        var completionTime = allCompletionTime / 4;
        var timeElapsed = 0f;
        var rotateSideWay1Inverse = Quaternion.Inverse(rotateSideWay1);
        var rotateSideWay2Inverse = Quaternion.Inverse(rotateSideWay2);
        var rotateDownInverse = Quaternion.Inverse(rotateDown);

        var initialRotation = arm.hand.localRotation;
        Debug.Log("arm.hand.localRotation: " + initialRotation);
        
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            if (timeElapsed / allCompletionTime > stopAtPercentage) break;
            var rotationToApply = Quaternion.Lerp(Quaternion.identity, rotateSideWay1, (float) Math.Sqrt(t))
                                  * Quaternion.Slerp(Quaternion.identity, rotateDown, t * t);
            _curLocalRotation = rotationToApply * startLocalRotation;
            Debug.DrawRay(arm.hand.position, arm.elbow.rotation * _curLocalRotation * Vector3.up, Color.cyan, 5f);
            // Debug.DrawRay(arm.hand.position, rightHandTarget.rotation * _neutralHandZAxis, Color.green, 5f);
            yield return null;
        }
        
        // down & -sideway1
        startLocalRotation = arm.hand.localRotation;
        timeElapsed = 0f;
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            if ((timeElapsed + 0.25f) / allCompletionTime > stopAtPercentage) break;
            var rotationToApply = Quaternion.Lerp(Quaternion.identity, rotateSideWay1Inverse, t * t) 
                                  * Quaternion.Slerp(Quaternion.identity, rotateDown, (float) Math.Sqrt(t));
            _curLocalRotation = rotationToApply * startLocalRotation;
            Debug.DrawRay(arm.hand.position, arm.elbow.rotation * _curLocalRotation * Vector3.up, Color.green, 5f);
            // Debug.DrawRay(arm.hand.position, rightHandTarget.rotation * _neutralHandZAxis, Color.green, 5f);
            yield return null;
        }

        // up & sideway2
        startLocalRotation = arm.hand.localRotation;
        timeElapsed = 0f;
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            if ((timeElapsed + 0.5f) / allCompletionTime > stopAtPercentage) break;
            var rotationToApply = Quaternion.Slerp(Quaternion.identity, rotateSideWay2, (float) Math.Sqrt(t)) 
                                  * Quaternion.Slerp(Quaternion.identity, rotateDownInverse, t * t);
            _curLocalRotation = rotationToApply * startLocalRotation;
            Debug.DrawRay(arm.hand.position, arm.elbow.rotation * _curLocalRotation * Vector3.up, Color.cyan, 5f);
            // Debug.DrawRay(arm.hand.position, rightHandTarget.rotation * _neutralHandZAxis, Color.green, 5f);
            yield return null;
        }
        
        // up & -sideway2
        startLocalRotation = arm.hand.localRotation;
        timeElapsed = 0f;
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            if ((timeElapsed + 0.75f) / allCompletionTime > stopAtPercentage) break;
            var rotationToApply = Quaternion.Slerp(Quaternion.identity, rotateSideWay2Inverse, t * t) 
                                  * Quaternion.Slerp(Quaternion.identity, rotateDownInverse, (float) Math.Sqrt(t));
            _curLocalRotation = rotationToApply * startLocalRotation;
            Debug.DrawRay(arm.hand.position, arm.elbow.rotation * _curLocalRotation * Vector3.up, Color.green, 5f);
            // Debug.DrawRay(arm.hand.position, rightHandTarget.rotation * _neutralHandZAxis, Color.green, 5f);
            yield return null;
        }
        
        // adjust back to initial rotation
        _curLocalRotation = initialRotation;
        Debug.Log("arm.hand.localRotation: " + arm.hand.localRotation);

        _rotating = false;
    }

    void PointToTarget()
    {
        // global look at
        var epeeLookRotation = Quaternion.LookRotation(bladeTarget.position - epeeTarget.position);
        var rightHandTargetLookRotation = epeeLookRotation * Quaternion.Inverse(epeeTarget.localRotation);
        Debug.DrawRay(rightHandTarget.position,  rightHandTargetLookRotation * Vector3.up, Color.grey, 5f);

        // convert to local space
        Debug.DrawRay(rightHandTarget.position, arm.elbow.rotation * arm.hand.localRotation * Vector3.up, Color.green, 5f);
        var rightHandTargetLocalLookRotation = Quaternion.Inverse(arm.elbow.rotation) * rightHandTargetLookRotation;
        Debug.DrawRay(rightHandTarget.position, arm.elbow.rotation * rightHandTargetLocalLookRotation * Vector3.up, Color.yellow, 5f);
        
        // cap
        var cappedRotation = CapRotation(rightHandTargetLocalLookRotation);
        Debug.Log("cappedRotation: " + cappedRotation);
        
        // set
        _curLocalRotation = cappedRotation * _curLocalRotation;
    }
    
    
    IEnumerator HitTarget()
    {
        _moving = true;

        // TODO: take into consideration arm's degree of freedom
        var expectedFinalPositionEpeeVector = bladeTarget.forward * (float)_epeeLength;
        var expectedFinalPositionVector = (bladeTarget.position - epeeTarget.position) + expectedFinalPositionEpeeVector;
        var startPosition = rightHandTarget.position;
        var endPosition = startPosition + expectedFinalPositionVector;  // moving rightHandTarget = moving epee
        Debug.DrawLine(epeeTarget.position, epeeTarget.position + expectedFinalPositionVector, Color.blue, 10f);
        Debug.DrawLine(startPosition, endPosition, Color.red, 10f);

        var completionTime = 1f;
        var timeElapsed = 0f;
        while (timeElapsed < completionTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / completionTime;
            var progress = (float) Math.Sqrt(t);
            rightHandTarget.position = Vector3.Lerp(startPosition, endPosition, progress);

            // rotation in global space
            var epeeLookRotation = Quaternion.LookRotation(bladeTarget.position - epeeTarget.position);
            var rightHandTargetLookRotation = epeeLookRotation * Quaternion.Inverse(epeeTarget.localRotation);
            Debug.DrawRay(rightHandTarget.position,  rightHandTargetLookRotation * Vector3.up, Color.magenta, 5f);
            // var curToLookRotation = Quaternion.FromToRotation(
            //     arm.hand.rotation * Vector3.one, rightHandTargetLookRotation * Vector3.one).eulerAngles;
            // Debug.Log("FromToRotation(arm.hand.rotation * Vector3.one, rightHandTargetLookRotation * Vector3.one): " + curToLookRotation);
            // Debug.DrawRay(rightHandTarget.position, Quaternion.Euler(curToLookRotation) * Vector3.forward, Color.green, 5f);
            
            var cappedRHTRotation = CapRotation(rightHandTargetLookRotation);
            Debug.Log("cappedRHTRotation: " + cappedRHTRotation.eulerAngles);

            // rotation in local space
            // var rotationToApply =
            //     Quaternion.Euler(cappedRHTRotation.Item1, cappedRHTRotation.Item2, cappedRHTRotation.Item3);
            // Debug.DrawRay(rightHandTarget.position, rotationToApply * Vector3.up, Color.green, 5f);
            
            _curLocalRotation = cappedRHTRotation * _curLocalRotation;
            Debug.DrawRay(rightHandTarget.position, _curLocalRotation * Vector3.up, Color.green, 5f);
            Debug.DrawRay(rightHandTarget.position, _neutralHandLocalRotation * cappedRHTRotation * Vector3.up, Color.cyan, 5f);

            yield return null;
        }
        _moving = false;
    }

    // IEnumerator BackToIdle(bool pointUp)
    // {
    //     var endRotation = Quaternion.AngleAxis(15, fencer.right) * parallelToFloorEpeeRotation;
    //     var endPosition = fencerHip.position + fencer.forward * 0.5f + Vector3.up * 0.1f;
    //
    //     var startRotation = epee.rotation;
    //     var startPosition = epee.position;
    //     var completionTime = 0.3f;
    //     var timeElapsed = 0f;
    //     while (timeElapsed < completionTime)
    //     {
    //         timeElapsed += Time.deltaTime;
    //         var t = timeElapsed / completionTime;
    //         // var progress = (float) Math.Sqrt(t);
    //         epee.position = Vector3.Lerp(startPosition, endPosition, t);
    //         epee.rotation = Quaternion.Lerp(startRotation, endRotation, t);
    //
    //         yield return null;
    //     } 
    // }
    
    # region old codes
    
    // void InitRotateAround(float radius)
    // {
    //     if (radius < (tipMoveDistancePerFrame * 4) / (2 * Math.PI))
    //     {
    //         Debug.Log("radius smaller than " + (tipMoveDistancePerFrame * 4) / (2 * Math.PI));
    //         return;
    //     }
    //
    //     if (radius > _epeeLength)
    //     {
    //         Debug.Log("Radius of " + radius + " is larger than _epeeLength of " + _epeeLength + ", trimmed.");
    //         radius = (float)_epeeLength;
    //     }
    //     _progress = 0f;
    //     var epeeTransform = transform;
    //     var upLookAt = epeeTransform.forward * (float)_epeeLength;
    //     var rotationAngle = (float) Math.Asin(radius / _epeeLength) * Mathf.Rad2Deg;
    //     var centerVectorLength = (float) Math.Sqrt(_epeeLength * _epeeLength - radius * radius);
    //     var toDownRotation = Quaternion.AngleAxis(rotationAngle, -epeeTransform.right);  // rotate around x axis, down
    //     var centerVector = (toDownRotation * upLookAt).normalized * centerVectorLength;
    //
    //     var upVector = upLookAt - centerVector;
    //     var rightVector = transform.right * upVector.magnitude;
    //     Debug.Log("upVector: " + upVector);
    //     Debug.Log("rightVector: " + rightVector);
    //
    //     // Issue: Didn't rotate back to initial rotation.... (ignore first ba)
    //
    //     _lookAts.Add(Quaternion.LookRotation(centerVector + upVector));
    //     _lookAts.Add(Quaternion.LookRotation(centerVector + rightVector));
    //     _lookAts.Add(Quaternion.LookRotation(centerVector - upVector));
    //     _lookAts.Add(Quaternion.LookRotation(centerVector - rightVector));
    //     _lookAts.Add(Quaternion.LookRotation(centerVector + upVector));
    //     _progressStep = tipMoveDistancePerFrame / (float) (2 * Math.PI * radius);
    //
    //     for (int i = 0; i < _lookAts.Count; i++)
    //     {
    //         var lookAt = _lookAts[i];
    //         Debug.DrawRay(epeeTransform.position, lookAt * epeeTransform.forward, Color.green, 5f);
    //     }
    //     Debug.DrawRay(epeeTransform.position, epeeTransform.forward, Color.green, 5f);
    //
    //     DoRotation();
    // }
    //
    // void DoRotation()
    // {
    //     _rotating = true;
    //     Quaternion from, to;
    //     float curSlerpProgress;
    //     if (_progress > 0.75f)
    //     {
    //         curSlerpProgress = _progress - 0.75f;
    //         from = _lookAts[3];
    //         to = _lookAts[4];
    //     } else if (_progress > 0.5f)
    //     {
    //         curSlerpProgress = _progress - 0.5f;
    //         from = _lookAts[2];
    //         to = _lookAts[3];
    //     } else if (_progress > 0.25f)
    //     {
    //         curSlerpProgress = _progress - 0.25f;
    //         from = _lookAts[1];
    //         to = _lookAts[2];
    //     }
    //     else
    //     {
    //         curSlerpProgress = _progress;
    //         from = _lookAts[0];
    //         to = _lookAts[1];
    //     }
    //     curSlerpProgress *= 4;
    //     if (Math.Abs(curSlerpProgress - 1) < 0.000001f)
    //     {
    //         curSlerpProgress = 1f;
    //     }
    //     epee.rotation = Quaternion.Slerp(from, to, curSlerpProgress);
    //
    //     if (Math.Abs(_progress - 1) < 0.000001f)
    //     {
    //         _rotating = false;
    //         _lookAts.Clear();
    //         Debug.Log("end rotation");
    //         return;
    //     }
    //
    //     _progress += _progressStep;
    //     if (_progress > 1)
    //     {
    //         _progress = 1;
    //         Debug.Log("cap progress at 1");
    //     }
    // }
    //
    // void InitMoveWhilePointTo()
    // {
    //     _progress = 0f;
    //     _linePoints.Add(transform.position);
    //     var expectedFinalPositionEpeeVector = bladeTarget.forward * (float)_epeeLength; // so blade target is facing the blade
    //     var expectedFinalPositionVector = (bladeTarget.position - transform.position) + expectedFinalPositionEpeeVector; 
    //     _linePoints.Add(transform.position + expectedFinalPositionVector);
    //     // Debug.Log("expectedFinalPositionVector: " + expectedFinalPositionVector);
    //     Debug.Log("linePoints[0]: " + _linePoints[0]);
    //     Debug.Log("linePoints[1]: " + _linePoints[1]);
    //     Debug.DrawLine(_linePoints[0], _linePoints[1], Color.green, 5f);
    //     if (expectedFinalPositionVector.magnitude == 0)
    //     {
    //         Debug.Log("move distance is 0, return.");
    //         return;
    //     }
    //     _progressStep = tipMoveDistancePerFrame / expectedFinalPositionVector.magnitude;
    //     DoMoveWhilePointTo();
    // }
    //
    // void DoMoveWhilePointTo()
    // {
    //     _moving = true;
    //     
    //     epee.position = Vector3.Lerp(_linePoints[0], _linePoints[1], _progress);
    //     epee.rotation = Quaternion.LookRotation(-(bladeTarget.position - epee.position));
    //
    //     if (Math.Abs(_progress - 1) < 0.000001f)
    //     {
    //         _moving = false;
    //         _linePoints.Clear();
    //         Debug.Log("end moving");
    //         return;
    //     }
    //     _progress += _progressStep;
    //     if (_progress > 1)
    //     {
    //         _progress = 1;
    //         Debug.Log("cap progress at 1");
    //     }
    // }
    
    # endregion

    void EnterEnGarde()
    {
        // defaults to 30 degree upwards
        var lookRotation = Quaternion.RotateTowards(
            Quaternion.LookRotation(fencer.forward), Quaternion.LookRotation(Vector3.up), 30);
        // _rb.angularVelocity 
        
        rightHandTarget.rotation *= lookRotation;
        rightHandTarget.position = fencerHip.position + fencer.forward * 0.5f + Vector3.up * 0.1f;
        Debug.Log("rightHandTarget.position: " + rightHandTarget.position);
        Debug.Log("rightHandTarget.rotation: " + rightHandTarget.rotation);
        _engarding = true;
    }

    void UndoEnGarde()
    {
        var lookRotation = Quaternion.RotateTowards(
            Quaternion.LookRotation(fencer.forward), Quaternion.LookRotation(Vector3.up), 30);
        rightHandTarget.rotation *= Quaternion.Inverse(lookRotation);
        rightHandTarget.position = fencerHip.position + fencer.right * 0.1f - fencer.up * 0.1f;
        Debug.Log("rightHandTarget.position: " + rightHandTarget.position);
        Debug.Log("rightHandTarget.rotation: " + rightHandTarget.rotation);
        _engarding = false;
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(rightHandTarget.position, _neutralHandXAxis, Color.red);
        Debug.DrawRay(rightHandTarget.position, _neutralHandYAxis, Color.green);
        Debug.DrawRay(rightHandTarget.position, _neutralHandZAxis, Color.blue);
    }

    private bool logged = false;
    
    private void FixedUpdate()
    {
        RaycastHit hit;

        Debug.DrawRay(rightHandTarget.position, rightHandTarget.up, Color.green);
        Debug.DrawRay(rightHandTarget.position, rightHandTarget.forward, Color.blue);
        if (!logged)
        {
            Debug.Log("in fixed update");
            logged = true;
        }
        
        if (Physics.Raycast(rightHandTarget.position, rightHandTarget.up, out hit))
            print("Found an object at transform.up, distance: " + hit.distance + "; collider name: " + hit.collider.name);
        if (Physics.Raycast(rightHandTarget.position, rightHandTarget.forward, out hit))
            print("Found an object at transform.forward, distance: " + hit.distance + "; collider name: " + hit.collider.name);
    }


}
