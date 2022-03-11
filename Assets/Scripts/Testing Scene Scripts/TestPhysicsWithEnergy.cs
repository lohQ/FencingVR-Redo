using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPhysicsWithEnergy : MonoBehaviour
{
    public int xIntervalCount;
    public int yIntervalCount;
    public int curXIntervalIndex;
    public int curYIntervalIndex;
    public float xyOffset;

    public Transform verticalEpeeTarget;
    public Transform verticalEpee;
    public Transform verticalEpeeRoot;
    public Transform verticalEpeeTip;
    public EnergyController verticalEnergyController;
    
    public Transform horizontalEpeeTarget;
    public Transform horizontalEpee;
    public Transform horizontalEpeeRoot;
    public Transform horizontalEpeeTip;
    public EnergyController horizontalEnergyController;

    public float overshootDistance;
    public float duration;

    private float _vEpeeXStartVal;
    private float _vEpeeXEndVal;
    private float _xInterval;
    private float _hEpeeYStartVal;
    private float _hEpeeYEndVal;
    private float _yInterval;
    private float _initialVerticalZ;
    private float _initialHorizontalZ;

    private bool _started = false;

    void Start()
    {
        verticalEpeeTarget.rotation = Quaternion.Euler(90, 0, 0);
        verticalEpee.rotation = verticalEpeeTarget.rotation;
        horizontalEpeeTarget.rotation = Quaternion.Euler(0, -90, 90);
        horizontalEpee.rotation = horizontalEpeeTarget.rotation;
        _vEpeeXStartVal = horizontalEpeeRoot.position.x + xyOffset;
        _vEpeeXEndVal = horizontalEpeeTip.position.x - xyOffset;
        _hEpeeYStartVal = verticalEpeeRoot.position.y + xyOffset;
        _hEpeeYEndVal = verticalEpeeTip.position.y - xyOffset;
        _xInterval = (_vEpeeXEndVal - _vEpeeXStartVal) / xIntervalCount;
        _yInterval = (_hEpeeYEndVal - _hEpeeYStartVal) / yIntervalCount;
        _initialVerticalZ = verticalEpee.position.z;
        _initialHorizontalZ = horizontalEpee.position.z;
        Debug.Log($"_xInterval: {_xInterval}, _yInterval: {_yInterval}");
        _started = true;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            curYIntervalIndex += 1;
            if (curYIntervalIndex > yIntervalCount)
            {
                curYIntervalIndex = 0;
                curXIntervalIndex += 1;
                if (curXIntervalIndex > xIntervalCount)
                {
                    curXIntervalIndex = 0;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            StartCoroutine(Test());
        }
    }
    
    IEnumerator Test()
    {
        var hPos = horizontalEpeeTarget.position;
        var vPos = verticalEpeeTarget.position;
        verticalEpeeTarget.position = new Vector3(
            _vEpeeXStartVal +_xInterval * curXIntervalIndex, 
            vPos.y, 
            _initialVerticalZ);
        verticalEpee.position = verticalEpeeTarget.position;
        horizontalEpeeTarget.position = new Vector3(
            hPos.x, 
            _hEpeeYStartVal + _yInterval * curYIntervalIndex, 
            _initialHorizontalZ);
        horizontalEpee.position = horizontalEpeeTarget.position;

        while (verticalEnergyController.value != 1 || horizontalEnergyController.value != 1)
        {
            yield return null;
        }

        var middleZ = (_initialHorizontalZ + _initialVerticalZ) / 2;
        var vTargetZ = middleZ + (middleZ > _initialVerticalZ ? overshootDistance : -overshootDistance);
        var hTargetZ = middleZ + (middleZ > _initialHorizontalZ ? overshootDistance : -overshootDistance);

        var timeElapsed = 0f;
        var vStartPos = verticalEpeeTarget.position;
        var hStartPos = horizontalEpeeTarget.position;
        var vEndPos = new Vector3(vStartPos.x, vStartPos.y, vTargetZ);
        var hEndPos = new Vector3(hStartPos.x, hStartPos.y, hTargetZ);
        Debug.Log($"vPos.z: {vPos.z}, hPos.z: {hPos.z}, middleZ: {middleZ}");
        Debug.Log($"vStartPos: {vStartPos}, vEndPos: {vEndPos}");
        Debug.Log($"hStartPos: {hStartPos}, hEndPos: {hEndPos}");
        Debug.DrawLine(vStartPos, vEndPos, Color.blue, duration * 2);
        Debug.DrawLine(hStartPos, hEndPos, Color.red, duration * 2);
        while (timeElapsed <= duration * 2)
        {
            var progress = timeElapsed / duration;
            verticalEpeeTarget.position =
                Vector3.Lerp(vStartPos, vEndPos, progress);
            horizontalEpeeTarget.position =
                Vector3.Lerp(hStartPos, hEndPos, progress);
            yield return null;
            timeElapsed += Time.deltaTime;
        }

        // check
        if (curXIntervalIndex < curYIntervalIndex)
        {
            // vertical should use more energy than horizontal
            if (verticalEnergyController.value < horizontalEnergyController.value)
            {
                Debug.Log("Yep vertical epee used up more energy!");
            }
            else
            {
                Debug.Log("Something's wrong! Vertical epee didn't use more energy than horizontal one.");
            }
        } 
        else if (curYIntervalIndex < curXIntervalIndex)
        {
            if (horizontalEnergyController.value < verticalEnergyController.value)
            {
                Debug.Log("Yep horizontal epee used up more energy!");
            }
            else
            {
                Debug.Log("Something's wrong! Horizontal epee didn't use more energy than vertical one.");
            }
        }
        else
        {
            if (horizontalEnergyController.value - verticalEnergyController.value > 0.1f)
            {
                Debug.Log("Something's wrong! There's > 0.1f energy level difference between horizontal and vertical epee.");
            }
            else
            {
                Debug.Log("Yep horizontal epee and vertical epee used up similar amount of energy!");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_started) return;

        var vPos = verticalEpeeRoot.position;
        for (int i = 0; i < xIntervalCount; i++)
        {
            var point = new Vector3(
                _vEpeeXStartVal + _xInterval * i,
                vPos.y,
                _initialVerticalZ);
            Gizmos.DrawWireSphere(point, 1);
        }
        var hPos = horizontalEpeeRoot.position;
        for (int i = 0; i < xIntervalCount; i++)
        {
            var point = new Vector3(
                hPos.x, 
                _hEpeeYStartVal + _yInterval * i, 
                _initialHorizontalZ);
            Gizmos.DrawWireSphere(point, 1);
        }
    }
}
