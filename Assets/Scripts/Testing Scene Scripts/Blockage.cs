using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blockage : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public int intervalNum;
    public Transform center;

    private int _intervalIndex;
    private Vector3 _startPointPos;
    private Vector3 _endPointPos;

    private void Start()
    {
        _startPointPos = startPoint.position;
        _endPointPos = endPoint.position;
    }
    
    public IEnumerator<Vector3> GetNextBlockagePos()
    {
        for (_intervalIndex = 0; _intervalIndex < intervalNum; _intervalIndex++)
        {
            yield return _startPointPos + (_endPointPos - _startPointPos) * _intervalIndex / intervalNum;
        }
    }

    public Vector3 Vector()
    {
        return endPoint.position - startPoint.position;
    }

    public Vector3 GetCurBlockage()
    {
        return startPoint.position + (endPoint.position - startPoint.position) * _intervalIndex / intervalNum;
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     for (int i = 0; i < collision.contactCount; i++)
    //     {
    //         var contact = collision.GetContact(i);
    //         Debug.DrawRay(contact.point, contact.normal, Color.red);
    //     }
    // }
    //
    // private void OnCollisionStay(Collision collision)
    // {
    //     for (int i = 0; i < collision.contactCount; i++)
    //     {
    //         var contact = collision.GetContact(i);
    //         Debug.DrawRay(contact.point, contact.normal, Color.red);
    //     }
    // }
    //
    // private void OnCollisionExit(Collision collision)
    // {
    //     for (int i = 0; i < collision.contactCount; i++)
    //     {
    //         var contact = collision.GetContact(i);
    //         Debug.DrawRay(contact.point, contact.normal, Color.red);
    //     }
    // }
}
