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
    public FootworkKeyFrame[] footworkKeyFrames;
    public int keyFrameOffset;

    public string stateNamePrefix = "Base Layer.Step.";
    public string entry = "Entry Point";
    public string exit = "Exit";
    
    // debug
    public bool debug;
    public MeshRenderer footworkDisplay;
    public Material enabledColor;
    public Material disabledColor;
    
    private Transform _moveTargetRoot;
    private Animator _animator;
    private FinalHandController _handController;
    
    private Dictionary<FootworkType, MoveTargetRootKeyFrames> _footworkKeyFrameDict;
    private int _animatorHashStep;
    private bool _inCor;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _handController = GetComponent<FinalHandController>();
        _moveTargetRoot = moveTarget.parent;
        _animatorHashStep = Animator.StringToHash("step");

        _footworkKeyFrameDict = new Dictionary<FootworkType, MoveTargetRootKeyFrames>();
        foreach (var pair in footworkKeyFrames)
        {
            _footworkKeyFrameDict[pair.footworkType] = pair.keyFrameData;
        }
        footworkDisplay.material = enabledColor;
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
    
    
    
    // -- below: for saving data into scriptable object --//
    
    private IEnumerator SaveKeyFrames(int step, string recoverStateName = "")
    {
        // TODO: make this independent of root rotation
        var stateName = StateNameFromStepValue(step);
        var footworkType = FootworkTypeFromStepValue(step);

        var keyFrameSO = _footworkKeyFrameDict[footworkType];
        var newTranslationData = new List<Vector3>();
        var newRotationData = new List<Quaternion>();

        var startMoveTargetRootPos = _moveTargetRoot.position;
        var startMoveTargetRootRot = _moveTargetRoot.rotation;

        _animator.SetInteger(_animatorHashStep, step);
        var entryTransitionName = $"{stateNamePrefix}{entry} -> {stateNamePrefix}{stateName}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(entryTransitionName))
        {
            yield return new WaitForFixedUpdate();
            _animator.SetInteger(_animatorHashStep, step);
        }

        var recoverTransitionName = $"{stateNamePrefix}{stateName} -> {stateNamePrefix}{recoverStateName}";
        var exitTransitionName = $"{stateNamePrefix}{stateName} -> {exit}";
        if (recoverStateName != "")
        {
            exitTransitionName = $"{stateNamePrefix}{recoverStateName} -> {exit}";
        }

        var curTransition = _animator.GetAnimatorTransitionInfo(0);
        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        while (
            curTransition.IsName(entryTransitionName) 
            || curState.IsName(stateName)
            || curTransition.IsName(recoverTransitionName)
            || curState.IsName(recoverStateName)
            || curTransition.IsName(exitTransitionName) 
            )
        {
            newTranslationData.Add(_moveTargetRoot.position - startMoveTargetRootPos);
            newRotationData.Add(Quaternion.Inverse(startMoveTargetRootRot) * _moveTargetRoot.rotation);
            yield return new WaitForFixedUpdate();
            curTransition = _animator.GetAnimatorTransitionInfo(0);
            curState = _animator.GetCurrentAnimatorStateInfo(0);
        }
        
        keyFrameSO.WriteTranslationData(newTranslationData);
        keyFrameSO.WriteRotationData(newRotationData);

        _animator.SetInteger(_animatorHashStep, 0);
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return new WaitForFixedUpdate();
        }
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
    
    private IEnumerator SaveLungeKeyFrames()
    {
        _inCor = true;
        yield return StartCoroutine(SaveKeyFrames(4, "Lunge Recover"));
        _inCor = false;
    }

    // -- above: for saving data into scriptable object --//

    
    
    private IEnumerator FollowKeyFrames(FootworkType footworkType)
    {
        // operations on epeeTarget should be increment and not assignment so they can stack on top of one another!
        var startRootPos = _moveTargetRoot.position;
        var moveTargetFromRoot = moveTarget.position - startRootPos;
        var translationData = _footworkKeyFrameDict[footworkType].cloneTranslationData;
        var rotationData = _footworkKeyFrameDict[footworkType].cloneRotationData;

        var prevTargetPos = epeeTarget.position;
        for (int i = keyFrameOffset; i < translationData.Count - keyFrameOffset; i++)
        {
            var rootPos = startRootPos + translationData[i];
            var targetPos = rootPos + rotationData[i] * moveTargetFromRoot;
            var diff = targetPos - prevTargetPos;
            if (debug) Debug.DrawRay(epeeTarget.position, diff, Color.red, 5f);
            
            epeeTarget.position += diff.normalized * Mathf.Min(diff.magnitude, _handController.velocity);
            prevTargetPos = targetPos;
            
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator DoFollowFootwork(int step)
    {
        _inCor = true;
        footworkDisplay.material = disabledColor;

        var stateName = StateNameFromStepValue(step);
        var footworkType = FootworkTypeFromStepValue(step);

        var transitionName = $"{stateNamePrefix}{entry} -> {stateNamePrefix}{stateName}";
        var exitTransitionName = $"{stateNamePrefix}{entry} -> {exit}";
        while (!_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            if (_animator.GetAnimatorTransitionInfo(0).IsName(exitTransitionName) 
                || _animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde")  // idk, maybe the state machine transition too fast?
                )
            {
                _inCor = false;
                yield break;
            }
            
            if (debug) Debug.Log($"[{logIdentifier}] waiting to enter transition {transitionName}");
            yield return new WaitForFixedUpdate();
        }

        if (footworkType == FootworkType.Lunge)
        {
            yield return StartCoroutine(FollowKeyFrames(footworkType));
            stateName = "Lunge Recover";    // exit transition should named lunge recover -> exit
        }
        else
        {
            if (footworkType != FootworkType.SmallStepForward && footworkType != FootworkType.SmallStepBackward)
            {
                yield return StartCoroutine(FollowKeyFrames(footworkType));
            }
            // wait for small step forward / backward to end
            transitionName = $"{stateNamePrefix}{stateName} -> {exit}";
            while (!_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
            {
                if (_animator.GetAnimatorTransitionInfo(0).IsName(exitTransitionName) 
                    || _animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde")  // idk, maybe the state machine transition too fast?
                   )
                {
                    _inCor = false;
                    yield break;
                }
                if (debug) Debug.Log($"[{logIdentifier}] waiting to enter transition {transitionName}");
                yield return new WaitForFixedUpdate();
            }
        }

        transitionName = $"{stateNamePrefix}{stateName} -> {exit}";
        while (_animator.GetAnimatorTransitionInfo(0).IsName(transitionName))
        {
            if (_animator.GetAnimatorTransitionInfo(0).IsName(exitTransitionName) 
                || _animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde")  // idk, maybe the state machine transition too fast?
               )
            {
                _inCor = false;
                yield break;
            }
            
            if (debug) Debug.Log($"[{logIdentifier}] waiting to end transition {transitionName}. step is {_animator.GetInteger(_animatorHashStep)}");
            yield return new WaitForFixedUpdate();
        }

        _inCor = false;
        footworkDisplay.material = enabledColor;
    }

    public string logIdentifier;
    
    private void FixedUpdate()
    {
        if (_inCor) return;
        
        var step = _animator.GetInteger(_animatorHashStep);
        if (step != 0)
        {
            StartCoroutine(DoFollowFootwork(step));
        }
        
        footworkDisplay.enabled = debug;
        
        // // use this to re-save the scriptableObjects
        // if (Input.GetKeyUp(KeyCode.X))
        // {
        //     StartCoroutine(SaveAllKeyFrames());
        // }
        //
        // if (Input.GetKeyUp(KeyCode.Y))
        // {
        //     StartCoroutine(SaveLungeKeyFrames());
        // }
    }

    public bool ReadyForNewFootwork()
    {
        return !_inCor;
    }
    
    public void ResetCoroutines()
    {
        StopAllCoroutines();
        _inCor = false;
        _animator.SetInteger(_animatorHashStep, 0);
    }

}
