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

    public void CollectObservations(VectorSensor sensor, BufferSensorComponent bufferSensor, int fencerNum)
    {
        Transform root;
        Transform selfWrist;
        Vector3 selfEpeeTipPos;
        List<Transform> targetOfSelf;
        Vector3 oppEpeeTipPos;
        List<Transform> targetOfOpp;
        Vector3 selfEpeePos;
        Vector3 oppEpeePos;
        EnergyController energyController;
        NewHitDetector hitDetector;
        Transform selfEpee;
        
        if (fencerNum == 1)
        {
            root = fencerOne;
            selfWrist = wristOne;
            selfEpeeTipPos = epeeTipOne.position;
            targetOfSelf = targetAreasOfOne;
            oppEpeeTipPos = epeeTipTwo.position;
            targetOfOpp = targetAreasOfTwo;
            selfEpeePos = epeeOne.position;
            oppEpeePos = epeeTwo.position;
            energyController = energyControllerOne;
            hitDetector = hitDetectorOne;
            selfEpee = epeeOne;
        }
        else
        {
            root = fencerTwo;
            selfWrist = wristTwo;
            selfEpeeTipPos = epeeTipTwo.position;
            targetOfSelf = targetAreasOfTwo;
            oppEpeeTipPos = epeeTipOne.position;
            targetOfOpp = targetAreasOfTwo;
            selfEpeePos = epeeTwo.position;
            oppEpeePos = epeeOne.position;
            energyController = energyControllerTwo;
            hitDetector = hitDetectorTwo;
            selfEpee = epeeTwo;
        }

        var wristFromFencer = root.InverseTransformPoint(selfWrist.position);
        normalizer.SaveMinMax(wristFromFencer, 0);
        sensor.AddObservation(normalizer.GetNormalized(wristFromFencer, 0));

        var normalizedWristRot = selfWrist.localRotation.eulerAngles / 180.0f - Vector3.one;  // [-1,1]
        sensor.AddObservation(normalizedWristRot);

        for (int i = 0; i < targetOfSelf.Count; i++)
        {
            var targetsFromFencer = root.InverseTransformPoint(targetOfSelf[i].position);
            normalizer.SaveMinMax(targetsFromFencer, 1);
            sensor.AddObservation(normalizer.GetNormalized(targetsFromFencer, 1));
        }

        var selfFromTip = root.InverseTransformPoint(oppEpeeTipPos);
        normalizer.SaveMinMax(selfFromTip, 1);
        sensor.AddObservation(normalizer.GetNormalized(selfFromTip, 1));

        // for (int i = 0; i < targetOfOpp.Count; i++)
        // {
        //     var targetFromTip = root.InverseTransformVector(oppEpeeTipPos - targetOfOpp[i].position);
        //     normalizer.SaveMinMax(targetFromTip, 1);
        //     sensor.AddObservation(normalizer.GetNormalized(targetFromTip, 1));
        // }

        var selfEpeeFromFencer = root.InverseTransformPoint(selfEpeePos);
        normalizer.SaveMinMax(selfEpeeFromFencer, 2);
        sensor.AddObservation(normalizer.GetNormalized(selfEpeeFromFencer, 2));

        var epeeTipFromEpee = root.InverseTransformVector(selfEpeeTipPos - selfEpeePos);
        normalizer.SaveMinMax(epeeTipFromEpee, 3);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));

        var oppEpeeFromFencer = root.InverseTransformPoint(oppEpeePos);
        normalizer.SaveMinMax(oppEpeeFromFencer, 4);
        sensor.AddObservation(normalizer.GetNormalized(oppEpeeFromFencer,4));

        epeeTipFromEpee = root.InverseTransformVector(oppEpeeTipPos - oppEpeePos);
        normalizer.SaveMinMax(epeeTipFromEpee, 3);
        sensor.AddObservation(normalizer.GetNormalized(epeeTipFromEpee, 3));

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
                normalizedImpulse.x, normalizedImpulse.y, normalizedImpulse.z
            });
            appendCount += 1;
        }
    }

    private void FixedUpdate()
    {
        var tipOnePos = epeeTipOne.position;
        var tipTwoPos = epeeTipTwo.position;
        
        _tipRaycastHitDistanceOne = TipRaycastHitDistance(
            tipOnePos, (tipOnePos - epeeOne.position), colorOne);
        _tipRaycastHitDistanceTwo = TipRaycastHitDistance(
            tipTwoPos, (tipTwoPos - epeeTwo.position), colorTwo);
        
        raycastTransformOne.position = tipOnePos;
        raycastTransformOne.rotation = epeeTipOne.rotation;
        raycastTransformTwo.position = tipTwoPos;
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