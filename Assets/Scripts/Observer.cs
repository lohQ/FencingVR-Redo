using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;


public class Observer : MonoBehaviour
{
    public Normalizer normalizer;
    
    // observation 1: wrist position + wrist rotation
    public Transform fencerOne;
    public Transform wristOne;
    public Transform fencerTwo;
    public Transform wristTwo;

    // observation 2: fencer distance from targets
    public Transform epeeTipOne;
    public List<Transform> targetAreasOfOne;
    public Transform epeeTipTwo;
    public List<Transform> targetAreasOfTwo;
    // for reward calculation
    [HideInInspector]
    public FencerColor colorOne;
    [HideInInspector]
    public FencerColor colorTwo;
    public float tipClosenessThreshold = 50;
    private float _tipRaycastHitDistanceOne;
    private float _tipRaycastHitDistanceTwo;
    
    // observation 3: epee position and vector
    public Transform epeeOne;
    public Transform epeeTwo;
    
    // observation 4: energy level
    public EnergyController energyControllerOne;
    public EnergyController energyControllerTwo;

    // variable length observation: collisions on epee
    public NewHitDetector hitDetectorOne;
    public NewHitDetector hitDetectorTwo;

    // raycast observation
    [HideInInspector]
    public Transform raycastTransformOne;
    [HideInInspector]
    public Transform raycastTransformTwo;
    
    // only used to get center moveTo position
    private FinalHandController _handControllerOne;
    private FinalHandController _handControllerTwo;

    private Rigidbody _epeeOneRb;
    private Rigidbody _epeeTwoRb;

    private void Start()
    {
        _handControllerOne = fencerOne.GetComponent<FinalHandController>();
        _handControllerTwo = fencerTwo.GetComponent<FinalHandController>();
        _epeeOneRb = epeeOne.GetComponent<Rigidbody>();
        _epeeTwoRb = epeeTwo.GetComponent<Rigidbody>();
    }

