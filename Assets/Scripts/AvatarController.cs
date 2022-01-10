using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AvatarController : MonoBehaviour
{
    private Rig _ikRig;
    private IkTargetController _ikTargetController;

    private Animator _animator;
    private int _forwardStepHash;
    private int _backwardStepHash;
    private int _endedHash;
    private bool _inEnGarde;

    private bool _started = false;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _forwardStepHash = Animator.StringToHash("forward_step");
        _backwardStepHash = Animator.StringToHash("backward_step");
        _endedHash = Animator.StringToHash("end");

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
        _inEnGarde = true;
        _ikRig.weight = 0;
        _animator.SetBool(_endedHash, false);
        _animator.SetInteger(_forwardStepHash, 0);
        _animator.SetInteger(_backwardStepHash, 0);
        yield return null;
        
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return null;
        }

        // var coroutineEndTime = Time.time;
        // Debug.Log("for " + name + ", coroutine took time of " + $"{(coroutineEndTime - coroutineStartTime):0.00}");
        
        _ikTargetController.Initialize();
        _ikRig.weight = 1;
    }

    public IEnumerator ExitEnGarde()
    {
        _inEnGarde = false;
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
        if ((forward - 1) > 0)
        {
            _animator.SetInteger(_forwardStepHash, (forward - 1));
        } else if ((forward - 1) < 0)
        {
            _animator.SetInteger(_backwardStepHash, -(forward - 1));
        }
    }

    public bool IsMoving()
    {
        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        if (curState.IsName("Step Forward") || curState.IsName("Step Backward"))
        {
            return true;
        }
        return false;
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

    }
}
