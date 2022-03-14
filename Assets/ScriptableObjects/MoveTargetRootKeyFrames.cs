using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MoveTargetRootKeyFrames", order = 0)]
public class MoveTargetRootKeyFrames : ScriptableObject
{
    public List<Vector3> translationData;
    public List<Quaternion> rotationData;
}
