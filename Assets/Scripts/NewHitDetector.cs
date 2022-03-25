using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NewHitDetector : MonoBehaviour
{
    public FollowFootwork followFootwork;
    public float impulseThreshold;
    public float hitAngleThreshold;

    private FencerColor _fencerColor;
    private int _otherBodyLayer;
    private int _otherWeaponLayer;
    private NewGameController _gameController;

    private Dictionary<Collider, Collision> _collisions;

    private void Start()
    {
        _collisions = new Dictionary<Collider, Collision>();
    }

    public void Initialize(FencerColor thisColor, FencerColor otherColor, NewGameController gameController)
    {
        _fencerColor = thisColor;
        _otherBodyLayer = LayerMask.NameToLayer(PhysicsEnvSettings.GetFencerBodyLayer(otherColor));
        _otherWeaponLayer = LayerMask.NameToLayer(PhysicsEnvSettings.GetFencerWeaponLayer(otherColor));
        _gameController = gameController;
    }

    private void OnCollisionEnter(Collision collision)
    {
        AddCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        AddCollision(collision);
        followFootwork.RegisterCollision();
        
        var midContact = collision.contacts[collision.contactCount / 2];
        Debug.DrawRay(midContact.point, collision.impulse, Color.yellow, 0.1f);
        
        if (collision.impulse.magnitude < impulseThreshold) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (
                contact.thisCollider.CompareTag(PhysicsEnvSettings.EpeeTipTag) 
                && contact.otherCollider.CompareTag(PhysicsEnvSettings.TargetAreaTag)
                && (contact.otherCollider.gameObject.layer == _otherBodyLayer
                    || contact.otherCollider.gameObject.layer == _otherWeaponLayer))
            {
                var hitAngle = Vector3.SignedAngle(transform.forward, contact.normal, transform.right);
                if (hitAngle < hitAngleThreshold && hitAngle > -hitAngleThreshold)
                {
                    _gameController.RegisterHit(_fencerColor);
                }
                break;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        RemoveCollision(collision.collider);
    }

    private void AddCollision(Collision collision)
    {
        _collisions[collision.collider] = collision;
    }

    private void RemoveCollision(Collider otherCollider)
    {
        _collisions.Remove(otherCollider);
    }

    public List<Collision> GetCollisionObservations()
    {
        return new List<Collision>(_collisions.Values);
    }

    public int GetOtherBodyLayer()
    {
        return _otherBodyLayer;
    }

    public int GetOtherWeaponLayer()
    {
        return _otherWeaponLayer;
    }

}
