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
    
    // debug
    public bool debug;
    public MeshRenderer footworkDisplay;
    public Material enabledColor;
    public Material disabledColor;
    
    private Transform _moveTargetRoot;
    private Transform _epee;
    private Animator _animator;
    private Rig _ikRig;
    private FinalHandController _handController;
    
    private Dictionary<FootworkType, MoveTargetRootKeyFrames> _footworkKeyFrameDict;
    private int _animatorHashStep;
    private bool _inCor;
    private bool _collided;

    private void Awake()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _animator = GetComponent<Animator>();
        _handController = GetComponent<FinalHandController>();
        _moveTargetRoot = moveTarget.parent;
        _epee = wristOnEpee.parent;
        _animatorHashStep = Animator.StringToHash("step");
        _collided = false;

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
    
    private IEnumerator SaveKeyFrames(int step, string recoverStateName = "")
    {
        // TODO: make this independent of root rotation
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
            keyFrameSO.translationData.Add(_moveTargetRoot.position - startMoveTargetRootPos);
            keyFrameSO.rotationData.Add(Quaternion.Inverse(startMoveTargetRootRot) * _moveTargetRoot.rotation);
            yield return new WaitForFixedUpdate();
            curTransition = _animator.GetAnimatorTransitionInfo(0);
            curState = _animator.GetCurrentAnimatorStateInfo(0);
        }

        _animator.SetInteger(_animatorHashStep, 0);
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return new WaitForFixedUpdate();
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
            if (debug) Debug.DrawRay(epeeTarget.position, diff, Color.red, 5f);

            epeeTarget.position += diff.normalized * Mathf.Min(diff.magnitude, _handController.velocity);
            prevTargetPos = targetPos;
            
            if (debug) Debug.Log($"[{logIdenfitier}] following captured keyFrames, at {i} of {translationData.Count}");
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
        if (debug) Debug.DrawLine(epeeTarget.position, newPosition, Color.red, 5f);

        var newRotation = wrist.rotation * Quaternion.Inverse(wristOnEpee.localRotation);
        epeeTarget.position = newPosition;
        epeeTarget.rotation = newRotation;
    }
    
    // obsolete
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
            
            if (debug) Debug.Log($"[{logIdenfitier}] waiting to enter transition {transitionName}");
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
                if (debug) Debug.Log($"[{logIdenfitier}] waiting to enter transition {transitionName}");
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
            
            if (debug) Debug.Log($"[{logIdenfitier}] waiting to end transition {transitionName}. step is {_animator.GetInteger(_animatorHashStep)}");
            yield return new WaitForFixedUpdate();
        }

        _inCor = false;
        footworkDisplay.material = enabledColor;
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

    public string logIdenfitier;
    
    private void FixedUpdate()
    {
        if (_inCor) return;
        
        var step = _animator.GetInteger(_animatorHashStep);
        if (step != 0)
        {
            StartCoroutine(DoFollowFootwork(step));
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log($"[{logIdenfitier}] FollowFootwork is in coroutine: {_inCor}");
            if (Input.GetKey(KeyCode.LeftShift))
            {
                Debug.Log($"[{logIdenfitier}] Reset coroutines");
                ResetCoroutines();
            }
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

    public void RegisterCollision()
    {
        _collided = true;
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
        _ikRig.weight = 1;
    }

}
