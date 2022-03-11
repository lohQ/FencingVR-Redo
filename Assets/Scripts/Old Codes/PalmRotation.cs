using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PalmRotation : MonoBehaviour
{
    public List<float> zPoints = new List<float>(){-80, -40, 0, 40, 80};
    private Vector3 _neutralVector;
    private Quaternion _neutralRotation;
    private Transform _parent;
    
    void Start()
    {
        _neutralVector = transform.right;
        _neutralRotation = transform.rotation;
    }

    public void SetNeutralXVector()
    {
        _neutralVector = transform.right;
        _neutralRotation = transform.rotation;
    }

    public bool useRotatedNeutralVector;
    
    public Quaternion GetPalmRotationToApply(int zPointIndex, bool debug)
    {
        // coz using Vector3.forward, z rotation not included
        // var fromNeutralToCurRotation = Quaternion.FromToRotation(
        //     _neutralRotation * Vector3.forward, transform.forward);
        // var rotatedNeutralVector = fromNeutralToCurRotation * _neutralVector;
        // Debug.DrawRay(transform.position, rotatedNeutralVector * 10, Color.yellow);
        
        // rotate the initial x axis vector along the z axis, and get the resulting x axis vector
        float curRotFromNeutral;
        // if (useRotatedNeutralVector)
        // {
        //     curRotFromNeutral = Vector3.SignedAngle(rotatedNeutralVector, transform.right, transform.forward);
        // }
        // else
        // {
            curRotFromNeutral = Vector3.SignedAngle(_neutralVector, transform.right, transform.forward);
        // }
        if (debug && curRotFromNeutral > 0) Debug.Log($"curRotFromNeutral: {curRotFromNeutral}; angle z to rotate: {zPoints[zPointIndex] - curRotFromNeutral}");

        // var targetVector = Quaternion.AngleAxis(zPoints[zPointIndex], transform.forward) * new Vector3(transform.position.x, 0, _neutralVector.z);
        // if (debug) Debug.DrawRay(transform.position, targetVector.normalized * 10, Color.green);
        // return Quaternion.FromToRotation(transform.right, targetVector);

        return Quaternion.AngleAxis(zPoints[zPointIndex] - curRotFromNeutral, transform.forward);
    }

    void Update()
    {
        Debug.DrawRay(transform.position, _neutralVector * 10, Color.magenta);
        Debug.DrawRay(transform.position, transform.forward * 10, Color.blue);
        Debug.DrawRay(transform.position, transform.right * 10, Color.red);

    }
    
}
