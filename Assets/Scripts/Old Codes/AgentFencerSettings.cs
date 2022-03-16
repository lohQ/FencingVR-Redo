using UnityEngine;

public class AgentFencerSettings : MonoBehaviour
{
    public FencerColor fencerColor;
    public Vector3 fencerForward, fencerRight;

    [Header("Hand Ik Control")]
    public float handEffectorMaxVelocity = 5f;
    public float handEffectorMaxAcceleration = 10f;
    // public float handEffectorRotationVelocity = 120f;
    public float handEffectorMaxAngularVelocity = 120f;
    public float handEffectorMaxAngularAcceleration = 180f;
    public int accelerationLevelCount = 2;  // should be same number as branch 0, 1, 2 size
    public int angAccelerationLevelCount = 2;  // should be same number as branch 3, 4, 5 size

    [Header("Hand IK Adjust")] 
    public float ikMissTeleportThreshold = 0.3f;
    public float ikMissTolerance = 0.1f;
    public float ikMissToleranceDegrees = 20f;
    
    [Header("Head Ik Control")]
    public float headAimEffectorMoveVelocity = 0.5f;
    public float headAimEffectorMaxDistanceFromOrigin = 0.5f;

}
