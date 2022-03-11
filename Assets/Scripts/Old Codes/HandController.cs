using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HandController : MonoBehaviour
{
    public AgentFencerSettings agentFencerSettings;
    private int _oppLayerMask;

    public Transform handEffector;
    public Transform hand;
    public Transform foreArm;
    public Transform upperArm;
    public Transform epee;
    public Transform epeeTip;

    public float handEffectorMaxRotDiff = 20f;
    public float handMaxDeviationDist = 0.3f;
    private float _scaledHandMaxDeviationDist;
    
    public float maxDegreesPerSec = 100f;
    public float maxDistancePerSec = 0.5f;
    public float rigWeightIncreasePerSec = 10f;
    public float rigWeightDecreasePerSec = 1f;

    public float tipDistThreshold = 0.5f;
    private float _scaledTipDistThreshold;

    private Vector3 _epeeToHandPositionOffset;
    private Quaternion _epeeToHandRotationOffset;
    private bool _collided;
    private Dictionary<Collider, Collision> _collisions;
    private float _armLength;

    private Rig _ikRig;

    void Start()
    {
        var oppColor = PhysicsEnvSettings.GetOther(agentFencerSettings.fencerColor);
        _oppLayerMask = LayerMask.GetMask(PhysicsEnvSettings.GetFencerBodyLayer(oppColor)) + 
                        LayerMask.GetMask(PhysicsEnvSettings.GetFencerWeaponLayer(oppColor));
        
        _scaledTipDistThreshold = tipDistThreshold * PhysicsEnvSettings.ScaleFactor;
        _scaledHandMaxDeviationDist = handMaxDeviationDist * PhysicsEnvSettings.ScaleFactor;
        Debug.DrawLine(hand.position, hand.position + _scaledHandMaxDeviationDist * Vector3.up, Color.green, 5f);

        var epeeFollow = handEffector.GetChild(0);
        _epeeToHandPositionOffset = -epeeFollow.localPosition;

        var rigBuilder = GetComponent<RigBuilder>();
        _ikRig = rigBuilder.layers[0].rig;
        _ikRig.weight = 0;
        _enabled = false;
        _collided = false;
        _collisions = new Dictionary<Collider, Collision>();
        var upperArmLength = (upperArm.position - foreArm.position).magnitude;
        var foreArmLength = (foreArm.position - hand.position).magnitude;
        _armLength = (float) Math.Sqrt(upperArmLength * upperArmLength + foreArmLength * foreArmLength);
    }

    public float sqrtMaxIkRigWeight = 0.95f;

    void FixedUpdate()
    {
        if (!_enabled) return;
        
        if (!_collided)
        {
            
            
            // carefully move the hand effector...
            handEffector.position = Vector3.MoveTowards(
                handEffector.position, hand.position, maxDistancePerSec * Time.deltaTime * PhysicsEnvSettings.ScaleFactor);
            handEffector.rotation = Quaternion.RotateTowards(handEffector.rotation, hand.rotation, maxDegreesPerSec * Time.deltaTime);

            // carefully decrease the rig weight...
            _ikRig.weight = Mathf.Max(_ikRig.weight - rigWeightDecreasePerSec * Time.deltaTime, 0);
        }
        else
        {
            // calculate difference between current hand position and supposed hand position (based on epee)
            var distanceVector = ((epee.position + _epeeToHandPositionOffset) - hand.position) / 2;
            var handRotationFromEpee = epee.rotation * _epeeToHandRotationOffset;   // the order doesn't matter?
            var angleDiff = Quaternion.Angle(hand.rotation, handRotationFromEpee);

            // don't move hand effector too far from arm
            var resultingDistanceFromUpperArm = (hand.position + distanceVector - upperArm.position).magnitude;
            if ((resultingDistanceFromUpperArm > _armLength))
            {
                // if (a)
                // {
                //     distanceVector *= (_armLength / resultingDistanceFromUpperArm);
                // }
                // else
                // {
                    distanceVector = -distanceVector / 2;   // pull epee towards hand
                // }
            }
            Debug.DrawLine(hand.position, hand.position + distanceVector, Color.green, 1f);

            // carefully move the hand effector...
            handEffector.position = Vector3.MoveTowards(
                handEffector.position, hand.position + distanceVector, maxDistancePerSec * Time.deltaTime * PhysicsEnvSettings.ScaleFactor);
            handEffector.rotation = Quaternion.RotateTowards(
                hand.rotation, handRotationFromEpee, maxDegreesPerSec * Time.deltaTime);

            // calculate the new rig weight and kinda generously increase / carefully decrease it
            var distRatio = Mathf.Min(distanceVector.magnitude / _scaledHandMaxDeviationDist, sqrtMaxIkRigWeight);
            var angleRatio = Mathf.Min(angleDiff / handEffectorMaxRotDiff, sqrtMaxIkRigWeight);
            var newWeight = Mathf.Max(distRatio * distRatio, angleRatio * angleRatio);
            if (newWeight >= _ikRig.weight)
            {
                _ikRig.weight = Mathf.Min(_ikRig.weight + rigWeightIncreasePerSec * Time.deltaTime, newWeight);
                Debug.Log("newWeight is "+ newWeight + "; increase right hand ik rig weight to " + _ikRig.weight);
            }
            else
            {
                _ikRig.weight = Mathf.Max(_ikRig.weight - rigWeightDecreasePerSec * Time.deltaTime, newWeight);
                Debug.Log("newWeight is "+ newWeight + "; decrease right hand ik rig weight to " + _ikRig.weight);
            }
        }
        _gizmosCenter = handEffector.position;
    }


    public float TipDistanceFromOpponent()
    {
        var hits = Physics.RaycastAll(
            epeeTip.position, epeeTip.forward, _scaledTipDistThreshold, _oppLayerMask);
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag(PhysicsEnvSettings.TargetAreaTag))
            {
                return hit.distance;
            }
        }
        return -1;
    }

    private bool _enabled;

    public void EnableHandIk(bool enable)
    {
        _enabled = enable;
        Debug.Log("_enabled set to " + enable);
        if (enable)
        {
            handEffector.position = hand.position;
            handEffector.rotation = hand.rotation;
            epee.position = handEffector.position - _epeeToHandPositionOffset;
            epee.rotation = handEffector.rotation * Quaternion.Inverse(_epeeToHandRotationOffset);
        }
    }

    // Assumption: one collider only generate one collision with epee
    
    public void RegisterCollision(Collision collision)
    {
        if (!_collided) Debug.Log("registered new collision with " + collision.collider.name);
        _collisions[collision.collider] = collision;
        _collided = true;
    }

    public void UnregisterCollision(Collider otherCollider)
    {
        Debug.Log("unregistered a collision with " + otherCollider.name);
        _collisions.Remove(otherCollider);
        if (_collisions.Count == 0) _collided = false;
    }
    
    public float[] GetCollisionFloats()
    {
        // need to normalize for buffer observation!
        var collisionFloats = new float[6 * _collisions.Count];
        int i = 0;
        foreach (var collision in _collisions.Values)
        {
            if (collision.contactCount == 0)
            {
                Debug.Log("contact count is zerooooooooo!");
                continue;
            }
            var midContactPoint = collision.contacts[collision.contactCount / 2];
            var inversedPoint = epee.InverseTransformPoint(midContactPoint.point);
            inversedPoint /= (epeeTip.position - epee.position).magnitude;  // is this correct????
            collisionFloats[i * 6] = inversedPoint.x;
            collisionFloats[i * 6 + 1] = inversedPoint.y;
            collisionFloats[i * 6 + 2] = inversedPoint.z;
            collisionFloats[i * 6 + 3] = midContactPoint.normal.x;
            collisionFloats[i * 6 + 3] = midContactPoint.normal.y;
            collisionFloats[i * 6 + 3] = midContactPoint.normal.z;
            i += 1;
        }

        return collisionFloats[..(i*6)];
    }
    
    public float noStepForwardAngleThreshold = 30f;
    public bool NoCollisionInFront(Vector3 frontDirection)
    {
        // This function is like not much use. Maybe can delete later. 
        if (!_collided) return true;

        foreach (var collision in _collisions.Values)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                var angle = Vector3.SignedAngle(-frontDirection, contact.normal, Vector3.up);
                if (angle < noStepForwardAngleThreshold && angle > -noStepForwardAngleThreshold)
                {
                    Debug.Log($"angle smaller than {noStepForwardAngleThreshold}, step forward disallowed");
                    return false;
                }
            }
        }

        return true;
    }
    
    private Vector3 _gizmosCenter = Vector3.zero;
    private void OnDrawGizmos()
    {
        if (_gizmosCenter != Vector3.zero)
        {
            Gizmos.DrawWireSphere(_gizmosCenter, 5f);
        }
    }

    public float GetIkRigWeightRatio()
    {
        return _ikRig.weight / (sqrtMaxIkRigWeight * sqrtMaxIkRigWeight);
        // find out where the epee is supposed to go in next frame?
        // then dot the direction vector and the impulse vector
        // i.e., if the impulse vector opposite direction as the direction vector, return higher value
        // if the impulse vector basically perpendicular to the direction vector, return lower value
        
    }

}