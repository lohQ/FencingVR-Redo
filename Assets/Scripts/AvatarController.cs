using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AvatarController : MonoBehaviour
{
    public float stepDistance;
    public Vector3 fencerForward;
    
    private Rig _ikRig;
    private IkTargetController _ikTargetController;

    private Animator _animator;
    private int _forwardStepHash;
    private int _backwardStepHash;
    private int _endedHash;
    private int _lungeHash;
    // private bool _inEnGarde;

    private bool _started = false;
    private int _envLayerMask;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _forwardStepHash = Animator.StringToHash("forward_step");
        _backwardStepHash = Animator.StringToHash("backward_step");
        _endedHash = Animator.StringToHash("end");
        _lungeHash = Animator.StringToHash("lunge");
        _envLayerMask = LayerMask.GetMask(new[] {"Environment"});
        curStateInt = -1;

        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _ikTargetController = GetComponent<IkTargetController>();
        _started = true;
    }

    public IEnumerator EnterEnGarde()
    {
        // var coroutineStartTime = Time.time;
        while (!_started)
        {
            yield return null;
        }
        // _inEnGarde = true;
        _ikRig.weight = 0;
        _animator.SetBool(_endedHash, false);
        _animator.SetInteger(_forwardStepHash, 0);
        _animator.SetInteger(_backwardStepHash, 0);
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return null;
        }
        // yield return null;
        
        // var coroutineEndTime = Time.time;
        // Debug.Log("for " + name + ", coroutine took time of " + $"{(coroutineEndTime - coroutineStartTime):0.00}");
        
        _ikTargetController.Initialize();
        _ikRig.weight = 1;
    }

    public IEnumerator ExitEnGarde()
    {
        // _inEnGarde = false;
        _animator.SetBool(_endedHash, true);
        yield return null;
        
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Standing"))
        {
            yield return null;
        }
        
        _ikRig.weight = 0;
    }

    public void SetNextStep(int forward)
    {
        if (forward > 0)
        {
            var hit = Physics.Raycast(
                new Ray(transform.position + Vector3.up, fencerForward), stepDistance, _envLayerMask);
            if (!hit)
            {
                _animator.SetInteger(_forwardStepHash, forward);
            }
        } else if (forward < 0)
        {
            var hit = Physics.Raycast(
                new Ray(transform.position + Vector3.up, -fencerForward), stepDistance, _envLayerMask);
            if (!hit)
            {
                _animator.SetInteger(_backwardStepHash, -forward);
            }
        }
    }

    public void Lunge()
    {
        _animator.SetBool(_lungeHash, true);
    }

    [HideInInspector]
    public int curStateInt;    // 8 for step forward/backward, 9 for lunge (same as branch number)

    public void SetCurStateInt()
    {
        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        if (curState.IsName("Step Forward") || curState.IsName("Step Backward"))
        {
            curStateInt = 8;
        }
        else if (curState.IsName("Lunge and Recover"))
        {
            curStateInt = 9;
        }
        else
        {
            curStateInt = -1;
        }
    }
    
    void Update()
    {
        # region keyboard input
        // if (Input.GetKeyUp(KeyCode.UpArrow))
        // {
        //     if (Input.GetKey(KeyCode.LeftShift))
        //     {
        //         _animator.SetInteger(_forwardStepHash, 3);
        //     }
        //     else
        //     {
        //         _animator.SetInteger(_forwardStepHash, 1);
        //     }
        // } else if (Input.GetKeyUp(KeyCode.DownArrow))
        // {
        //     if (Input.GetKey(KeyCode.LeftShift))
        //     {
        //         _animator.SetInteger(_backwardStepHash, 3);
        //     }
        //     else
        //     {
        //         _animator.SetInteger(_backwardStepHash, 1);
        //     }
        // }
        # endregion

        // if (Input.GetKeyUp(KeyCode.Return))
        // {
        //     var isEnded = _animator.GetBool(_endedHash);
        //     if (!isEnded && _inEnGarde)
        //     {
        //         StartCoroutine(ExitEnGarde());
        //     }
        //     else if (isEnded && !_inEnGarde)
        //     {
        //         StartCoroutine(EnterEnGarde());
        //     }
        // }

        // var prevState = curStateInt;
        SetCurStateInt();
        // if (prevState != 9 && curStateInt == 9)
        // {
        //     
        //     Debug.Log("set ikRig.weight to " + rigWeightWhenLunge);
        //     _ikRig.weight = rigWeightWhenLunge;
        // } else if (prevState == 9 && curStateInt != 9)
        // {
        //     Debug.Log("set ikRig.weight back to 1");
        //     _ikRig.weight = 1;
        // }

    }
}
