using System;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Normalizer : MonoBehaviour
{
    public Vector3 minWristFromFencer;
    public Vector3 maxWristFromFencer;

    public Vector3 minTargetFromTip;
    public Vector3 maxTargetFromTip;

    public Vector3 minSelfEpeeFromFencer;
    public Vector3 maxSelfEpeeFromFencer;
    public Vector3 minOppEpeeFromFencer;
    public Vector3 maxOppEpeeFromFencer;
    public Vector3 minEpeeTipFromEpee;
    public Vector3 maxEpeeTipFromEpee;

    public Vector3 minContactPoint;
    public Vector3 maxContactPoint;
    public Vector3 minImpulse;
    public Vector3 maxImpulse;

    public void SaveMinMax(Vector3 newVal, int index)
    {
        Vector3 min;
        Vector3 max;
        if (index == 0)
        {
            min = minWristFromFencer;
            max = maxWristFromFencer;
        }
        else if (index == 1)
        {
            min = minTargetFromTip;
            max = maxTargetFromTip;
        }
        else if (index == 2)
        {
            min = minSelfEpeeFromFencer;
            max = maxSelfEpeeFromFencer;
        }
        else if (index == 3)
        {
            min = minEpeeTipFromEpee;
            max = maxEpeeTipFromEpee;
        }
        else if (index == 4)
        {
            min = minOppEpeeFromFencer;
            max = maxOppEpeeFromFencer;
        }
        else if (index == 5)
        {
            min = minContactPoint;
            max = maxContactPoint;
        }
        else if (index == 6)
        {
            min = minImpulse;
            max = maxImpulse;
        }
        else
        {
            return;
        }

        if (newVal.x < min.x)
        {
            min.x = newVal.x;
        }
        else if (newVal.x > max.x)
        {
            max.x = newVal.x;
        }

        if (newVal.y < min.y)
        {
            min.y = newVal.y;
        }
        else if (newVal.y > max.y)
        {
            max.y = newVal.y;
        }

        if (newVal.z < min.z)
        {
            min.z = newVal.z;
        }
        else if (newVal.z > max.z)
        {
            max.z = newVal.z;
        }

        if (index == 0)
        {
            minWristFromFencer = min;
            maxWristFromFencer = max;
        }
        else if (index == 1)
        {
            minTargetFromTip = min;
            maxTargetFromTip = max;
        }
        else if (index == 2)
        {
            minSelfEpeeFromFencer = min;
            maxSelfEpeeFromFencer = max;
        }
        else if (index == 3)
        {
            minEpeeTipFromEpee = min;
            maxEpeeTipFromEpee = max;
        }
        else if (index == 4)
        {
            minOppEpeeFromFencer = min;
            maxOppEpeeFromFencer = max;
        }
        else if (index == 5)
        {
            minContactPoint = min;
            maxContactPoint = max;
        }
        else if (index == 6)
        {
            minImpulse = min;
            maxImpulse = max;
        }
    }

    private float GetNormalized(float newVal, float min, float max)
    {
        return (newVal - min) / (max - min);
    }
    
    public float GetNormalizedCapped(float newVal, float min, float max)
    {
        if (newVal < min)
        {
            return min;
        }

        if (newVal > max)
        {
            return max;
        }

        return GetNormalized(newVal, min, max);
    }
    
    public Vector3 GetNormalized(Vector3 newVal, int index)
    {
        Vector3 min;
        Vector3 max;
        if (index == 0)
        {
            min = minWristFromFencer;
            max = maxWristFromFencer;
        } else if (index == 1)
        {
            min = minTargetFromTip;
            max = maxTargetFromTip;
        } else if (index == 2)
        {
            min = minSelfEpeeFromFencer;
            max = maxSelfEpeeFromFencer;
        } else if (index == 3)
        {
            min = minEpeeTipFromEpee;
            max = maxEpeeTipFromEpee;
        } else if (index == 4)
        {
            min = minOppEpeeFromFencer;
            max = maxOppEpeeFromFencer;
        } 
        else
        {
            return Vector3.zero;
        }

        var normX = GetNormalized(newVal.x, min.x, max.x);
        var normY = GetNormalized(newVal.y, min.y, max.y);
        var normZ = GetNormalized(newVal.z, min.z, max.z);
        return new Vector3(normX, normY, normZ);
    }

    public Vector3 GetNormalizedCapped(Vector3 newVal, int index)
    {
        Vector3 min;
        Vector3 max;
        if (index == 5)
        {
            min = minContactPoint;
            max = maxContactPoint;
        } else if (index == 6)
        {
            min = minImpulse;
            max = maxImpulse;
        } else
        {
            return Vector3.zero;
        }
        
        var normX = GetNormalizedCapped(newVal.x, min.x, max.x);
        var normY = GetNormalizedCapped(newVal.y, min.y, max.y);
        var normZ = GetNormalizedCapped(newVal.z, min.z, max.z);
        return new Vector3(normX, normY, normZ);
    }
}
