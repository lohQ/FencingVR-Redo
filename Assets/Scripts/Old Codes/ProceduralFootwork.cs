using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class ProceduralFootwork : MonoBehaviour
{
    enum FootAction
    {
        StepFrontFootFront,
        StepBackFootFront,
        StepFrontFootBack,
        StepBackFootBack,
        FullStepFront,
        FullStepBack,
        Lunge,
        LungeRecover
    }
    
    [Serializable]
    public struct FootSole
    {
        public Transform heel;
        public Transform toe;
    }
    
    [Serializable]
    public struct Leg
    {
        public Transform root;
        public Transform knee;
        public Transform foot;

        public Leg(Transform root, Transform knee, Transform foot)
        {
            this.root = root;
            this.knee = knee;
            this.foot = foot;
        }
    }
    
    [Header("Presets")] 
    public float normalSpeed;
    public float fastSpeed;
    public float minFeetWidth;
    public float backFootXOffset;
    public float groundTime;
    public float stepYOffset;
    
    [Header("Footwork Control Parameters")] 
    public float squatDepth;
    

    [Header("Transforms")]
    // backFootTarget and frontFootTarget consist of both source object and hint object
    public Transform backFootTarget;
    public Transform frontFootTarget;
    public Transform fencer;
    public Leg backLeg;
    public Leg frontLeg;
    public SpineAim hipsAim;
    public SpineAim lowerBackAim;
    // used to check grounded-ness, not needed for now
    // public FootSole backFootSole;
    // public FootSole frontFootSole;

    private float _maxFeetWidth;
    private float _legLength;
    private Vector3 _frontFootTargetPosition;
    private Quaternion _frontFootTargetRotation;
    private Vector3 _backFootTargetPosition;
    private Quaternion _backFootTargetRotation;

    private bool _engarding = false;
    public float distanceIncrement = 0.01f;
    public float nextStepDistance;

    private Dictionary<FootAction, KeyCode> _footMoves = new Dictionary<FootAction, KeyCode>();
    private Dictionary<FootAction, List<FootAction>> _nextFootMoves = new Dictionary<FootAction, List<FootAction>>();

    private void Start()
    {
        _legLength = (frontLeg.knee.position - frontLeg.root.position).magnitude +
                     (frontLeg.foot.position - frontLeg.knee.position).magnitude;
        nextStepDistance = 0;
        _footMoves[FootAction.StepFrontFootFront] = KeyCode.UpArrow;
        _footMoves[FootAction.StepFrontFootBack] = KeyCode.DownArrow;
        _footMoves[FootAction.StepBackFootFront] = KeyCode.LeftArrow;
        _footMoves[FootAction.StepBackFootBack] = KeyCode.RightArrow;
        _footMoves[FootAction.Lunge] = KeyCode.Space;
        _footMoves[FootAction.LungeRecover] = KeyCode.Space;
        _nextFootMoves[FootAction.StepFrontFootFront] = new List<FootAction>
        {
            FootAction.StepFrontFootFront,
            FootAction.StepFrontFootBack,
            FootAction.StepBackFootFront,
            FootAction.StepBackFootBack,
            FootAction.Lunge
        };
        _nextFootMoves[FootAction.StepFrontFootBack] = _nextFootMoves[FootAction.StepFrontFootFront];
        _nextFootMoves[FootAction.StepBackFootFront] = _nextFootMoves[FootAction.StepFrontFootFront];
        _nextFootMoves[FootAction.StepBackFootBack] = _nextFootMoves[FootAction.StepFrontFootFront];
        _nextFootMoves[FootAction.Lunge] = new List<FootAction>{FootAction.LungeRecover};
        _nextFootMoves[FootAction.LungeRecover] = _nextFootMoves[FootAction.StepFrontFootFront];
        
        _backFootTargetPosition = backLeg.foot.position;
        _frontFootTargetPosition = frontLeg.foot.position;
        EnterEnGarde();
    }

    private void Update()
    {
        if (_engarding)
        {
            // if (_curAction == FootAction.StepFrontFootFront || 
            //     _curAction == FootAction.StepFrontFootBack ||
            //     _curAction == FootAction.StepBackFootBack ||
            //     _curAction == FootAction.StepBackFootFront)
            // {
            //     DoStep();
            //     return;
            // }

            if (_curAction == null)
            {
                FootAction? newAction = null;
                foreach (var footAction in _nextAvailActions)
                {
                    if (Input.GetKeyUp(_footMoves[footAction]))
                    {
                        newAction = footAction;
                    }
                }

                if (newAction != null)
                {
                    if (newAction == FootAction.Lunge)
                    {
                        StartCoroutine(Lunge());
                    }
                    else if (newAction == FootAction.LungeRecover)
                    {
                        StartCoroutine(LungeRecover());
                    }
                    else
                    {
                        StartCoroutine(StepFoot(nextStepDistance, normalSpeed, (FootAction)newAction));
                        nextStepDistance = 0f;
                    }
                } else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                {
                    nextStepDistance += distanceIncrement;
                }
            }

        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (_engarding)
            {
                UndoEnGarde();
            }
            else
            {
                EnterEnGarde();
            }
        }

        frontFootTarget.position = _frontFootTargetPosition;
        frontFootTarget.rotation = _frontFootTargetRotation;
        backFootTarget.position = _backFootTargetPosition;
        backFootTarget.rotation = _backFootTargetRotation;

        // if (Input.GetKeyUp(KeyCode.Return))
        // {
        //     Debug.Log("front foot is grounded: " + IsGrounded(frontFootSole));
        //     Debug.Log("back foot is grounded: " + IsGrounded(backFootSole));
        // }

    }

    private Vector3 _BackFootDir()
    {
        return -fencer.right;
    }

    private float FencerHeight(float feetWidth)
    {
        // TODO: find the proper way to model fencer's height change
        // _maxFeetWidth is for fencer.forward axis
        var calfLength = (frontLeg.knee.position - frontLeg.foot.position).magnitude;
        _maxFeetWidth = (frontLeg.root.position - frontLeg.knee.position).magnitude 
                        + (float)Math.Sqrt(_legLength * _legLength - calfLength * calfLength);
        // var originPosition = new Vector3(fencer.position.x, 0, fencer.position.y);
        // var startPoint = originPosition + Vector3.up * calfLength;
        // var endPoint = originPosition + Vector3.up * _legLength;
        var t = (feetWidth - minFeetWidth) / _maxFeetWidth;
        var cur = (float) Math.Sqrt(t);
        var height = Mathf.Lerp(_legLength, calfLength, cur) - hipsYOffset;
        Debug.Log("with feetWidth of " + feetWidth + ", fencer height should be " + height);
        // Debug.Log("with this height the feetWidth should be: " + FencerFeetWidth(height));
        return height;
    }
    
    private float FencerFeetWidth(float height)
    {
        var calfLength = (frontLeg.knee.position - frontLeg.foot.position).magnitude;
        _maxFeetWidth = (frontLeg.root.position - frontLeg.knee.position).magnitude 
                        + (float)Math.Sqrt(_legLength * _legLength - calfLength * calfLength);

        // var gradient = (minFeetWidth - _maxFeetWidth) / (_legLength - calfLength);
        // Debug.Log("gradient: " + gradient);
        
        var t = (height - calfLength) / (_legLength - calfLength);
        var cur = (float) Math.Sqrt(t);
        var feetWidth = Mathf.Lerp( _maxFeetWidth, 0f, cur);
        // var feetWitdh = (height - calfLength) * gradient + _maxFeetWidth;
        Debug.Log("with height of " + height + ", fencer feet width should be " + feetWidth);
        Debug.Log("with this feetwidth the height should be: " + FencerHeight(feetWidth));
        return feetWidth;
    }

    private void EnterEnGarde()
    {
        var backFootDir = _BackFootDir();
        _frontFootTargetRotation = frontLeg.foot.rotation;
        _backFootTargetRotation = Quaternion.LookRotation(backFootDir) * backLeg.foot.rotation;

        // later try move fencer forward a bit?
        var fencerPos = fencer.position;
        var initialFeetWidth = FencerFeetWidth(_legLength - squatDepth);
        _frontFootTargetPosition =
            new Vector3(fencerPos.x, frontLeg.foot.position.y, fencerPos.z) + fencer.forward * initialFeetWidth;
        _backFootTargetPosition = 
            new Vector3(fencerPos.x, backLeg.foot.position.y, fencerPos.z)  + backFootDir * backFootXOffset;
        // fencer.position = fencerPos - Vector3.up * squatDepth;
        fencer.position = new Vector3(fencer.position.x, -squatDepth, fencer.position.z) + initialFeetWidth / 4 * fencer.forward;

        hipsAim.SetPosition();
        lowerBackAim.SetPosition();

        _engarding = true;
        _nextAvailActions = new List<FootAction>
        {
            FootAction.StepFrontFootFront,
            FootAction.StepFrontFootBack,
            FootAction.StepBackFootFront,
            FootAction.StepBackFootBack,
            FootAction.Lunge
        };
    }

    private void UndoEnGarde()
    {
        var backFootDir = _BackFootDir();
        _backFootTargetRotation = Quaternion.LookRotation(-backFootDir) * _backFootTargetRotation;

        var fencerPos = fencer.position - DistanceBetweenFeet() / 4 * fencer.forward;
        _backFootTargetPosition = 
            new Vector3(fencerPos.x, backFootTarget.position.y, fencerPos.z) + backFootDir * 0.1f;
        _frontFootTargetPosition = 
            new Vector3(fencerPos.x, frontFootTarget.position.y, fencerPos.z) - backFootDir * 0.1f;
        // fencer.position = fencerPos + fencer.up * squatDepth;
        fencer.position = new Vector3(fencerPos.x, 0, fencerPos.z);

        _engarding = false;
        _nextAvailActions.Clear();
    }

    private void UpdateFencerHeight()
    {
        // var newHeight = (_legLength - squatDepth) - (DistanceBetweenFeet() - initialFeetWidth) * _squatToFeetWidthRatio;
        // if (newHeight > (_legLength - squatDepth)) {
        //     newHeight = _legLength - squatDepth;
        //     // Debug.Log("newHeight is larger than _legLength - squatDepth, trimmed");
        //     // Debug.Log("DistanceBetweenFeet is " + DistanceBetweenFeet() + ", minFeetWidth is " + minFeetWidth + ", _squatToFeetWidthRatio is " + _squatToFeetWidthRatio);
        // }
        // var calfLength = (frontLeg.knee.position - frontLeg.foot.position).magnitude;
        // if (newHeight < (_legLength - calfLength))
        // {
        //     newHeight = _legLength - calfLength;
        //     // Debug.Log("newHeight is smaller than _legLength - maxSquatDepth, trimmed");
        //     // Debug.Log("DistanceBetweenFeet is " + DistanceBetweenFeet() + ", minFeetWidth is " + minFeetWidth + ", _squatToFeetWidthRatio is " + _squatToFeetWidthRatio);
        // }
        // // Debug.Log("newHeight: " + newHeight);
        // fencer.position += Vector3.up * ((newHeight - _legLength) - fencer.position.y);
        fencer.position = new Vector3(fencer.position.x, FencerHeight(DistanceBetweenFeet()), fencer.position.z);
    }
    
    private int _maxAdjustIteration;
    
    private void AdjustFencerPos()
    {
        /*
         * Imagine a line between front foot and back foot.
         * This function adjusts fencer position so it is at the back side instead of the front side of the line.
         */
        var fencerPos = fencer.position;
        var fencerBackFootDirection = -fencer.right;
        var fencerForwardVector = fencer.forward;

        var frontToCenter = (fencerPos - frontLeg.foot.position).normalized;
        var centerToBack = (backLeg.foot.position - fencerPos).normalized;
        var frontToCenterProjection = Vector3.Project(frontToCenter, fencerBackFootDirection).magnitude;
        var centerToBackProjection = Vector3.Project(centerToBack, fencerBackFootDirection).magnitude;

        _maxAdjustIteration = 5;
        while (_maxAdjustIteration > 0 && frontToCenterProjection > centerToBackProjection)
        {
            fencer.position -= fencerForwardVector * 0.01f;    // observed that usually it is too forward. So just manually move it backward a bit
            fencer.position -= fencerBackFootDirection * 0.01f;
            frontToCenter = (fencerPos - frontLeg.foot.position).normalized;
            centerToBack = (backLeg.foot.position - fencerPos).normalized;
            frontToCenterProjection = Vector3.Project(frontToCenter, fencerBackFootDirection).magnitude;
            centerToBackProjection = Vector3.Project(centerToBack, fencerBackFootDirection).magnitude;
            _maxAdjustIteration -= 1;
        }

        if (_maxAdjustIteration > 0)
        {
            hipsAim.SetPosition();
            lowerBackAim.SetPosition();
        }
    }
    
    private float DistanceBetweenFeet()
    {
        var backFootDirection = _BackFootDir();
        var result = (frontLeg.foot.position - (backLeg.foot.position - backFootDirection * backFootXOffset)).magnitude;
        // looks correct
        // Debug.DrawLine(frontLeg.foot.position, (backLeg.foot.position - backFootDirection * backFootXOffset), Color.white, 5f);
        // Debug.Log("DistanceBetweenFeet: " + result);
        return result;
    }

    private List<FootAction> _nextAvailActions;
    [CanBeNull] private FootAction? _curAction;

    # region Previous step codes
    
    // private void InitStep(float distance, float speed, FootAction footAction)
    // {
    //     // first set the required variables
    //     var fencerForward = fencer.forward;
    //     switch (footAction)
    //     {
    //         case FootAction.StepFrontFootFront:
    //             distance = CheckStepDistance(true, true, distance);
    //             if (distance == 0) return;
    //             _moveTarget = frontFootTarget;
    //             _startPoint = frontFootTarget.position;
    //             _endPoint = _startPoint + fencerForward * distance;
    //             break;
    //         case FootAction.StepFrontFootBack:
    //             distance = CheckStepDistance(true, false, distance);
    //             if (distance == 0) return;
    //             _moveTarget = frontFootTarget;
    //             _startPoint = frontFootTarget.position;
    //             _endPoint = _startPoint - fencerForward * distance;
    //             break;
    //         case FootAction.StepBackFootFront:
    //             distance = CheckStepDistance(false, true, distance);
    //             if (distance == 0) return;
    //             _moveTarget = backFootTarget;
    //             _startPoint = backFootTarget.position;
    //             _endPoint = _startPoint + fencerForward * distance;
    //             break;
    //         case FootAction.StepBackFootBack:
    //             distance = CheckStepDistance(false, false, distance);
    //             if (distance == 0) return;
    //             _moveTarget = backFootTarget;
    //             _startPoint = backFootTarget.position;
    //             _endPoint = _startPoint - fencerForward * distance;
    //             break;
    //         default:
    //             Debug.Log("Unexpected step action. Return");
    //             return;
    //     }
    //     _progress = 0;
    //     _speed = speed;
    //     _startBodyPosition = fencer.position;
    //     // length of the triangle path
    //     _distance = (float)Math.Sqrt(distance*distance + stepYOffset * stepYOffset);
    //     _curAction = footAction;
    //     _nextAvailActions = _nextFootMoves[footAction];
    //     Debug.Log("Desired forward distance: " + distance + "; path distance: " + _distance);
    //     Debug.Log("Speed: " + speed);
    //
    //     DoStep();
    // }

    // private void DoStep(){
    //     // move according to progress
    //     if (_progress < 1)
    //     {
    //         InternalStep();
    //         _progress += _speed * Time.deltaTime / _distance;
    //         if (_progress > 1)
    //         {
    //             _progress = 1;
    //         }
    //     }
    //     // make sure it moved till 1!
    //     else if (_progress < 2)
    //     {
    //         _progress = 1;
    //         InternalStep();
    //         _progress = 3;
    //         _remainingGroundTime = groundTime;
    //     }
    //     // then pause and ground the foot
    //     else
    //     {
    //         if (Ground())
    //         {
    //             // when grounded, end current action
    //             _curAction = null;
    //             _progress = 0;
    //             Debug.Log("Grounded");
    //             
    //         }
    //     }
    // }

    // private void InternalStep()
    // {
    //     var oldPosition = _moveTarget.position;
    //     var oldFencerPosition = fencer.position;
    //     _moveTarget.position = _startPoint + (_endPoint - _startPoint) * _progress;
    //     if (_progress < 0.75f)
    //     {
    //         _moveTarget.position += Vector3.up * stepYOffset * _progress / 0.75f;
    //     }
    //     else
    //     {
    //         _moveTarget.position += Vector3.up * stepYOffset * (1 - _progress) / 0.25f;
    //     }
    //
    //     fencer.position = _startBodyPosition + (_endPoint - _startPoint) * _progress / 2;
    //     UpdateFencerHeight();
    //
    //     Debug.DrawLine(oldFencerPosition, fencer.position, Color.blue, 5f);
    //     Debug.DrawLine(oldPosition, _moveTarget.position, Color.green, 5f);
    // }

    # endregion
    
    private float CheckStepDistance(bool frontFoot, bool forward, float distance)
    {
        float maxDistance;
        if (frontFoot != forward)
        {
            // frontFoot but move backward, or backFoot but move forward 
            maxDistance = DistanceBetweenFeet() - minFeetWidth;
        }
        else
        {
            // frontFoot move forward, backFoot move backward
            maxDistance = _maxFeetWidth - DistanceBetweenFeet();
        }
        if (maxDistance < 0) maxDistance = 0;
        Debug.Log("distance: " + distance + "; maxDistance: " + maxDistance);
        return distance > maxDistance ? maxDistance : distance;
    }
    
    private IEnumerator StepFoot(float distance, float speed, FootAction footAction)
    {
        // TODO: use Lerp
        bool moveFront = false;
        Vector3 startPoint = Vector3.zero;
        Vector3 endPoint = Vector3.zero;

        var fencerForward = fencer.forward;
        switch (footAction)
        {
            case FootAction.StepFrontFootFront:
                distance = CheckStepDistance(true, true, distance);
                if (distance == 0) break;
                moveFront = true;
                startPoint = frontFootTarget.position;
                endPoint = startPoint + fencerForward * distance;
                break;
            case FootAction.StepFrontFootBack:
                distance = CheckStepDistance(true, false, distance);
                if (distance == 0) break;
                moveFront = true;
                startPoint = frontFootTarget.position;
                endPoint = startPoint - fencerForward * distance;
                break;
            case FootAction.StepBackFootFront:
                distance = CheckStepDistance(false, true, distance);
                if (distance == 0) break;
                moveFront = false;
                startPoint = backFootTarget.position;
                endPoint = startPoint + fencerForward * distance;
                break;
            case FootAction.StepBackFootBack:
                distance = CheckStepDistance(false, false, distance);
                if (distance == 0) break;
                moveFront = false;
                startPoint = backFootTarget.position;
                endPoint = startPoint - fencerForward * distance;
                break;
            default:
                distance = 0;
                Debug.Log("Unexpected step action. Return");
                break;
        }

        if (distance != 0)
        {
            var lerpDuration = distance / speed;
            Debug.Log("lerpDuration: " + lerpDuration);
            var startBodyPosition = fencer.position;
            _curAction = footAction;
            _nextAvailActions = _nextFootMoves[footAction]; // used in lunge so shouldn't do this
            
            float timeElapsed = 0;
            var progress = timeElapsed / lerpDuration;
            while (progress <= 1)
            {
                var oldPosition = moveFront ? _frontFootTargetPosition : _backFootTargetPosition;
                var oldFencerPosition = fencer.position;

                var newPosition = startPoint + (endPoint - startPoint) * progress;
                if (progress < 0.75f)
                {
                    newPosition += Vector3.up * stepYOffset * progress / 0.75f;
                }
                else
                {
                    newPosition += Vector3.up * stepYOffset * (1 - progress) / 0.25f;
                }

                if (moveFront)
                {
                    _frontFootTargetPosition = newPosition;
                }
                else
                {
                    _backFootTargetPosition = newPosition;
                }

                fencer.position = startBodyPosition + (endPoint - startPoint) * progress / 2;
                UpdateFencerHeight();

                Debug.DrawLine(oldFencerPosition, fencer.position, Color.blue, 5f);
                Debug.DrawLine(oldPosition, newPosition, Color.green, 5f);
                Debug.Log("progress: " + progress + "; moveTargetPosition: " + newPosition);

                if (Math.Abs(progress - 1) < 0.000001)
                {
                    break;
                }

                timeElapsed += Time.deltaTime;
                progress = timeElapsed / lerpDuration;
                if (progress > 1) progress = 1;
                yield return null;
            }

            // ground
            timeElapsed = 0;
            while (timeElapsed < groundTime)
            {
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            _curAction = null;
            Debug.Log("Grounded");
        }
        
    }

    private IEnumerator FullStep(float distance, float speed, FootAction footAction)
    {
        if (footAction == FootAction.FullStepFront)
        {
            yield return StartCoroutine(StepFoot(distance, speed, FootAction.StepFrontFootFront));
            yield return StartCoroutine(StepFoot(distance, speed, FootAction.StepBackFootFront));
        } else if (footAction == FootAction.FullStepBack)
        {
            yield return StartCoroutine(StepFoot(distance, speed, FootAction.StepBackFootBack));
            yield return StartCoroutine(StepFoot(distance, speed, FootAction.StepFrontFootBack));
        }
    }

    public float kickTime = 0.5f;
    public float landTime = 0.1f;
    public float hipsYOffset = 0.93f;
    
    private IEnumerator Lunge()
    {
        _curAction = FootAction.Lunge;
        _nextAvailActions = _nextFootMoves[(FootAction)_curAction];
        
        // TODO: fix backFoot hint position
        // TODO: kick higher?

        // (back foot pushes) body move forward & downward
        var oldBodyPosition = fencer.position;
        var calfLength = (frontLeg.knee.position - frontLeg.foot.position).magnitude;
        var desiredBodyPositionFromBackFoot = (float) Math.Sqrt(_legLength * _legLength - calfLength * calfLength);
        var endBodyPosition = backLeg.foot.position + backFootXOffset * fencer.right + desiredBodyPositionFromBackFoot * fencer.forward;
        endBodyPosition.y = frontLeg.foot.position.y + calfLength - hipsYOffset;
        var midBodyPosition = oldBodyPosition + (endBodyPosition - oldBodyPosition) * 0.8f;
        // Debug.Log("calfLength: " + calfLength + "; frontLeg.foot.position.y: " + frontLeg.foot.position.y);
        // Debug.Log("oldBodyPosition: " + oldBodyPosition + ", endBodyPosition: " + endBodyPosition + ", midBodyPosition: " + midBodyPosition);
        
        // (on top of body move forward) front foot kick up
        var desiredRotation = Quaternion.FromToRotation(
            (frontLeg.foot.position - frontLeg.knee.position),
            (frontLeg.knee.position - frontLeg.root.position)
        );
        var oldFootPosition = frontLeg.foot.position;
        var curCalfVector = frontLeg.foot.position - frontLeg.knee.position;
        var desiredCalfVector = desiredRotation * curCalfVector;
        var midFootPosition = frontLeg.knee.position + desiredCalfVector;
        var oldFootRotation = frontLeg.foot.rotation;
        var midFootRotation = Quaternion.AngleAxis(45, -fencer.right) * oldFootRotation;
        // Debug.Log("oldFootPosition: " + oldFootPosition + ", midFootPosition: " + midFootPosition);
        // Debug.Log("oldFootRotation.eulerAngles: " + oldFootRotation.eulerAngles + ", midFootRotation.eulerAngles: " + midFootRotation.eulerAngles);
        
        // var kickTime = 0.5f;
        var timeElapsed = 0f;
        while (timeElapsed < kickTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / kickTime;
            var progress = 1.01f * (float) Math.Sqrt(t);
            var newFencerPosition = Vector3.Lerp(
                oldBodyPosition, midBodyPosition, progress);     // 1.01f so there's a time that the body done moving
            var fencerPositionDiff = newFencerPosition - oldBodyPosition;
            fencer.position = newFencerPosition;
            _frontFootTargetPosition = Vector3.Lerp(oldFootPosition, midFootPosition, progress) + fencerPositionDiff;
            _frontFootTargetRotation = Quaternion.Lerp(oldFootRotation, midFootRotation, progress);
            
            yield return null;
        }

        // then front foot land (and body move forward)
        var thighLength = (frontLeg.root.position - frontLeg.knee.position).magnitude;
        var endFootPosition = backLeg.foot.position + backFootXOffset * fencer.right + fencer.forward * (desiredBodyPositionFromBackFoot + thighLength);
        // Debug.Log("endFootPosition: " + endFootPosition);
        // var landTime = 0.1f;
        timeElapsed = 0f;
        while (timeElapsed < landTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / landTime;
            var newFencerPosition = Vector3.Lerp(midBodyPosition, endBodyPosition, t);
            var fencerPositionDiff = newFencerPosition - fencer.position;
            fencer.position = newFencerPosition;
            _frontFootTargetPosition = Vector3.Lerp(midFootPosition, endFootPosition, t) + fencerPositionDiff;
            _frontFootTargetRotation = Quaternion.Lerp(midFootRotation, oldFootRotation, t);
            
            yield return null;
        }

        var lungeGroundTime = 1f;
        timeElapsed = 0f;
        while (timeElapsed < lungeGroundTime)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _curAction = null;
    }
    
    private IEnumerator LungeRecover()
    {
        _curAction = FootAction.LungeRecover;
        _nextAvailActions = _nextFootMoves[(FootAction)_curAction];
        
        // shift body position relative to both legs
        var initialFeetWidth = FencerFeetWidth(_legLength - squatDepth);
        var oldBodyPosition = fencer.position;
        var endBodyPosition = backLeg.foot.position + fencer.right * backFootXOffset + fencer.forward * initialFeetWidth / 2;
        endBodyPosition.y = -squatDepth;
        var toMidBodyVector = (endBodyPosition - oldBodyPosition) / 2;
        var midBodyPosition = oldBodyPosition + toMidBodyVector;
        midBodyPosition.y = oldBodyPosition.y;
        Debug.Log("oldBodyPosition: " + oldBodyPosition + "; endBodyPosition: " + endBodyPosition + "; midBodyPosition: " + midBodyPosition);

        var moveBodyTime = 0.2f;
        var timeElapsed = 0f;
        while (timeElapsed < moveBodyTime)
        {
            timeElapsed += Time.deltaTime;
            var t = timeElapsed / moveBodyTime;
            fencer.position = Vector3.Lerp(oldBodyPosition, midBodyPosition, (float) Math.Sqrt(t));

            yield return null;
        }
        
        // step backwards
        var distance = (frontLeg.foot.position - backLeg.foot.position + fencer.right * backFootXOffset).magnitude - initialFeetWidth;
        var stepBackwardTime = 0.1f;
        var speed = distance / stepBackwardTime;
        Debug.Log("distance: " + distance + "; speed: " + speed);
        yield return StartCoroutine(StepFoot(distance, speed, FootAction.StepFrontFootBack));
    }
    
    // private bool IsGrounded(FootSole footSole)
    // {
    //     // yes means the ground must be at y=0
    //     // fk flexibility. I don't need that. 
    //     if (footSole.heel.position.y <= 0 || footSole.toe.position.y <= 0)
    //     {
    //         return true;
    //     }
    //     Debug.Log("footSole.heel.position.y: " + footSole.heel.position.y);
    //     Debug.Log("footSole.toe.position.y: " + footSole.toe.position.y);
    //
    //     return false;
    // }

    // private void OnDrawGizmos()
    // {
    //     if (_started)
    //     {
    //         Gizmos.DrawWireSphere(frontFootTarget.GetChild(0).position, 0.08f);
    //         Gizmos.DrawWireSphere(backFootTarget.GetChild(0).position, 0.08f);
    //         Gizmos.DrawWireSphere(frontFootTarget.GetChild(1).position, 0.02f);
    //         Gizmos.DrawWireSphere(backFootTarget.GetChild(1).position, 0.02f);
    //     }
    // }
    
}
