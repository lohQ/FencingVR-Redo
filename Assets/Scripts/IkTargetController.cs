using System;
using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class IkTargetController : MonoBehaviour
{
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
    public string weaponLayer;
    private int _ignoreWeaponLayerMask;
    public string bodyLayer;
    private int _ignoreSelfLayerMask;

    [Header("Position Control")]
    public float speed;
    public Vector3 fencerForward, fencerRight;
    public Transform epee, epeeFollow, epeeTip;
    public float ikMissTolerance = 0.1f;
    public float ikMissToleranceDegrees = 30f;
    public float ikMissTeleportThreshold = 0.8f;
    private float _armLength;
    
    [Header("Rotation Control")]
    public RotationLimit rotationLimit;
    public float rotationSpeed;     // by degrees
    private Quaternion _initialLocalRotation;

    [Header("Head Aim Control")]
    public float headTargetMoveMaxDistance;
    public float headTargetMoveSpeed;
    private Vector3 _headTargetInitialPosition;
    private Vector3 _headTargetMoveVector;
    
    public bool log;

    void Start()
    {
        _ignoreWeaponLayerMask = ~LayerMask.GetMask(weaponLayer);
        _ignoreSelfLayerMask = ~(LayerMask.GetMask(bodyLayer) + LayerMask.GetMask(weaponLayer));
        _armLength = (hand.position - foreArm.position).magnitude + (foreArm.position - upperArm.position).magnitude;
        _initialLocalRotation = handIkTarget.localRotation;
        // useHandAsBasePosition = false;
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
    }

    private Vector3 _moveVector;
    private Vector3 _rotationToApply;
    
    public void SetMoveVector(int x, int y, int z, bool fast)
    {
        // var moveVector = Vector3.zero;
        _moveVector = Vector3.zero;
        _moveVector += (x-1) * fencerRight;
        _moveVector += (y-1) * Vector3.up;
        _moveVector += (z-1) * fencerForward;
        _moveVector = _moveVector.normalized * speed * Time.deltaTime;
        if (fast)
        {
            _moveVector *= 2;
        }

    }
    
    public void SetRotationToApply(int x, int y, int z, bool fast)
    {
        // var moveVector = Vector3.zero;
        _rotationToApply = Vector3.zero;
        _rotationToApply += (x-1) * Vector3.right;
        _rotationToApply += (y-1) * Vector3.up;
        _rotationToApply += (z-1) * Vector3.forward;
        _rotationToApply = _rotationToApply * rotationSpeed * Time.deltaTime;
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
        // when some errors happen and epee left hand
        var epeeFollowToEpee = epee.position - epeeFollow.position;
        if (epeeFollowToEpee.magnitude > ikMissTeleportThreshold)
        {
            epee.position = epeeFollow.position;
        }
        else if (epeeFollowToEpee.magnitude > ikMissTolerance)
        {
            _moveVector += speed * Time.deltaTime * epeeFollowToEpee.normalized;
            _moveVector *= (speed * Time.deltaTime) / _moveVector.magnitude;
            // Debug.DrawLine(epeeFollow.position, epeeFollow.position + _moveVector, Color.green, 5f);
            // if (_moveVector.magnitude > epeeFollowToEpee.magnitude)
            // {
            //     _moveVector = epeeFollowToEpee;
            // }
        }

        var epeeFollowToEpeeRot = Vector3.Angle(epeeFollow.forward, epee.forward);
        if (epeeFollowToEpeeRot > ikMissToleranceDegrees)
        {
            _rotationToApply = (Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(epeeFollow.forward, epee.forward), epeeFollowToEpeeRot / 3)
                                * Quaternion.Euler(_rotationToApply) ).eulerAngles;
            // Debug.Log("epeeFollowToEpeeRot: " + epeeFollowToEpeeRot + "; rotation to apply: " + _rotationToApply);
        }
    }
    
    // public bool useHandAsBasePosition;  // when there's lunge then use hand as base position, else use handIkTargetParent's position

    public void SetHeadTargetMoveVector(int x, int y)
    {
        _headTargetMoveVector = Vector3.zero;
        _headTargetMoveVector += x * fencerRight;
        _headTargetMoveVector += y * Vector3.up;
        _headTargetMoveVector = headTargetMoveSpeed * Time.deltaTime * _headTargetMoveVector.normalized;

        if (((headIkAimTarget.position + _headTargetMoveVector) - _headTargetInitialPosition).magnitude > headTargetMoveMaxDistance)
        {
            _headTargetMoveVector = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        # region position control

        # region keyboard input
        var moveVector = Vector3.zero;
        // if (Input.GetKey(KeyCode.W))
        // {
        //     moveVector += Vector3.up;
        // }
        // else if (Input.GetKey(KeyCode.S))
        // {
        //     moveVector += -Vector3.up;
        // }
        // if (Input.GetKey(KeyCode.A))
        // {
        //     moveVector += -fencerRight;
        // }
        // else if (Input.GetKey(KeyCode.D))
        // {
        //     moveVector += fencerRight;
        // }
        // if (Input.GetKey(KeyCode.Q))
        // {
        //     moveVector += -fencerForward;
        // }
        // else if (Input.GetKey(KeyCode.E))
        // {
        //     moveVector += fencerForward;
        // }
        # endregion

        AdjustHandIkToEpee();
        moveVector = _moveVector;
        // var basePosition = useHandAsBasePosition ? hand.position : handIkTargetParent.position;
        if (moveVector != Vector3.zero)
        {
            // moveVector = moveVector.normalized * speed * Time.deltaTime;
            if (log) Debug.Log("moveVector this frame: " + moveVector);
            if ((hand.position + moveVector - upperArm.position).magnitude > _armLength)
            {
                if (log) Debug.Log("moving out of range, do nothing");
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
                handIkTargetParent.position += moveVector;
                if (log) Debug.Log("Add moveVector to _position, new handIkTargetParent.position: " + handIkTargetParent.position);
            }

            _moveVector = Vector3.zero;
        }
        // else
        // {
        //     handIkTargetParent.position = basePosition;
        // }
        
        # endregion
        
        # region rotation control
        
        # region keyboard input
        var rotationToApply = Vector3.zero;
        // if (Input.GetKey(KeyCode.I))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * Vector3.right;
        // }
        // else if (Input.GetKey(KeyCode.K))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * -Vector3.right;
        // }
        // if (Input.GetKey(KeyCode.J))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * Vector3.up;
        // }
        // else if (Input.GetKey(KeyCode.L))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * -Vector3.up;
        // }
        // if (Input.GetKey(KeyCode.U))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * Vector3.forward;
        // }
        // else if (Input.GetKey(KeyCode.O))
        // {
        //     rotationToApply += rotationSpeed * Time.deltaTime * -Vector3.forward;
        // }
        # endregion

        rotationToApply = _rotationToApply;
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
