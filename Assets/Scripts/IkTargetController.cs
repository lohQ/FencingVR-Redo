using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

public class IkTargetController : MonoBehaviour
{
    public Transform upperArm, foreArm, hand;
    public Transform handIkTarget, handIkTargetParent;
    public Vector3 fencerForward, fencerRight;

    # region collision detection
    public List<Transform> raycastSources;
    public string weaponLayer;
    private int _ignoreWeaponLayerMask;
    # endregion

    # region position control
    public float speed;
    private float _armLength;
    # endregion
    
    # region rotation control
    public RotationLimit rotationLimit;
    public float rotationSpeed;     // by degrees
    private Quaternion _initialLocalRotation;
    # endregion
    
    public bool log;

    void Start()
    {
        _ignoreWeaponLayerMask = ~LayerMask.GetMask(weaponLayer);
        _armLength = (hand.position - foreArm.position).magnitude + (foreArm.position - upperArm.position).magnitude;
        _initialLocalRotation = handIkTarget.localRotation;
    }

    public void Initialize()
    {
        handIkTargetParent.position = hand.position;
        handIkTargetParent.rotation = foreArm.rotation;
        handIkTarget.localRotation = _initialLocalRotation;
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
    
    void Update()
    {

    }

    void AdjustHandIkToEpee()
    {
        
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

        moveVector = _moveVector;
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
                    // Debug.DrawLine(source.position, source.position + moveVector, Color.green);
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
    }

}
