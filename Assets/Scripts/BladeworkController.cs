using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeworkController : MonoBehaviour
{
    public bool debug;
    private FinalHandController _handController;
    private bool _inRotCor;
    private bool _inMoveCor;

    // change RotateToTarget interface to not take input. Use the SetRotationTarget function instead. 
    // add more PointTo targets
    // seems like some part of the excessive oscilation is because of the energy level thing...?
    // TODO: debug why MoveToTarget doesn't move in one shot but requires two run?
    // the target position didn't change but the initial aim is not accurate?
    // i guess it is because the epee-to-wrist vector changes when the hand translate
    // tested most bladeworks and looks ok
    // TODO: add in footwork animation
    //modify the wrist rotation radius
    // hopefully these could be done by noon?
    
    // add in collider
    // TODO: test hit
    // TODO: reuse/create game env
    // TODO: create agent fencer
    
    // TODO: randomize env variables and create curriculum
    
    private void Start()
    {
        _handController = GetComponent<FinalHandController>();
        _handController.SetMoveToTargetPosition(1, 0, 0, false);
        _handController.SetNextSuppination(0);
    }
    
    private IEnumerator BackToIdle()
    {
        if (debug) Debug.Log("Start back to idle");
        _inRotCor = true;
        _inMoveCor = true;

        _handController.SetMoveToTargetPosition(1, 0, 0, false);
        _handController.SetNextSuppination(0);

        var rotationError = _handController.maxRotationError * 2;
        yield return new WaitWhile(
            () => !_handController.ReachedMoveTarget() || !_handController.ReachedRotationTarget(rotationError));

        _inRotCor = false;
        _inMoveCor = false;
        if (debug) Debug.Log("End back to idle");
    }
    
    private IEnumerator ExtendArm()
    {
        if (debug) Debug.Log($"Start extend arm");
        _inMoveCor = true;

        _handController.SetMoveToTargetPosition(1, 0, 1, true);
        yield return new WaitWhile(() => !_handController.ReachedMoveTarget());

        _inMoveCor = false;
        if (debug) Debug.Log($"End extend arm");
    }
    
    private IEnumerator RotateWrist(bool clockwise)
    {
        if (debug) Debug.Log($"Start rotate wrist {new String(clockwise ? "" : "anti")}clockwise");
        _inRotCor = true;

        var initialPointToWorld = _handController.rotateToWorldPoint;
        _handController.rotateToWorldPoint = true;
        
        var pointTarget = _handController.externalPointToTarget;
        var refPointTargets = _handController.pointToTargets;
        var moveVelocity = _handController.velocity * 5;    // this is wrist velocity but just use first la
        
        if (clockwise)
        {
            var paths = new []{0, 10, 11, 12, 0};
            for (int i = 0; i < paths.Length - 1; i++)
            {
                var start = refPointTargets[i];
                var end = refPointTargets[i + 1];
                var startPos = start.position;
                var endPos = end.position;
                pointTarget.position = start.position;

                var timeElapsed = 0f;
                var duration = (endPos - startPos).magnitude / moveVelocity;
                while (timeElapsed < duration)
                {
                    timeElapsed += Time.deltaTime;
                    pointTarget.position = Vector3.Lerp(startPos, endPos, timeElapsed / duration);
                    _handController.SetWorldPointTo();
                    yield return null;
                }
            }
        }
        else
        {
            var paths = new []{0, 12, 11, 10, 0};
            for (int i = 0; i < paths.Length - 1; i++)
            {
                var start = refPointTargets[i];
                var end = refPointTargets[i + 1];
                var startPos = start.position;
                var endPos = end.position;
                pointTarget.position = start.position;

                var timeElapsed = 0f;
                var duration = (endPos - startPos).magnitude / moveVelocity;
                while (timeElapsed < duration)
                {
                    timeElapsed += Time.deltaTime;
                    pointTarget.position = Vector3.Lerp(startPos, endPos, timeElapsed / duration);
                    yield return null;
                }
            }
        }

        _handController.rotateToWorldPoint = initialPointToWorld;
        _inRotCor = false;
        if (debug) Debug.Log($"End rotate wrist {new String(clockwise ? "" : "anti")}clockwise");
    }

    private IEnumerator Parry(int parryNum)
    {
        if (debug) Debug.Log($"Start parry {parryNum}");
        _inRotCor = true;
        _inMoveCor = true;

        switch (parryNum)
        {
            case 4:
                _handController.SetMoveToTargetPosition(1, -1, 0, false);
                _handController.SetNextSuppination(1);
                break;
            case 6:
                _handController.SetMoveToTargetPosition(1, 1, 0, false);
                _handController.SetNextSuppination(-1);
                break;
            case 7:
                _handController.SetMoveToTargetPosition(1, -1, -1, true);
                _handController.SetNextSuppination(-1);
                break;
            case 8:
                _handController.SetMoveToTargetPosition(1, 1, -1, true);
                _handController.SetNextSuppination(1);
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
        if (debug) Debug.Log($"End parry {parryNum}");
    }
    
    // used for now only
    public int forward;
    public int rightward;
    public int upward;
    public bool extended;
    public bool clockwise;

    public int supIndex;
    public int parry;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.S))
        {
            _handController.SetMoveToTargetPosition(forward, rightward, upward, extended);
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            _handController.SetNextSuppination(supIndex);
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
            else if (debug) 
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
            else if (debug)
            {
                Debug.Log($"GetKeyUp(KeyCode.Return), do nothing as _inMoveCor is {_inMoveCor} and _inRotCor is {_inRotCor}");
            }
        }

    }
}
