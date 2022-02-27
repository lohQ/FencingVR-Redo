using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HandEffectorController : MonoBehaviour
{
    // agent action:
    // move hand effector towards one target
    // rotate hand effector to point to one target

    [Header("Reference Transforms")]
    public Transform hand;
    public Transform moveTargetParent;
    public PointTarget pointTarget;
    public PalmRotation palmRotation;
    public Transform epee;
    public Transform epeeTip;

    [Header("Control Target & Parameters")]
    public Transform handEffector;
    public Transform epeeFollow;
    public float maxVelocity;
    public float maxAngularVelocity;

    public bool debug = true;

    private List<Transform> _moveTargets;
    private int _moveTargetIndex;
    private int _pointTargetIndex;
    private int _palmTargetIndex;
    private Dictionary<Collider, Collision> _collisions;
    private float _epeeLength;
    private bool _enabled;
    private Rig _ikRig;
    private Vector3 _movePrevFrame;

    private void Start()
    {
        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _ikRig.weight = 0;
        
        _moveTargets = new List<Transform>();
        foreach (Transform child in moveTargetParent)
        {
            _moveTargets.Add(child);
        }

        _moveTargetIndex = 0;
        _pointTargetIndex = 0;
        _palmTargetIndex = 0;

        _movePrevFrame = Vector3.zero;
        _collisions = new Dictionary<Collider, Collision>();
        _epeeLength = (epeeTip.position - epee.position).magnitude;
    }

    public bool adjustToPhysicEpee;

    private void MoveHandEffector(String description, Vector3 move)
    {
        if (debug) Debug.Log($"[{description}] move handEffector by {move}");
        handEffector.position += move;
    }
    
    private void RotateHandEffector(String description, Quaternion rot)
    {
        if (debug) Debug.Log($"[{description}] rotate handEffector by {rot.eulerAngles}");
        handEffector.rotation = rot * handEffector.rotation;
    }

    private void AdjustToPhsyicEpee()
    {
        // cap if exceed wrist rotation limit (note that pointTarget does not consider suppination / pronation here)
        var capContactPointResult = pointTarget.CapPointTarget(epeeTip.position);
        var rotateToPoint = capContactPointResult.Item1;
        var capped = capContactPointResult.Item2;

        // toVect is the (capped) current epeeVector
        var toVect = rotateToPoint - epee.position;

        // 'current epee -> collision point' corresponds to 'current epeeFollow -> supposed collision point'
        // rotate 'handEffector -> supposed collision point' to 'handEffector -> capped actual collision point'

        
        // if epeeFollow and epee has different position
        // WILL NEED COLLISION POINT!!!
        // how to handle multiple collision points?
        // if similar normal direction, pick max; if different normal direction, cancel out?
        // ok actually let me handle ONE collision point first...
        
        // if (debug) Debug.DrawRay(epee.position, fromVect, Color.cyan);
        // if (debug) Debug.DrawRay(epeeFollow.position, toVect, Color.blue);
        
        // var adjustRotationRequired = Quaternion.FromToRotation(fromVect, toVect);
        // adjustRotationRequired.ToAngleAxis(out var angle, out var axis);
        // angle = Mathf.Min(angle, maxAngularVelocity * Time.deltaTime * 2);  // double that of maxAngularVelocity!
        // var adjustRotationToApply = Quaternion.AngleAxis(angle, axis);
        // // handEffector.rotation = adjustRotationToApply * handEffector.rotation;
        // RotateHandEffector("SystemAdjustRotation", adjustRotationToApply);

        foreach (var collision in _collisions.Values)
        {
            var midContact = collision.GetContact(collision.contactCount / 2);
            // if (debug) Debug.DrawRay(midContact.point, collision.impulse, Color.blue);

            // fromVect is the epeeVector with rotation of epeeFollow
            var fromVect = epeeFollow.rotation * Quaternion.Inverse(epee.rotation) * (epeeTip.position - epee.position);

            var collisionPointFromRootRatio = (midContact.point - epee.position).magnitude / _epeeLength;
            var toCollisionPt = epee.position + toVect * collisionPointFromRootRatio;
            var fromCollisionPt = epeeFollow.position + fromVect * collisionPointFromRootRatio;
            var from = fromCollisionPt - hand.position;
            var to = toCollisionPt - hand.position;
            // green line should be closer to epee than red line
            // BECAUSE IT ONLY DEAL WITH COLLISION WITH GUARDDDDD?!!! Well, apparantly nope. 
            if (debug) Debug.DrawRay(hand.position, from, Color.red, 1f);
            if (debug) Debug.DrawRay(hand.position, to, Color.magenta, 1f);
            
            var adjustRotationRequired = Quaternion.FromToRotation(from, to);
            adjustRotationRequired.ToAngleAxis(out var angle, out var axis);
            angle = Mathf.Min(angle, maxAngularVelocity * Time.deltaTime * 2);  // double that of maxAngularVelocity!
            var adjustRotationToApply = Quaternion.AngleAxis(angle, axis);
            RotateHandEffector("SystemAdjustRotation", adjustRotationToApply);
            
            if (capped)
            {
                // project current epee contact point on to capped epee vector
                // supposedly will get the point nearest to the contact point on the capped epee vector
                // then translate this point to current epee contact point
                var nearestPtOnToVect = epee.position +
                                        Vector3.Project(midContact.point - epee.position, toVect);
                var adjustMoveVector = midContact.point - nearestPtOnToVect;
                
                // var pointFromRootRatio = (midContact.point - epee.position).magnitude / _epeeLength;
                // var adjustMoveVector = 2 * (1 - pointFromRootRatio) * (midContact.point - rotateToPoint);
                if (adjustMoveVector.magnitude > maxVelocity * Time.deltaTime)
                {
                    adjustMoveVector = adjustMoveVector.normalized * maxVelocity * Time.deltaTime;
                }
                
                // cap adjustMoveVector?

                // if (debug) Debug.DrawLine(midContact.point, nearestPtOnToVect, Color.green);
                // handEffector.position += adjustMoveVector;
                MoveHandEffector("SystemAdjustTranslation", adjustMoveVector);
            }
        }
    }

    public bool blockMovementPushover;
    public bool slideMovementPushover;
    
    private void FixedUpdate()
    {
        if (_ikRig.weight == 0) return;

        float angle;
        Vector3 axis;
        // collide near tip then rotate, collide near root then translate <- decided by physics engine

        // question: will rotating handEffector towards actual epee result in collider stuck?
        // a lot better, but still got issues. Probably because the counter rotation didn't totally cancel out applied rotation
        // but let's test with both moving epee first! <- kinda no effect... pass through still happens
        // the red line doesn't look right

        List<Collider> collidersToRemove = new List<Collider>();
        foreach (var pair in _collisions)
        {
            if (pair.Value.contactCount == 0)
            {
                collidersToRemove.Add(pair.Key);
            }
        }
        foreach (var c in collidersToRemove)
        {
            _collisions.Remove(c);
        }
        
        if (_collisions.Count > 0 && adjustToPhysicEpee)
        {
            AdjustToPhsyicEpee();
        }
        
        List<Vector3> collisionPoints = new List<Vector3>();
        List<Vector3> collisionNormals = new List<Vector3>();
        foreach (var collision in _collisions.Values)
        {
            var midContact = collision.GetContact(collision.contactCount / 2);
            if (debug) Debug.DrawRay(midContact.point, collision.impulse, Color.blue);
            collisionPoints.Add(midContact.point);
            collisionNormals.Add(midContact.normal);
        }

        
        // rotate hand left/right, up/down
        /*
        // hand should have same position as pointTarget as it is initialiezd so
        // plus moving hand = rotating elbow = moving pointTarget too
        */
        var targetLookAt = pointTarget.GetPosition(_pointTargetIndex);
        var from = epeeTip.position - hand.position;
        var to = targetLookAt - hand.position;
        var rotationRequired = Quaternion.FromToRotation(from, to);
        rotationRequired.ToAngleAxis(out angle, out axis);
        if (angle > 1)
        {
            if (debug) Debug.DrawRay(hand.position, to, Color.green);
            // if (debug) Debug.Log($"angle to rotate: {angle}; axis to rotate: {axis}");
            angle = Mathf.Min(angle, maxAngularVelocity * Time.deltaTime);
            var rotationToApply = Quaternion.AngleAxis(angle, axis);

            // check whether this rotation push collision points over
            if (blockMovementPushover || slideMovementPushover)
            {
                for (int i = 0; i < collisionPoints.Count; i++)
                {
                    var toContact = collisionPoints[i] - hand.position;
                    var afterRotation = rotationToApply * toContact;
                    var collisionPointVector = afterRotation - toContact;
                    var moveVectAngleWithCollisionNormal = Vector3.Angle(collisionNormals[i], collisionPointVector);
                    if (debug) Debug.DrawRay(collisionPoints[i], collisionNormals[i] * 10, Color.blue);
                    if (debug) Debug.DrawLine(collisionPoints[i], hand.position + afterRotation, Color.green, 1f);
                    // if (debug) Debug.Log($"moveVectAngleWithCollisionNormal: {moveVectAngleWithCollisionNormal}");
                    if (moveVectAngleWithCollisionNormal > 90)
                    {
                        // don't rotate if is pushing?
                        if (blockMovementPushover)
                        {
                            // seems like handEffector is not moved. But still the epee move through another epee
                            // what is moving it???
                            if (debug) Debug.Log("moveVectAngleWithCollisionNormal > 90, not rotating");
                            rotationToApply = Quaternion.identity;
                            break;
                        }
                        if (slideMovementPushover)
                        {
                            // if (debug) Debug.Log("moveVectAngleWithCollisionNormal > 90, sliding (handle only one collision for now");
                            var newCollisionPointVector = Vector3.ProjectOnPlane(collisionPointVector, collisionNormals[i]);
                            var newCollisionPoint = collisionPoints[i] + newCollisionPointVector;
                            if (debug) Debug.DrawRay(collisionPoints[i], newCollisionPointVector, Color.cyan, 1f);
                            var newAfterRotation = newCollisionPoint - hand.position;
                            rotationToApply = Quaternion.FromToRotation(from, newAfterRotation);
                            break;
                        }
                    }
                }
            }
            
            // handEffector.rotation = rotationToApply * handEffector.rotation;
            RotateHandEffector("UserWristRotation", rotationToApply);
        }
        
        // rotate hand palm up/down
        // rotationRequired = palmRotation.GetPalmRotationToApply(_palmTargetIndex, debug);
        // rotationRequired.ToAngleAxis(out angle, out axis);
        // if (angle > 1)
        // {
        //     // if (debug) Debug.Log($"angle to twist: {angle}; axis to twist: {axis}");
        //     // if (debug) Debug.DrawRay(hand.position, to, Color.green);
        //     angle = Mathf.Min(angle, maxAngularVelocity * Time.deltaTime);
        //     var rotationToApply = Quaternion.AngleAxis(angle, axis);
        //     // handEffector.rotation = Quaternion.AngleAxis(angle, axis) * handEffector.rotation;
        //     RotateHandEffector("UserPalmRotation", rotationToApply);
        // }

        // move hand to defined position
        var targetPosition = _moveTargets[_moveTargetIndex].position;
        var moveVector = (targetPosition - handEffector.position) * Time.deltaTime;
        if (moveVector.magnitude > 0.01)
        {
            if (moveVector.magnitude > maxVelocity * Time.deltaTime)
            {
                moveVector = moveVector.normalized * maxVelocity * Time.deltaTime;
            }
            // cap the change of speed?
            if ((moveVector - _movePrevFrame).magnitude > maxVelocity * Time.deltaTime)
            {
                moveVector = (moveVector - _movePrevFrame).normalized * maxVelocity * Time.deltaTime;
            }
            if (debug) Debug.DrawLine(handEffector.position, handEffector.position + moveVector, Color.green);
            // handEffector.position += moveVector;
            MoveHandEffector("UserHandMovement", moveVector);
            _movePrevFrame = moveVector;
        }
        
        // show the supposed epee position if follow handEffector
        if (debug) Debug.DrawRay(epeeFollow.position, 
            epeeFollow.rotation * Quaternion.Inverse(epee.rotation) * (epeeTip.position - epee.position),
            Color.cyan);

    }

    public void SetMoveTarget(int targetIndex)
    {
        _moveTargetIndex = targetIndex;
    }

    public void SetPointTarget(int targetIndex)
    {
        _pointTargetIndex = targetIndex;
    }
    
    public void SetPalmTarget(int targetIndex)
    {
        _palmTargetIndex = targetIndex;
    }

    public void RegisterCollision(Collision collision)
    {
        _collisions[collision.collider] = collision;
        var colliderNames = "";
        foreach (var otherCollider in _collisions.Keys)
        {
            colliderNames += otherCollider.name + ", ";
        }
        // if (debug) Debug.Log("RegisterCollision, current collisions are: " + colliderNames);
    }

    public void UnregisterCollision(Collider otherCollider)
    {
        _collisions.Remove(otherCollider);
        var colliderNames = "";
        foreach (var other in _collisions.Keys)
        {
            colliderNames += other.name + ", ";
        }
        // if (debug) Debug.Log("UnregisterCollision, current collisions are: " + colliderNames);
    }

    public void DisableIK()
    {
        _ikRig.weight = 0;
    }
    
    public void Initialize()
    {
        _ikRig.weight = 1;
        moveTargetParent.position = hand.position;
        moveTargetParent.localRotation = Quaternion.identity;

        handEffector.position = hand.position;
        handEffector.rotation = hand.rotation;
        epee.position = epeeFollow.position;
        epee.rotation = epeeFollow.rotation;

        // rotation is hard set
        // pointTarget.position = hand.position;
        palmRotation.SetNeutralXVector();
        
        // Debug.Log("intialized");
    }
    
}
