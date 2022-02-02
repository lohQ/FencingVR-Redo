using System;
using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class IkTargetController : MonoBehaviour
{
    public AgentFencerSettings agentFencerSettings;

    [Header("Ik Targets and Sources")] 
    public Transform upperArm;
    public Transform foreArm;
    public Transform hand;
    public Transform handIkTarget;
    public Transform handIkTargetParent;
    public Transform head;
    public Transform headIkAimTarget;
    
    [Header("Collision Detection")]
    public List<Transform> raycastSources;
    private int _ignoreWeaponLayerMask;
    private int _ignoreSelfLayerMask;

    [Header("Position Control")]
    public Transform epee;
    public Transform epeeFollow;
    public Transform epeeTip;
    private float _armLength;
    private float _scaledTeleportThreshold;
    private float _scaledMissTolerance;
    private float _ikMissToleranceDegrees;
    private float _maxVelocity;
    private float _maxAcceleration;
    private Vector3 _curVelocity;
    private Vector3 _curAcceleration;
    
    [Header("Rotation Control")]
    public RotationLimit rotationLimit;
    private float _rotationVelocity;
    private Quaternion _initialLocalRotation;

    // [Header("Head Aim Control")]
    private float _headTargetMoveMaxDistance;
    private float _headTargetMoveSpeed;
    private Vector3 _headTargetInitialPosition;
    private Vector3 _headTargetMoveVector;
    
    public bool log;

    void Start()
    {
        _armLength = (hand.position - foreArm.position).magnitude + (foreArm.position - upperArm.position).magnitude;
        _initialLocalRotation = handIkTarget.localRotation;

        var weaponLayer = PhysicsEnvSettings.GetFencerWeaponLayer(agentFencerSettings.fencerColor);
        var bodyLayer = PhysicsEnvSettings.GetFencerBodyLayer(agentFencerSettings.fencerColor);
        _ignoreWeaponLayerMask = ~LayerMask.GetMask(weaponLayer);
        _ignoreSelfLayerMask = ~(LayerMask.GetMask(bodyLayer) + LayerMask.GetMask(weaponLayer));

        _scaledTeleportThreshold = agentFencerSettings.ikMissTeleportThreshold * PhysicsEnvSettings.ScaleFactor;
        _scaledMissTolerance = agentFencerSettings.ikMissTolerance * PhysicsEnvSettings.ScaleFactor;
        _ikMissToleranceDegrees = agentFencerSettings.ikMissToleranceDegrees;
        
        _maxVelocity = agentFencerSettings.handEffectorMaxVelocity * PhysicsEnvSettings.ScaleFactor;
        _maxAcceleration = agentFencerSettings.handEffectorMaxAcceleration * PhysicsEnvSettings.ScaleFactor;
        _rotationVelocity = agentFencerSettings.handEffectorRotationVelocity;
        _headTargetMoveMaxDistance = agentFencerSettings.headAimEffectorMaxDistanceFromOrigin;
        _headTargetMoveSpeed = agentFencerSettings.headAimEffectorMoveVelocity;
        
        _curVelocity = Vector3.zero;
        _curAcceleration = Vector3.zero;
    }

    public void Initialize()
    {
        handIkTargetParent.position = hand.position;
        handIkTargetParent.rotation = foreArm.rotation;
        handIkTarget.localRotation = _initialLocalRotation;
        _headTargetInitialPosition = head.position + 100 * head.forward;     // scaled up
        headIkAimTarget.position = _headTargetInitialPosition;
        epee.position = epeeFollow.position;
        epee.rotation = epeeFollow.rotation;
        _curVelocity = Vector3.zero;
        _curAcceleration = Vector3.zero;
    }

    // private Vector3 _moveVector;
    private Vector3 _rotationToApply;
    
    public void SetMoveVector(int accX, int accY, int accZ)
    {
        // _moveVector = Vector3.zero;
        // _moveVector += (x-1) * agentFencerSettings.fencerRight;
        // _moveVector += (y-1) * Vector3.up;
        // _moveVector += (z-1) * agentFencerSettings.fencerForward;
        // _moveVector = _moveVector.normalized * _speed * Time.deltaTime;
        // _moveVector *= speedFactor;

        var acceleration = (accX * agentFencerSettings.fencerRight 
                               + accY * Vector3.up 
                               + accZ * agentFencerSettings.fencerForward) 
                           * _maxAcceleration / agentFencerSettings.accelerationLevelCount * Time.deltaTime;
        var newVelocity = _curVelocity + acceleration;
        if (newVelocity.magnitude > _maxVelocity)
        {
            newVelocity = newVelocity.normalized * _maxVelocity;
        }
        _curAcceleration = newVelocity - _curVelocity;
        if (log && _curAcceleration.magnitude != 0)
        {
            Debug.Log("velocity: " + (_curVelocity + _curAcceleration));
        }
    }
    
    public void SetRotationToApply(int x, int y, int z, bool fast)
    {
        // var moveVector = Vector3.zero;
        _rotationToApply = Vector3.zero;
        _rotationToApply += x * Vector3.right;
        _rotationToApply += y * Vector3.up;
        _rotationToApply += z * Vector3.forward;
        _rotationToApply = _rotationToApply * _rotationVelocity * Time.deltaTime;
        if (fast)
        {
            _rotationToApply *= 2;
        }
    }

    public float TipDistanceFromOpponent()
    {
        var hits = Physics.RaycastAll(epeeTip.position, epeeTip.forward, 70f, _ignoreSelfLayerMask);
        // Debug.DrawLine(epeeTip.position, epeeTip.position + 70 * epeeTip.forward, Color.green);
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Target Area"))
            {
                return hit.distance;
            }
        }
        return -1;
    }
    
    void AdjustHandIkToEpee()
    {
        var epeeFollowToEpee = epee.position - epeeFollow.position;
        if (epeeFollowToEpee.magnitude > _scaledTeleportThreshold)
        {
            // when epee too far away, teleport epee to hand
            epee.position = epeeFollow.position;
            if (log) Debug.Log("teleported epee");
        }
        else if (epeeFollowToEpee.magnitude > _scaledMissTolerance)
        {
            // when epee not too far away, move hand to epee
            _curAcceleration += epeeFollowToEpee;
            if (_curAcceleration.magnitude > _maxAcceleration)
            {
                _curAcceleration = _curAcceleration.normalized * _maxAcceleration;
            }

            if (log) Debug.Log("adjusted acceleration to " + _curAcceleration);
            if (log) Debug.DrawLine(
                    epeeFollow.position, epeeFollow.position + _curVelocity + _curAcceleration, 
                    Color.green, 5f);

            // _moveVector += _speed * Time.deltaTime * epeeFollowToEpee.normalized;
            // _moveVector *= (_speed * Time.deltaTime) / _moveVector.magnitude;
            // Debug.DrawLine(epeeFollow.position, epeeFollow.position + _moveVector, Color.green, 5f);
            // if (_moveVector.magnitude > epeeFollowToEpee.magnitude)
            // {
            //     _moveVector = epeeFollowToEpee;
            // }
        }

        var epeeFollowToEpeeRot = Vector3.Angle(epeeFollow.forward, epee.forward);
        if (epeeFollowToEpeeRot > _ikMissToleranceDegrees)
        {
            _rotationToApply = (Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(epeeFollow.forward, epee.forward), epeeFollowToEpeeRot / 3)
                                * Quaternion.Euler(_rotationToApply) ).eulerAngles;
            // Debug.Log("epeeFollowToEpeeRot: " + epeeFollowToEpeeRot + "; rotation to apply: " + _rotationToApply);
        }
    }
    
    public void SetHeadTargetMoveVector(int x, int y)
    {
        _headTargetMoveVector = Vector3.zero;
        _headTargetMoveVector += x * agentFencerSettings.fencerRight;
        _headTargetMoveVector += y * Vector3.up;
        _headTargetMoveVector = _headTargetMoveSpeed * Time.deltaTime * _headTargetMoveVector.normalized;

        if (((headIkAimTarget.position + _headTargetMoveVector) - _headTargetInitialPosition).magnitude > _headTargetMoveMaxDistance)
        {
            _headTargetMoveVector = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        # region position control

        AdjustHandIkToEpee();
        if (_curAcceleration != Vector3.zero)
        {
            var moveVector = (_curVelocity + _curAcceleration) * Time.deltaTime;
            if ((hand.position + moveVector - upperArm.position).magnitude > _armLength)
            {
                if (log) Debug.Log("moving out of range, do nothing");
                moveVector = Vector3.zero;
            }
            else
            {
                foreach (var source in raycastSources)
                {
                    var hits = Physics.RaycastAll(
                        source.position, moveVector, moveVector.magnitude * 1.1f, _ignoreWeaponLayerMask);
                    foreach (var hit in hits)
                    {
                        if (hit.distance <= moveVector.magnitude)
                        {
                            if (log) Debug.Log("Raycast hit. Set moveVector to 0");
                            moveVector = Vector3.zero;
                            break;
                        }
                    }
                }
            }

            if (moveVector == Vector3.zero)
            {
                _curVelocity = Vector3.zero;
                if (log) Debug.Log("set curVelocity to zero");
            }
            else
            {
                handIkTargetParent.position += moveVector;
                _curVelocity += _curAcceleration;
                rotationLimit.Apply();
            }
            _curAcceleration = Vector3.zero;
        }
        
        # endregion
        
        # region rotation control
        
        var rotationToApply = _rotationToApply;
        if (rotationToApply != Vector3.zero)
        {
            var handIkTargetPos = handIkTarget.position;
            var willCollide = false;
            Collider[] containingColliders = new Collider[3];
            foreach (Transform source in raycastSources)
            {
                var toSourceVector = source.position - handIkTargetPos;
                var rotatedSourceVector = Quaternion.Euler(rotationToApply) * toSourceVector;
                var containingColliderCount = Physics.OverlapSphereNonAlloc(
                    handIkTargetPos + rotatedSourceVector, 0f, containingColliders, _ignoreWeaponLayerMask);
                // Debug.DrawLine(source.position, handIkTargetPos + rotatedSourceVector, Color.green);
                if (containingColliderCount > 0)
                {
                    if (log) Debug.Log(source.name + " will be in collider if apply rotation. Do nothing. ");
                    willCollide = true;
                    break;
                }
            }
            
            if (!willCollide)
            {
                handIkTarget.Rotate(rotationToApply);
                rotationLimit.Apply();
            }

            _rotationToApply = Vector3.zero;
        }
        
        # endregion

        headIkAimTarget.position += _headTargetMoveVector;

    }
    
    
}
