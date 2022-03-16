using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AvatarController : MonoBehaviour
{
    // agent action:
    // set step number to determine whether step forward/backward, small/normal/big, lunge
    
    private HandEffectorController _handEffectorController;
    public float[] stepDistances = {0.4f, 0.6f, 0.8f};
    private float[] _scaledStepDistances;
    
    private Animator _animator;
    private int _stepHash;
    private int _endedHash;

    private int _envLayerMask;
    private bool _started = false;

    void Start()
    {
        _handEffectorController = GetComponent<HandEffectorController>();
        _animator = GetComponent<Animator>();

        _stepHash = Animator.StringToHash("step");
        _endedHash = Animator.StringToHash("end");
        _envLayerMask = LayerMask.GetMask(new[] {PhysicsEnvSettings.EnvironmentLayer});

        _scaledStepDistances = new float[stepDistances.Length];
        for (int i = 0; i < stepDistances.Length; i++)
        {
            _scaledStepDistances[i] = stepDistances[i] * PhysicsEnvSettings.ScaleFactor;
        }
        _started = true;
    }

    public IEnumerator EnterEnGarde()
    {
        while (!_started)
        {
            yield return null;
        }
        
        _animator.SetBool(_endedHash, false);
        _animator.SetInteger(_stepHash, 0);
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("En Garde"))
        {
            yield return null;
        }
        _handEffectorController.Initialize();
    }

    public IEnumerator ExitEnGarde()
    {
        _handEffectorController.DisableIK();

        _animator.SetBool(_endedHash, true);
        yield return null;
        
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Standing"))
        {
            yield return null;
        }
    }

    // step range is [-3, 4]
    public void SetNextStep(int step)
    {
        if (step == 0) return;
        
        // prevent from stepping out of piste & set animation parameter
        var index = step;
        var direction = transform.forward;
        if (step < 0)
        {
            index = -index;
            direction = -direction;
        }

        if (step != 4)
        {
            var origin = transform.position + Vector3.up;
            var hit = Physics.Raycast(new Ray(origin, direction), _scaledStepDistances[index-1], _envLayerMask);
            if (hit) return;
        }
        _animator.SetInteger(_stepHash, step);
    }

}
