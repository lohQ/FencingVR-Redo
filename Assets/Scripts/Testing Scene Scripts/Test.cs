using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Vector3[] epee0FollowPositions;
    public Vector3[] epee0FollowRotations;
    public Vector3[] epee1FollowPositions;
    public Vector3[] epee1FollowRotations;

    public float[] durations;
    
    public Transform epee0Follow;
    public Transform epee1Follow;
    public Transform epee0;
    public Transform epee1;
    
    // public bool failIfNotFollowed;
    // public float missTolerance;

    public GameObject successIndicator;
    public GameObject failureIndicator;

    private bool _started;
    
    void Start()
    {
        successIndicator.SetActive(false);
        failureIndicator.SetActive(false);

        if (epee0FollowPositions.Length != epee0FollowRotations.Length
            || epee1FollowPositions.Length != epee1FollowRotations.Length
            || epee0FollowPositions.Length != epee1FollowPositions.Length
            || durations.Length != epee0FollowPositions.Length)
        {
            Debug.Log("The length of the lists must be equal!");
        }
        else
        {
            _started = true;
            StartCoroutine(TestCollision());
        }

    }

    public float missTolerance = 0.02f;
    public float withCollisionMissTolerance = 0.05f;

    IEnumerator TestCollision()
    {
        var numTransform = epee0FollowPositions.Length;
        for (int i = 0; i < numTransform; i += 1)
        {
            float timeElapsed = 0f;
            Vector3 epee0StartPosition = epee0.position;
            Quaternion epee0StartRotation = epee0.rotation;
            Vector3 epee1StartPosition = epee1.position;
            Quaternion epee1StartRotation = epee1.rotation;
            while (timeElapsed <= durations[i])
            {
                timeElapsed += Time.deltaTime;
                var t = timeElapsed / durations[i];
                epee0Follow.position = Vector3.Lerp(epee0StartPosition, epee0FollowPositions[i], t);
                epee0Follow.rotation = Quaternion.Lerp(epee0StartRotation, Quaternion.Euler(epee0FollowRotations[i]), t);
                epee1Follow.position = Vector3.Lerp(epee1StartPosition, epee1FollowPositions[i], t);
                epee1Follow.rotation = Quaternion.Lerp(epee1StartRotation, Quaternion.Euler(epee1FollowRotations[i]), t);
                yield return new WaitForFixedUpdate();
            }

            // wait for epee to reach epeeFollow
            yield return new WaitForFixedUpdate();

            if ((epee0.position - epee0Follow.position).magnitude > withCollisionMissTolerance)
            {
                failureIndicator.SetActive(true);
                Debug.Log("epee 0 failed to reach position " + i);
                Debug.Log("magnitude difference is: " + (epee0.position - epee0Follow.position).magnitude);
                yield break;
            }

            // if (Quaternion.Angle(epee0.rotation, epee0Follow.rotation) > 0.2f)
            // {
            //     failureIndicator.SetActive(true);
            //     Debug.Log("epee 0 failed to reach rotation " + i);
            //     Debug.Log("angle between current and desired rotation is: " + Quaternion.Angle(epee0.rotation, epee0Follow.rotation));
            //     yield break;
            // }
            
            if ((epee1.position - epee1Follow.position).magnitude > withCollisionMissTolerance)
            {
                failureIndicator.SetActive(true);
                Debug.Log("epee 1 failed to reach position " + i);
                Debug.Log("magnitude difference is: " + (epee1.position - epee1Follow.position).magnitude);
                yield break;
            }
            
            // 

            // if (Quaternion.Angle(epee1.rotation, epee1Follow.rotation) > 0.2f)
            // {
            //     failureIndicator.SetActive(true);
            //     Debug.Log("epee 1 failed to reach rotation " + i);
            //     yield break;
            // }
        }
        
        successIndicator.SetActive(true);
    }

    public Vector3 cubeSize = new Vector3(0.05f, 0.05f, 0.05f);
    
    private void OnDrawGizmos()
    {
        if (_started)
        {
            var numTransform = epee0FollowPositions.Length;
            for (int i = 0; i < numTransform; i += 1)
            {
                Gizmos.DrawCube(epee0FollowPositions[i], cubeSize);
                Gizmos.DrawCube(epee1FollowPositions[i], cubeSize);
            }
        }
    }
}
