using UnityEngine;

public class UnsetIntParamBehaviour : StateMachineBehaviour
{
    public string parameterName;
    private int _parameterHash;

    void Awake()
    {
        _parameterHash = Animator.StringToHash(parameterName);
    }
    
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(_parameterHash, 0);
    }

}