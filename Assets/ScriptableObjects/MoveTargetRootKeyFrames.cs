using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MoveTargetRootKeyFrames", order = 0)]
public class MoveTargetRootKeyFrames : ScriptableObject
{
    // previously public and sometimes it will reset to zero, now use public method to track who is doing that
    private List<Vector3> _translationData;
    private List<Quaternion> _rotationData;

    public List<Vector3> cloneTranslationData;
    public List<Quaternion> cloneRotationData;

    private void Awake()
    {
        // write data and clone data happen in different run, so can save in previous run and clone in Awake. 
        cloneTranslationData = new List<Vector3>(_translationData);
        cloneRotationData = new List<Quaternion>(_rotationData);
    }

    public void WriteTranslationData(List<Vector3> newData){
        Debug.Log($"newData of length {newData.Count} written to _translationData");
        _translationData = new List<Vector3>(newData);
    }

    public void WriteRotationData(List<Quaternion> newData)
    {
        Debug.Log($"newData of length {newData.Count} written to _rotationData");
        _rotationData = new List<Quaternion>(newData);
    }
}
