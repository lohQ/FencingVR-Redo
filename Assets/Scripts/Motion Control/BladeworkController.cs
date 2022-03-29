using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeworkController : MonoBehaviour
{
    [HideInInspector]
    public List<Transform> worldPointToTargets;     // Set by GameController
    public bool debug;
    private FinalHandController _handController;
    private bool _inRotCor;
    private bool _inMoveCor;

    private void Start()
    {
        _handController = GetComponent<FinalHandController>();
        _inMoveCor = false;
        _inRotCor = false;
    }
    
    private IEnumerator RotateWrist(bool clockwise)
    {
        if (debug) Debug.Log($"Start rotate wrist {new String(clockwise ? "" : "anti")}clockwise");
        _inRotCor = true;

        var pointTarget = _handController.externalPointToTarget;
        var refPointTargets = _handController.pointToTargets;
        var moveVelocity = _handController.velocity * 3;    // this is wrist velocity but just use first la

        var paths = clockwise ? new []{0, 5, 4, 3, 2, 1, 0} : new []{0, 1, 2, 3, 4, 5, 0};
        for (int i = 0; i < paths.Length - 1; i++)
        {
            var start = refPointTargets[paths[i]];
            var end = refPointTargets[paths[i+1]];
            var startPos = start.position;
            var endPos = end.position;
            pointTarget.position = startPos;

            var timeElapsed = 0f;
            var duration = (endPos - startPos).magnitude / moveVelocity;
            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;
                pointTarget.position = Vector3.Lerp(startPos, endPos, timeElapsed / duration);
                yield return new WaitForFixedUpdate();
            }
        }

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
        while (!_handController.ReachedMoveTarget() || !_handController.ReachedRotationTarget(rotationError))
        {
            yield return new WaitForFixedUpdate();
        }
        // yield return new WaitWhile(
        //     () => !_handController.ReachedMoveTarget() || !_handController.ReachedRotationTarget(rotationError));

        _inRotCor = false;
        _inMoveCor = false;
        if (debug) Debug.Log($"End parry {parryNum}");
    }

    
    
    // ----- below are the exposed functions ----- //
    
    public void DoWristTranslation(int forward, int rightward, int upward, bool extended, int hintX)
    {
        if (_inMoveCor) return;
        _handController.SetMoveToTargetPosition(forward, rightward, upward, extended);
        _handController.SetHintPosition(hintX);
    }

    public void DoWristRotation(int supIndex, int pointToIndex)
    {
        if (_inRotCor) return;

        _handController.SetNextSuppination(supIndex);

        if (pointToIndex > 0)
        {
            // got 5
            _handController.externalPointToTarget.position = worldPointToTargets[pointToIndex - 1].position;
        }
        else
        {
            // got 5
            // 0 * 2 = 0
            // 1 * 2 = 2
            // 2 * 2 = 4
            // 3 * 2 = 6
            // 4 * 2 = 8
            _handController.externalPointToTarget.position = _handController.pointToTargets[(-pointToIndex) * 2].position;

            // if (pointToIndex == 0)
            // {
            //     // idle (point to center)
            //     _handController.externalPointToTarget.position = _handController.pointToTargets[0].position;
            // }
            // else if (pointToIndex == -1)
            // {
            //     StartCoroutine(RotateWrist(true));
            //     _inRotCor = true;
            // }
            // else if (pointToIndex == -2)
            // {
            //     StartCoroutine(RotateWrist(false));
            //     _inRotCor = true;
            // }
        }
    }

    public bool CanTranslateWrist()
    {
        return !_inMoveCor;
    }

    public bool CanRotateWrist()
    {
        return !_inRotCor;
    }

    public void ResetCoroutines()
    {
        StopAllCoroutines();

        _inMoveCor = false;
        _inRotCor = false;
        _handController.ResetCoroutines();
    }

    // ----- above are the exposed functions ----- //
}
