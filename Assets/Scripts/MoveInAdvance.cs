using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MoveInAdvance : MonoBehaviour
{
    public enum FootworkType
    {
        Lunge,
        LungeRecover,
        SmallStepForward,
        StepForward,
        LargeStepForward,
        SmallStepBackward,
        StepBackward,
        LargeStepBackward
    }

    public Transform moveTarget;
    public Transform epeeTarget;
    public Transform wrist;
    public Transform wristOnEpee;
    public int keyFrameOffset;

    public float lungeRigWeight;
    public float lungeCollisionRigWeight;
    public float rigWeightIncreaseRate;
    public float rigWeightDecreaseRate;

    public string stateNamePrefix = "Base Layer.Step.";
    public string entry = "Entry Point";
    public string exit = "Exit";
    
    private Transform _moveTargetRoot;
    private Transform _epee;
    private Dictionary<FootworkType, List<Tuple<Vector3, Quaternion>>> _footworkKeyFrames;
    private Animator _animator;
    private Rig _ikRig;
    private FinalHandController _handController;
    private int _animatorHashStep;
    private bool _inCor;
    private bool _collided;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _handController = GetComponent<FinalHandController>();
        _moveTargetRoot = moveTarget.parent;
        _epee = wristOnEpee.parent;
        _animatorHashStep = Animator.StringToHash("step");
        _footworkKeyFrames = new Dictionary<FootworkType, List<Tuple<Vector3, Quaternion>>>();
        _collided = false;
    }
    
    private FootworkType FootworkTypeFromStepValue(int step)
    {
        switch (step)
        {
            case 1:
                return FootworkType.SmallStepForward;
            case 2:
                return FootworkType.StepForward;
            case 3:
                return FootworkType.LargeStepForward;
            case 4:
                return FootworkType.Lunge;
            case -1:
                return FootworkType.SmallStepBackward;
            case -2:
                return FootworkType.StepBackward;
            case -3:
                return FootworkType.LargeStepBackward;
            default:
                return FootworkType.Lunge;
        }
    }
    
    private String StateNameFromStepValue(int step)
    {
        switch (step)
        {
            case 1:
                return "Small Step Forward";
            case 2:
                return "Regular Step Forward";
            case 3:
                return "Big Step Forward";
            case 4:
                return "Lunge";
            case -1:
                return "Small Step Backward";
            case -2:
                return "Regular Step Backward";
            case -3:
                return "Big Step Backward";
            default:
                return "Lunge";
        }
    }
    
    private IEnumerator SaveKeyFrames(int step)
    {
        var stateName = StateNameFromStepValue(step);
        var footworkType = FootworkTypeFromStepValue(step);

        _footworkKeyFrames[footworkType] = new List<Tuple<Vector3, Quaternion>>();

        var startMoveTargetRootPos = _moveTargetRoot.position;
        var startMoveTargetRootRot = _moveTargetRoot.rotation;

        _animator.SetInteger(_animatorHashStep, step);
        var entryTransitionName = $"{stateNamePrefix}{entry} -> {stateNamePrefix}{stateName}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(entryTransitionName))
        {
            yield return null;
        }

        var exitTransitionName = $"{stateNamePrefix}{stateName} -> {exit}";
        var curTransition = _animator.GetAnimatorTransitionInfo(0);
        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        while (
            curTransition.IsName(entryTransitionName) 
            || curTransition.IsName(exitTransitionName) 
            || curState.IsName(stateName))
        {
            _footworkKeyFrames[footworkType].Add(new Tuple<Vector3, Quaternion>(
                _moveTargetRoot.position - startMoveTargetRootPos, 
                Quaternion.Inverse(startMoveTargetRootRot) * _moveTargetRoot.rotation));
            yield return new WaitForFixedUpdate();
            curTransition = _animator.GetAnimatorTransitionInfo(0);
            curState = _animator.GetCurrentAnimatorStateInfo(0);
        }

        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return null;
        }
        
        Debug.Log($"{stateName} has {_footworkKeyFrames[footworkType].Count} keyframes");
    }
    
    private IEnumerator FollowKeyFrames(FootworkType footworkType)
    {
        // operations on epeeTarget should be increment and not assignment so they can stack on top of one another!
        var moveTargetFromRoot = moveTarget.position - _moveTargetRoot.position;
        var startRootPos = _moveTargetRoot.position;
        var keyFrames = _footworkKeyFrames[footworkType];

        var prevTargetPos = epeeTarget.position;
        for (int i = keyFrameOffset; i < keyFrames.Count - keyFrameOffset; i++)
        {
            var rootPos = startRootPos + keyFrames[i].Item1;
            var targetPos = rootPos + keyFrames[i].Item2 * moveTargetFromRoot;
            var diff = targetPos - prevTargetPos;
            Debug.DrawRay(epeeTarget.position, diff, Color.red, 5f);
            epeeTarget.position += diff.normalized * Mathf.Min(diff.magnitude, _handController.velocity);
            prevTargetPos = targetPos;
            yield return new WaitForFixedUpdate();
        }
    }

    private void PutEpeeTargetInWrist()
    {
        var positionDiff = wrist.position - wristOnEpee.position;
        var rotationDiff = Quaternion.Inverse(wristOnEpee.rotation) * wrist.rotation;

        var curEpeeFromWrist = _epee.position - wristOnEpee.position;
        var rotatedEpeeFromWrist = rotationDiff * curEpeeFromWrist;
        var translatedEpeeFromWrist = positionDiff + rotatedEpeeFromWrist;
        var newPosition = wristOnEpee.position + translatedEpeeFromWrist;
        Debug.DrawLine(epeeTarget.position, newPosition, Color.red, 5f);

        var newRotation = wrist.rotation * Quaternion.Inverse(wristOnEpee.localRotation);
        epeeTarget.position = newPosition;
        epeeTarget.rotation = newRotation;
    }
    
    private IEnumerator FollowAnim()
    {
        var transitionDuration = _animator.GetAnimatorTransitionInfo(0).duration;
        var timeElapsed = 0f;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.fixedDeltaTime * Time.timeScale;
            _ikRig.weight = Mathf.Lerp(1, lungeRigWeight, timeElapsed / transitionDuration);
            PutEpeeTargetInWrist();

            yield return new WaitForFixedUpdate();
        }

        var endTransitionName = $"{stateNamePrefix}Lunge Recover -> {exit}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(endTransitionName))
        {
            if (!_collided)
            {
                PutEpeeTargetInWrist();
                _ikRig.weight = Mathf.Max(
                    _ikRig.weight - rigWeightDecreaseRate * Time.fixedDeltaTime * Time.timeScale, lungeRigWeight);
            }
            else
            {
                _ikRig.weight = Mathf.Min(
                    _ikRig.weight + rigWeightIncreaseRate * Time.fixedDeltaTime * Time.timeScale, lungeCollisionRigWeight);
                _collided = false;  // consumed _collided per frame and reset to true if still colliding
            }

            yield return new WaitForFixedUpdate();
        }
        
        transitionDuration = _animator.GetAnimatorTransitionInfo(0).duration;
        timeElapsed = 0f;
        var startRigWeight = _ikRig.weight;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.fixedDeltaTime * Time.timeScale;
            _ikRig.weight = Mathf.Lerp(startRigWeight, 1, timeElapsed / transitionDuration);
            PutEpeeTargetInWrist();

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator FollowFootwork(int step)
    {
        _inCor = true;

        var stateName = StateNameFromStepValue(step);
        var footworkType = FootworkTypeFromStepValue(step);

        var transitionName = $"{stateNamePrefix}{entry} -> {stateNamePrefix}{stateName}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            yield return null;
        }

        if (footworkType == FootworkType.Lunge)
        {
            // for now won't be able to move/rotate the wrist while lunging
            _handController.DisableControl();
            yield return StartCoroutine(FollowAnim());
            _handController.EnableControl();

            stateName = "Lunge Recover";
        }
        else
        {
            yield return StartCoroutine(FollowKeyFrames(footworkType));
        }

        transitionName = $"{stateNamePrefix}{stateName} -> {exit}";
        while (_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            yield return null;
        }

        _inCor = false;
    }

    private IEnumerator SaveAllKeyFrames()
    {
        _inCor = true;
        yield return StartCoroutine(SaveKeyFrames(2));
        yield return StartCoroutine(SaveKeyFrames(-2));
        yield return StartCoroutine(SaveKeyFrames(3));
        yield return StartCoroutine(SaveKeyFrames(-3));
        _inCor = false;
    }
    
    private void Update()
    {
        if (_inCor) return;
        
        var step = _animator.GetInteger(_animatorHashStep);
        if (step != 0)
        {
            StartCoroutine(FollowFootwork(step));
        }
        
        if (Input.GetKeyUp(KeyCode.A))
        {
            StartCoroutine(SaveAllKeyFrames());
        }
        
        if (Input.GetKeyUp(KeyCode.P))
        {
            var transitionInfo = _animator.GetAnimatorTransitionInfo(0);
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            var potentialTransitions = new String[]
            {
                $"{stateNamePrefix}{entry} -> {stateNamePrefix}Lunge",
                $"{stateNamePrefix}Lunge -> {stateNamePrefix}Lunge Recover",
                $"{stateNamePrefix}Lunge Recover -> {exit}",
            };
            var potentialStates = new String[]
            {
                "Lunge", "Lunge Recover", $"{stateNamePrefix}Lunge", $"{stateNamePrefix}Lunge Recover"
            };
            foreach (var trans in potentialTransitions)
            {
                if (transitionInfo.IsName(trans))
                {
                    Debug.Log($"transition is {trans}");
                }
            }
            foreach (var state in potentialStates)
            {
                if (stateInfo.IsName(state))
                {
                    Debug.Log($"state is {state}");
                }
            }
        }
    }

    public void RegisterCollision()
    {
        _collided = true;
    }
}
