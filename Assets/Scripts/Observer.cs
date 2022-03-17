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

    // observation 2: tip distance from targets
    public float maxTargetLocalDistance;
    public Transform epeeTipOne;
    public List<Transform> targetAreasOfOne;
    public Transform epeeTipTwo;
    public List<Transform> targetAreasOfTwo;
    // for reward calculation
    private float _minDistanceTipOne;
    private float _minDistanceTipTwo;
    
    // observation 3: epee position and vector
    public Transform epeeOne;
    public Transform epeeTwo;
    
    // observation 4: energy level
    public EnergyController energyControllerOne;
    public EnergyController energyControllerTwo;

    // variable length observation: collisions on epee
    public NewHitDetector hitDetectorOne;
    public NewHitDetector hitDetectorTwo;


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
            var targetFromTip = root.InverseTransformVector(selfEpeeTipPos - targetOfSelf[i].position);
            normalizer.SaveMinMax(targetFromTip, 1);
            sensor.AddObservation(normalizer.GetNormalized(targetFromTip, 1));
        }

        for (int i = 0; i < targetOfOpp.Count; i++)
        {
            var targetFromTip = root.InverseTransformVector(oppEpeeTipPos - targetOfOpp[i].position);
            normalizer.SaveMinMax(targetFromTip, 1);
            sensor.AddObservation(normalizer.GetNormalized(targetFromTip, 1));
        }

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

        sensor.AddObservation(energyController.value);

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
            normalizer.SaveMinMax(transformedContactPoint, 5);
            normalizer.SaveMinMax(transformedContactPoint, 6);

            var normalizedContactPoint = normalizer.GetNormalized(transformedContactPoint, 5);
            var normalizedImpulse = normalizer.GetNormalized(transformedImpulse, 6);

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
        _minDistanceTipOne = MinTipToTargetDistance(epeeTipOne.position, targetAreasOfOne);
        _minDistanceTipTwo = MinTipToTargetDistance(epeeTipTwo.position, targetAreasOfTwo);
    }

    public float SelfMinTipDistance(int fencerNum)
    {
        return fencerNum == 1 ? _minDistanceTipOne : _minDistanceTipTwo;
    }

    public float OppMinTipDistance(int fencerNum)
    {
        return fencerNum == 1 ? _minDistanceTipTwo : _minDistanceTipOne;
    }

    private float MinTipToTargetDistance(Vector3 tipPos, List<Transform> targets)
    {
        var minDistance = Mathf.Infinity;
        for (int i = 0; i < targets.Count; i++)
        {
            var distance = (tipPos - targets[i].position).magnitude;
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance;
    }
}