    public void CollectObservations(VectorSensor sensor, BufferSensorComponent bufferSensor, int fencerNum)
    {
        Transform root;
        Transform selfWrist;
        Vector3 selfEpeeTipPos;
        List<Transform> targetOfSelf;
        Vector3 oppEpeeTipPos;
        Vector3 selfEpeePos;
        Vector3 selfEpeeVel;
        Vector3 oppEpeePos;
        Vector3 oppEpeeVel;
        NewHitDetector hitDetector;
        Transform selfEpee;
        float oppTipRaycastHitDistance;
        FinalHandController handController;
        
        // used by both observation and reward
        var tipOnePos = epeeTipOne.position;
        var tipTwoPos = epeeTipTwo.position;
        _tipRaycastHitDistanceOne = TipRaycastHitDistance(
            tipOnePos, (tipOnePos - epeeOne.position), colorOne);
        _tipRaycastHitDistanceTwo = TipRaycastHitDistance(
            tipTwoPos, (tipTwoPos - epeeTwo.position), colorTwo);
        
        if (fencerNum == 1)
        {
            root = fencerOne;
            selfWrist = wristOne;
            selfEpeeTipPos = epeeTipOne.position;
            targetOfSelf = targetAreasOfOne;
            oppEpeeTipPos = epeeTipTwo.position;
            selfEpeePos = epeeOne.position;
            selfEpeeVel = _epeeOneRb.velocity;
            oppEpeePos = epeeTwo.position;
            oppEpeeVel = _epeeTwoRb.velocity;
            hitDetector = hitDetectorOne;
            selfEpee = epeeOne;
            oppTipRaycastHitDistance = _tipRaycastHitDistanceTwo;
            handController = _handControllerOne;
        }
        else
        {
            root = fencerTwo;
            selfWrist = wristTwo;
            selfEpeeTipPos = epeeTipTwo.position;
            targetOfSelf = targetAreasOfTwo;
            oppEpeeTipPos = epeeTipOne.position;
            selfEpeePos = epeeTwo.position;
            selfEpeeVel = _epeeTwoRb.velocity;
            oppEpeePos = epeeOne.position;
            oppEpeeVel = _epeeOneRb.velocity;
            hitDetector = hitDetectorTwo;
            selfEpee = epeeTwo;
            oppTipRaycastHitDistance = _tipRaycastHitDistanceOne;
            handController = _handControllerTwo;
        }

        var wristFromFencer = root.InverseTransformPoint(selfWrist.position);
        sensor.AddObservation(normalizer.GetNormalized(wristFromFencer, 0));

        var normalizedWristRot = selfWrist.localRotation.eulerAngles / 180.0f - Vector3.one;  // [-1,1]
        sensor.AddObservation(normalizedWristRot);

        for (int i = 0; i < targetOfSelf.Count; i++)
        {
            var targetsFromFencer = root.InverseTransformPoint(targetOfSelf[i].position);
            sensor.AddObservation(normalizer.GetNormalized(targetsFromFencer, 1));
        }

        var selfFromTip = root.InverseTransformPoint(oppEpeeTipPos);
        sensor.AddObservation(normalizer.GetNormalized(selfFromTip, 1));

        var selfEpeeFromFencer = root.InverseTransformPoint(selfEpeePos);
        sensor.AddObservation(normalizer.GetNormalized(selfEpeeFromFencer, 2));
        var epeeTipFromEpee = root.InverseTransformVector(selfEpeeTipPos - selfEpeePos);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));
        var selfEpeeVelocity = root.InverseTransformVector(selfEpeeVel);
        sensor.AddObservation(normalizer.GetNormalized(selfEpeeVelocity, 7));

        var oppEpeeFromFencer = root.InverseTransformPoint(oppEpeePos);
        sensor.AddObservation(normalizer.GetNormalized(oppEpeeFromFencer,4));
        epeeTipFromEpee = root.InverseTransformVector(oppEpeeTipPos - oppEpeePos);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));
        var oppEpeeVelocity = root.InverseTransformVector(oppEpeeVel);
        sensor.AddObservation(normalizer.GetNormalized(oppEpeeVelocity, 7));

        if (oppTipRaycastHitDistance < 0)
        {
            sensor.AddObservation(-1);
        }
        else
        {
            sensor.AddObservation(oppTipRaycastHitDistance / tipClosenessThreshold);
        }

        // // commented out for self-play training rerun
        // // (selfCenter - oppEpeeMid) should be bounded by (self - oppEpeeTip)
        // var parryVector = CenterMoveTargetToOppEpee(handController, oppEpeePos, oppEpeeTipPos);
        // var transformedParryVector = root.InverseTransformVector(parryVector);
        // normalizer.SaveMinMax(transformedParryVector, 1);
        // sensor.AddObservation(normalizer.GetNormalized(transformedParryVector, 1));
        
        // sensor.AddObservation(energyController.value);

        var collisions = hitDetector.GetCollisionObservations();
        var maxAppendCount = bufferSensor.ObservableSize;
        var appendCount = 0;
        foreach (var collision in collisions)
        {
            if (appendCount > maxAppendCount) break;
            if (collision.contactCount == 0) continue;

            var midContact = collision.contacts[collision.contactCount / 2].point;
            var transformedContactPoint = selfEpee.InverseTransformPoint(midContact);
            var transformedImpulse = selfEpee.InverseTransformVector(collision.impulse);

            var normalizedContactPoint = normalizer.GetNormalizedCapped(transformedContactPoint, 5);
            var normalizedImpulse = normalizer.GetNormalizedCapped(transformedImpulse, 6);
            
            bufferSensor.AppendObservation(new []
            {
                normalizedContactPoint.x, normalizedContactPoint.y, normalizedContactPoint.z,
                normalizedImpulse.x, normalizedImpulse.y, normalizedImpulse.z,
                ColliderTypeToFloat(collision.collider, hitDetector)
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

    private Vector3 CenterMoveTargetToOppEpee(FinalHandController handController, Vector3 oppEpeePos, Vector3 oppEpeeTipPos)
    {
        var centerMoveTo = handController.GetMoveToTargetPosition(1, 0, 0, false);
        var oppEpee_2Over3 = oppEpeePos + (oppEpeeTipPos - oppEpeePos) * 2 / 3;
        Debug.DrawLine(centerMoveTo, oppEpee_2Over3, Color.green);
        return (oppEpee_2Over3 - centerMoveTo);
    }

    private void FixedUpdate()
    {
        raycastTransformOne.position = epeeTipOne.position;
        raycastTransformOne.rotation = epeeTipOne.rotation;
        raycastTransformTwo.position = epeeTipTwo.position;
        raycastTransformTwo.rotation = epeeTipTwo.rotation;
    }

    public float SelfTipRaycastHitDistance(int fencerNum)
    {
        return fencerNum == 1 ? _tipRaycastHitDistanceOne : _tipRaycastHitDistanceTwo;
    }

    public float OppTipRaycastHitDistance(int fencerNum)
    {
        return fencerNum == 1 ? _tipRaycastHitDistanceTwo : _tipRaycastHitDistanceOne;
    }

    private float TipRaycastHitDistance(Vector3 tipPos, Vector3 tipDir, FencerColor selfColor)
    {
        var ray = new Ray(tipPos, tipDir.normalized);
        var oppColor = PhysicsEnvSettings.GetOther(selfColor);
        var layerMask = LayerMask.GetMask(PhysicsEnvSettings.GetFencerBodyLayer(oppColor)) 
                        + LayerMask.GetMask(PhysicsEnvSettings.GetFencerWeaponLayer(oppColor));
        
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