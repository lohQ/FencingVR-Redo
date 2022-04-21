using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;


public class Observer : MonoBehaviour
{
    public Normalizer normalizer;

    [Serializable]
    public struct StuffsToObserve
    {
        // all observations are relative to this
        public Transform fencer;

        // observation 1: wrist position + wrist rotation
        public Transform wrist;
        
        // observation 2: fencer distance from targets
        public List<Transform> targetPoints;
        public Transform epeeTip;

        // observation 3: epee position and vector and velocity
        public Transform epee;
        [HideInInspector] public Rigidbody epeeRigidbody;

        // variable length observation: collisions on epee
        public NewHitDetector hitDetector;
    }
    
    
    public StuffsToObserve one;
    public StuffsToObserve two;
    // for reward calculation
    public float tipClosenessThreshold = 50;
    private float _tipRaycastHitDistanceOne;
    private float _tipRaycastHitDistanceTwo;

    
    private void Start()
    {
        one.epeeRigidbody = one.epee.GetComponent<Rigidbody>();
        two.epeeRigidbody = two.epee.GetComponent<Rigidbody>();
    }

    public void CollectObservations(VectorSensor sensor, BufferSensorComponent bufferSensor, int fencerNum)
    {
        StuffsToObserve self;
        StuffsToObserve opp;
        float oppTipRaycastHitDistance;
        
        // used by both observation and reward
        var tipOnePos = one.epeeTip.position;
        var tipTwoPos = two.epeeTip.position;
        _tipRaycastHitDistanceOne = TipRaycastHitDistance(
            tipOnePos, (tipOnePos - one.epee.position), 1);
        _tipRaycastHitDistanceTwo = TipRaycastHitDistance(
            tipTwoPos, (tipTwoPos - two.epee.position), 2);

        if (fencerNum == 1)
        {
            self = one;
            opp = two;
            oppTipRaycastHitDistance = _tipRaycastHitDistanceTwo;
        }
        else
        {
            self = two;
            opp = one;
            oppTipRaycastHitDistance = _tipRaycastHitDistanceOne;
        }
        
        var root = self.fencer;

        var wrist = self.wrist;
        var wristFromFencer = root.InverseTransformPoint(wrist.position);
        sensor.AddObservation(normalizer.GetNormalized(wristFromFencer, 0));
        var normalizedWristRot = wrist.localRotation.eulerAngles / 180.0f - Vector3.one;  // [-1,1]
        sensor.AddObservation(normalizedWristRot);

        foreach (var targetPoint in self.targetPoints)
        {
            var targetsFromFencer = root.InverseTransformPoint(targetPoint.position);
            sensor.AddObservation(normalizer.GetNormalized(targetsFromFencer, 1));
        }

        var selfFromTip = root.InverseTransformPoint(opp.epeeTip.position);
        sensor.AddObservation(normalizer.GetNormalized(selfFromTip, 1));

        var selfEpeeFromFencer = root.InverseTransformPoint(self.epee.position);
        sensor.AddObservation(normalizer.GetNormalized(selfEpeeFromFencer, 2));
        var epeeTipFromEpee = root.InverseTransformVector(self.epeeTip.position - self.epee.position);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));
        var selfEpeeVelocity = root.InverseTransformVector(self.epeeRigidbody.velocity);
        sensor.AddObservation(normalizer.GetNormalized(selfEpeeVelocity, 7));

        var oppEpeeFromFencer = root.InverseTransformPoint(opp.epee.position);
        sensor.AddObservation(normalizer.GetNormalized(oppEpeeFromFencer,4));
        epeeTipFromEpee = root.InverseTransformVector(opp.epeeTip.position - opp.epee.position);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));
        var oppEpeeVelocity = root.InverseTransformVector(opp.epeeRigidbody.velocity);
        sensor.AddObservation(normalizer.GetNormalized(oppEpeeVelocity, 7));

        if (oppTipRaycastHitDistance < 0)
        {
            sensor.AddObservation(-1);
        }
        else
        {
            sensor.AddObservation(oppTipRaycastHitDistance / tipClosenessThreshold);
        }

        var collisions = self.hitDetector.GetCollisionObservations();
        var maxAppendCount = bufferSensor.ObservableSize;
        var appendCount = 0;
        foreach (var collision in collisions)
        {
            if (appendCount > maxAppendCount) break;
            if (collision.contactCount == 0) continue;

            var midContact = collision.contacts[collision.contactCount / 2].point;
            var transformedContactPoint = self.epee.InverseTransformPoint(midContact);
            var transformedImpulse = self.epee.InverseTransformVector(collision.impulse);

            var normalizedContactPoint = normalizer.GetNormalizedCapped(transformedContactPoint, 5);
            var normalizedImpulse = normalizer.GetNormalizedCapped(transformedImpulse, 6);
            
            bufferSensor.AppendObservation(new []
            {
                normalizedContactPoint.x, normalizedContactPoint.y, normalizedContactPoint.z,
                normalizedImpulse.x, normalizedImpulse.y, normalizedImpulse.z,
                ColliderTypeToFloat(collision.collider, self.hitDetector)
            });
            appendCount += 1;
        }
    }

    private float ColliderTypeToFloat(Collider otherCollider, NewHitDetector hitDetector)
    {
        if (otherCollider.CompareTag(PhysicsEnvSettings.TargetAreaTag) 
            && otherCollider.gameObject.layer == hitDetector.GetOtherBodyLayer())
        {
            return 1f;
        }

        if (otherCollider.gameObject.layer == hitDetector.GetOtherWeaponLayer())
        {
            return 0.5f;
        }

        return 0f;
    }
    
    public float SelfTipRaycastHitDistance(int fencerNum)
    {
        return fencerNum == 1 ? _tipRaycastHitDistanceOne : _tipRaycastHitDistanceTwo;
    }

    public float OppTipRaycastHitDistance(int fencerNum)
    {
        return fencerNum == 1 ? _tipRaycastHitDistanceTwo : _tipRaycastHitDistanceOne;
    }

    private float TipRaycastHitDistance(Vector3 tipPos, Vector3 tipDir, int fencerNum)
    {
        var ray = new Ray(tipPos, tipDir.normalized);
        var oppNum = PhysicsEnvSettings.GetOther(fencerNum);
        var layerMask = LayerMask.GetMask(PhysicsEnvSettings.GetFencerBodyLayer(oppNum)) 
                        + LayerMask.GetMask(PhysicsEnvSettings.GetFencerWeaponLayer(oppNum));
        
        var hits = Physics.RaycastAll(ray, tipClosenessThreshold, layerMask);
        if (hits.Length != 0)
        {
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag(PhysicsEnvSettings.TargetAreaTag))
                {
                    return hit.distance;
                }
            }

            return -1;
        }
        return -1;
    }

}