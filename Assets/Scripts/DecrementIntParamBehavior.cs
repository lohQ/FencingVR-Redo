using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecrementIntParamBehavior : StateMachineBehaviour
{
    public string parameterName;
    public int decrement;
    private int _parameterHash;
    
    void Awake()
    {
        _parameterHash = Animator.StringToHash(parameterName);
    }
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(_parameterHash, animator.GetInteger(_parameterHash) - decrement);
    }

}
