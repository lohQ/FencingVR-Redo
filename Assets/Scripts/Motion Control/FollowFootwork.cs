using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

// still doesn't really look good but at least it's better than without this script...
// use fixedDeltaTime instead of deltaTime for better granularity

public class FollowFootwork : MonoBehaviour
{
    [Serializable]
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

    [Serializable]
    public struct FootworkKeyFrame
    {
        public FootworkType footworkType;
        public MoveTargetRootKeyFrames keyFrameData;
    }

    public Transform moveTarget;
    public Transform epeeTarget;
    public Transform wrist;
    public Transform wristOnEpee;
    public FootworkKeyFrame[] footworkKeyFrames;
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
    private Animator _animator;
    private Rig _ikRig;
    private FinalHandController _handController;
    
    private Dictionary<FootworkType, MoveTargetRootKeyFrames> _footworkKeyFrameDict;
    private int _animatorHashStep;
    private bool _inCor;
    private bool _collided;
    private bool _bladeworkDisabled;

    private void Start()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _animator = GetComponent<Animator>();
        _handController = GetComponent<FinalHandController>();
        _moveTargetRoot = moveTarget.parent;
        _epee = wristOnEpee.parent;
        _animatorHashStep = Animator.StringToHash("step");
        _collided = false;
        _bladeworkDisabled = false;

        _footworkKeyFrameDict = new Dictionary<FootworkType, MoveTargetRootKeyFrames>();
        foreach (var pair in footworkKeyFrames)
        {
            _footworkKeyFrameDict[pair.footworkType] = pair.keyFrameData;
        }
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
    
    private string StateNameFromStepValue(int step)
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

        var keyFrameSO = _footworkKeyFrameDict[footworkType];
        keyFrameSO.translationData = new List<Vector3>();
        keyFrameSO.rotationData = new List<Quaternion>();

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
            keyFrameSO.translationData.Add(_moveTargetRoot.position - startMoveTargetRootPos);
            keyFrameSO.rotationData.Add(Quaternion.Inverse(startMoveTargetRootRot) * _moveTargetRoot.rotation);
            yield return new WaitForFixedUpdate();
            curTransition = _animator.GetAnimatorTransitionInfo(0);
            curState = _animator.GetCurrentAnimatorStateInfo(0);
        }

        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return null;
        }
        
        Debug.Log($"{stateName} has {keyFrameSO.translationData.Count} keyframes");
    }
    
    private IEnumerator FollowKeyFrames(FootworkType footworkType)
    {
        // operations on epeeTarget should be increment and not assignment so they can stack on top of one another!
        var startRootPos = _moveTargetRoot.position;
        var moveTargetFromRoot = moveTarget.position - startRootPos;
        var translationData = _footworkKeyFrameDict[footworkType].translationData;
        var rotationData = _footworkKeyFrameDict[footworkType].rotationData;

        var prevTargetPos = epeeTarget.position;
        for (int i = keyFrameOffset; i < translationData.Count - keyFrameOffset; i++)
        {
            var rootPos = startRootPos + translationData[i];
            var targetPos = rootPos + rotationData[i] * moveTargetFromRoot;
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

    private IEnumerator DoFollowFootwork(int step)
    {
        _inCor = true;
        Debug.Log("Enter DoFollowFootwork");

        var stateName = StateNameFromStepValue(step);
        var footworkType = FootworkTypeFromStepValue(step);

        var transitionName = $"{stateNamePrefix}{entry} -> {stateNamePrefix}{stateName}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            yield return null;
        }
        Debug.Log($"Enter transition {transitionName}");

        if (footworkType == FootworkType.Lunge)
        {
            // for now won't be able to move/rotate the wrist while lunging! At most react to collision
            _handController.DisableControl();
            _bladeworkDisabled = true;
            yield return StartCoroutine(FollowAnim());
            _handController.EnableControl();
            _bladeworkDisabled = false;

            stateName = "Lunge Recover";
        }
        else
        {
            if (footworkType != FootworkType.SmallStepForward && footworkType != FootworkType.SmallStepBackward)
            {
                yield return StartCoroutine(FollowKeyFrames(footworkType));
            }

            transitionName = $"{stateNamePrefix}{stateName} -> {exit}";
                yield return new WaitWhile(
                    () => !_animator.GetAnimatorTransitionInfo(0).IsName(transitionName));
                Debug.Log($"Enter transition {transitionName}");
        }

        transitionName = $"{stateNamePrefix}{stateName} -> {exit}";
        while (_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            yield return null;
        }
        Debug.Log($"Exit transition {transitionName}");

        Debug.Log("Exit DoFollowFootwork");
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
            StartCoroutine(DoFollowFootwork(step));
        }
        
        // use this to re-save the scriptableObjects
        if (Input.GetKeyUp(KeyCode.A))
        {
            StartCoroutine(SaveAllKeyFrames());
        }
    }

    public void RegisterCollision()
    {
        _collided = true;
    }

    public bool BladeworkDisabled()
    {
        return _bladeworkDisabled;
    }
}
