using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeworkController : MonoBehaviour
{
    public int maxIterWristRotate;
    public float maxWristRotationError;

    private FinalHandController _handController;
    private bool _inRotCor;
    private bool _inMoveCor;

    // change RotateToTarget interface to not take input. Use the SetRotationTarget function instead. 
    // add more PointTo targets
    // seems like some part of the excessive oscilation is because of the energy level thing...?
    // TODO: debug why MoveToTarget doesn't move in one shot but requires two run?
    // the target position didn't change but the initial aim is not accurate :(
    // i guess it is because the epee-to-wrist vector changes when the hand translate
    // TODO: test all the bladeworks
    // TODO: add in footwork animation
    // TODO: modify the wrist rotation radius
    // hopefully these could be done by noon?
    
    // TODO: add in collider
    // TODO: test hit
    // TODO: reuse/create game env
    // TODO: create agent fencer
    
    // TODO: randomize env variables and create curriculum
    
    private void Start()
    {
        _handController = GetComponent<FinalHandController>();
        _handController.SetMoveToTargetPosition(1, 0, 0, false);
        _handController.SetNextRotation(0, 0);
        _inRotCor = false;
    }
    
    public IEnumerator BackToIdle()
    {
        Debug.Log("Start back to idle");
        _inRotCor = true;
        _inMoveCor = true;

        _handController.SetMoveToTargetPosition(1, 0, 0, false);
        _handController.SetNextRotation(0, 0);

        var rotationError = _handController.maxRotationError * 2;
        yield return new WaitWhile(
            () => !_handController.ReachedMoveTarget() || !_handController.ReachedRotationTarget(rotationError));

        _inRotCor = false;
        _inMoveCor = false;
        Debug.Log("End back to idle");
    }
    
    public IEnumerator ExtendArm()
    {
        Debug.Log($"Start extend arm");
        _inMoveCor = true;

        _handController.SetMoveToTargetPosition(1, 0, 0, true);
        yield return new WaitWhile(() => !_handController.ReachedMoveTarget());

        _inMoveCor = false;
        Debug.Log($"End extend arm");
    }
    
    private IEnumerator RotateWrist(bool clockwise)
    {
        // note: should only use when pointing to center. Else may have awkward movement revert to center before rotating
        
        Debug.Log($"Start rotate wrist {new String(clockwise ? "" : "anti")}clockwise");
        _inRotCor = true;
        _handController.rotatingByDefault = false;
        while (_handController.Rotating()) yield return null;

        var initialMaxIter = _handController.maxIteration;
        _handController.maxIteration = maxIterWristRotate;
        var initialRotError = _handController.maxRotationError;
        _handController.maxRotationError = maxWristRotationError;

        if (clockwise)
        {
            for (int i = 0; i < 9; i++)
            {
                _handController.SetNextRotation(0, i);
                yield return StartCoroutine(_handController.RotateToTarget());
            }
            _handController.SetNextRotation(0, 0);
            yield return StartCoroutine(_handController.RotateToTarget());
        }
        else
        {
            _handController.SetNextRotation(0, 0);
            yield return StartCoroutine(_handController.RotateToTarget());
            for (int i = 8; i >= 0; i--)
            {
                _handController.SetNextRotation(0, i);
                yield return StartCoroutine(_handController.RotateToTarget());
            }
        }

        _handController.maxIteration = initialMaxIter;
        _handController.maxRotationError = initialRotError;
        _handController.rotatingByDefault = true;
        _inRotCor = false;
        Debug.Log($"End rotate wrist {new String(clockwise ? "" : "anti")}clockwise");
    }

    public IEnumerator Parry(int parryNum)
    {
        Debug.Log($"Start parry {parryNum}");
        _inRotCor = true;
        _inMoveCor = true;

        switch (parryNum)
        {
            case 4:
                _handController.SetMoveToTargetPosition(1, -1, 0, true);
                _handController.SetNextRotation(1, 0);
                break;
            case 6:
                _handController.SetMoveToTargetPosition(1, 1, 0, true);
                _handController.SetNextRotation(-1, 0);
                break;
            case 7:
                _handController.SetMoveToTargetPosition(1, -1, -1, true);
                _handController.SetNextRotation(-1, 0);
                break;
            case 8:
                _handController.SetMoveToTargetPosition(1, 1, -1, true);
                _handController.SetNextRotation(1, 0);
                break;
            default:
                Debug.Log($"haven't implemented parry {parryNum} yet, do nothing.");
                break;
        }
        
        var rotationError = _handController.maxRotationError * 2;
        yield return new WaitWhile(
            () => !_handController.ReachedMoveTarget() || !_handController.ReachedRotationTarget(rotationError));

        _inRotCor = false;
        _inMoveCor = false;
        Debug.Log($"End parry {parryNum}");
    }
    
    // used for now only
    public int forward;
    public int rightward;
    public int upward;
    public bool extended;
    public bool clockwise;

    public int supIndex;
    public int pointToIndex;
    public int parry;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.S))
        {
            _handController.SetMoveToTargetPosition(forward, rightward, upward, extended);
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            _handController.SetNextRotation(supIndex, pointToIndex);
        }

        if (!_inRotCor && Input.GetKeyUp(KeyCode.Space))
        {
            StartCoroutine(RotateWrist(clockwise));
        }

        KeyCode[] keyCodes = {KeyCode.Alpha4, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8};
        foreach (var kc in keyCodes)
        {
            if (Input.GetKeyUp(kc))
            {
                parry = (int)kc - 48;
            }
        }

        if (Input.GetKeyUp(KeyCode.Backslash))
        {
            if (!_inMoveCor && !_inRotCor)
            {
                
                if (parry == 4 || parry == 6 || parry == 7 || parry == 8)
                {
                    StartCoroutine(Parry(parry));
                }
            }
            else
            {
                Debug.Log($"GetKeyUp(KeyCode.Backslash), do nothing as _inMoveCor is {_inMoveCor} and _inRotCor is {_inRotCor}");
            }
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!_inMoveCor && !_inRotCor)
            {
                StartCoroutine(Input.GetKey(KeyCode.LeftShift) ? BackToIdle() : ExtendArm());
            }
            else
            {
                Debug.Log($"GetKeyUp(KeyCode.Return), do nothing as _inMoveCor is {_inMoveCor} and _inRotCor is {_inRotCor}");
            }
        }

    }
}
