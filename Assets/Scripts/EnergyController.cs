using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyController : MonoBehaviour
{
    // range: [0,1]
    public float value;

    [Tooltip("How much energy to restore per second")]
    public float restorationRate = 0.2f;
    [Tooltip("How much energy to use for one second of translation")]
    public float translationUsageRate;
    [Tooltip("How much energy to use for one second of rotation")]
    public float rotationUsageRate;

    public Image greenCircle;
    public float startFilling = 0.075f;
    public float endFilling = 0.935f;
    
    public RectTransform displayTransform;
    public Transform displayPos;

    private float _usage;

    public float ForceMultiplier()
    {
        if (value > 0.6f)
        {
            return 1;
        }
        return value / 0.6f;
    }
    
    public void DoMove(float forceRatio)
    {
        // this force ratio ranges between [0,1]
        _usage += translationUsageRate * forceRatio * Time.deltaTime;
    }

    public void DoRotate(float forceRatio)
    {
        // this force ratio is degreeDelta * [0,1], so it can be as big as the degreeDelta
        _usage += rotationUsageRate * forceRatio * Time.deltaTime;
    }
    
    private void FixedUpdate()
    {
        value -= _usage;
        value += restorationRate * Time.deltaTime;
        if (value > 1) value = 1;
        if (value < 0) value = 0;
        _usage = 0f;
    }

    private void Update()
    {
        greenCircle.fillAmount = startFilling + value * (endFilling - startFilling);
        displayTransform.position = displayPos.position;
        displayTransform.LookAt(displayPos.position + displayPos.forward);
        Debug.DrawRay(displayPos.position, displayPos.forward, Color.blue);
        Debug.DrawRay(displayPos.position, displayPos.right, Color.red);
        Debug.DrawRay(displayPos.position, displayPos.up, Color.green);
    }
    
}
