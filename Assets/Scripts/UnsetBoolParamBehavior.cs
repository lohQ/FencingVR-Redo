using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class UnsetBoolParamBehavior : StateMachineBehaviour
{
    public string parameterName;
    private int _parameterHash;

    void Awake()
    {
        _parameterHash = Animator.StringToHash(parameterName);
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(_parameterHash, false);
    }

}