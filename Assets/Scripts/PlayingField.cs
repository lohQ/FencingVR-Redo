using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayingField : MonoBehaviour
{
    public enum AgentPlayMode{
        Attack,
        Defense,
        AttackDefense,
        SelfPlay
    }

    public AgentPlayMode playMode;

    [Header("AgentPlayMode.Attack")]
    public Transform targetsParent;
    public GameObject originalTarget;
    public float xMaxOffset, yMinOffset, yMaxOffset, zMinOffset, zMaxOffset;
    public float xRotMax, zRotMax;
    public Transform upperArm;
    private List<GameObject> _targets;
    // set by curriculum
    private float _radiusRangeMin, _radiusRangeMax;
    private int _targetCountRangeMin, _targetCountRangeMax;

    [Header("AgentPlayMode.Defense")]
    public GameObject epeeForThrow;
    public float epeeXMaxOffset, epeeYMinOffset, epeeYMaxOffset;
    public List<Transform> agentBodyTargetPoints;
    public float epeeRoundDuration;
    public float epeeInterval;      // should be long enough for epee to touch body
    private bool _throwingEpees;
    // set by curriculum
    private float _epeeMinForce, _epeeMaxForce;
    private float _epeeOriginDistance;

    private void Start()
    {
        SetAttackModeParameters(0.6f, 0.8f, 1, 1);
        _targets = new List<GameObject>();
        for (int i = 0; i < _targetCountRangeMax; i++)
        {
            var newTarget = Instantiate(originalTarget, transform.position, Quaternion.identity, targetsParent);
            newTarget.SetActive(false);
            _targets.Add(newTarget);
        }

        SetDefenseModeParameters(1, 5, 3);
        epeeForThrow.SetActive(false);
        epeeForThrow.transform.position = transform.position - Vector3.up;
    }

    // public void StartPlaying()
    // {
    //     fencer.position = enGardePoint.position;
    //     foreach (var t in _targets)
    //     {
    //         t.SetActive(false);
    //     }
    //
    //     if (playMode == AgentPlayMode.Attack)
    //     {
    //         SetTargets();
    //     }
    // }

    public void SetAttackModeParameters(float minRadius, float maxRadius, int minTargetCount, int maxTargetCount)
    {
        _radiusRangeMin = minRadius;
        _radiusRangeMax = maxRadius;
        _targetCountRangeMin = minTargetCount;
        _targetCountRangeMax = maxTargetCount;
    }

    private void SetDefenseModeParameters(float minForce, float maxForce, float distance)
    {
        _epeeMinForce = minForce;
        _epeeMaxForce = maxForce;
        _epeeOriginDistance = distance;
    }

    private void SetTargets()
    {
        var count = Random.Range(_targetCountRangeMin, _targetCountRangeMax);
        var positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            var radius = Random.Range(_radiusRangeMin, _radiusRangeMax);
            _targets[i].transform.localScale = new Vector3(radius, 0.01f, radius);

            var positionTooNear = false;
            Vector3 position;
            do
            {
                var xPos = Random.Range(-xMaxOffset, xMaxOffset);
                var yPos = Random.Range(yMinOffset, yMaxOffset);
                var zPos = Random.Range(zMinOffset, zMaxOffset);
                position = transform.position + new Vector3(xPos, yPos, zPos);
                foreach (var existingPos in positions)
                {
                    if ((position - existingPos).magnitude < 0.2f)
                    {
                        positionTooNear = true;
                        Debug.Log("position " + position + " too near to " + existingPos + ", generate a new one");
                        break;
                    }
                }
            } while (positionTooNear);
            positions.Add(position);
            _targets[i].transform.position = position;

            var baseRotation = Quaternion.LookRotation(position - upperArm.position, Vector3.forward);
            var xRot = Random.Range(-xRotMax, xRotMax);
            var zRot = Random.Range(-zRotMax, zRotMax);
            var rotation = baseRotation * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(xRot, 0, zRot);
            _targets[i].transform.rotation = rotation;

            _targets[i].SetActive(true);
            // Debug.Log("created new target at position " + position + " with rotation " + rotation.eulerAngles 
            //           + " (base rotation: " + baseRotation.eulerAngles + ")");
        }
    }

    private IEnumerator ThrowEpees()
    {
        _throwingEpees = true;
        epeeForThrow.SetActive(true);
        var timeElapsed = 0f;
        var epeeIndex = 0;
        while (timeElapsed < epeeRoundDuration)
        {
            timeElapsed += Time.deltaTime;

            var epeeTargetPointPos = agentBodyTargetPoints[Random.Range(0, agentBodyTargetPoints.Count-1)].position;
            epeeTargetPointPos += Random.Range(-0.02f, 0.02f) * Vector3.up +
                                  Random.Range(-0.02f, 0.02f) * Vector3.right +
                                  Random.Range(-0.02f, 0.02f) * Vector3.forward;
            var forceToApply = Random.Range(_epeeMinForce, _epeeMaxForce);
            Debug.Log("forceToApply: " + forceToApply);

            var xPos = Random.Range(-epeeXMaxOffset, epeeXMaxOffset) + transform.position.x;
            var yPos = Random.Range(epeeYMinOffset, epeeYMaxOffset) + transform.position.y;
            var xDiff = xPos - epeeTargetPointPos.x;
            var yDiff = yPos - epeeTargetPointPos.y;
            var zDiff = (float) Math.Sqrt(_epeeOriginDistance * _epeeOriginDistance - xDiff * xDiff - yDiff * yDiff);
            var zPos = zDiff + epeeTargetPointPos.z;
            var originPosition = new Vector3(xPos, yPos, zPos);
            
            // teleporting rigidbody, so wait for one frame ba
            epeeForThrow.transform.position = originPosition;
            epeeForThrow.transform.LookAt(epeeTargetPointPos);
            var epeeRb = epeeForThrow.GetComponent<Rigidbody>();
            epeeRb.velocity = Vector3.zero;
            epeeRb.angularVelocity = Vector3.zero;
            yield return null;
            
            timeElapsed += Time.deltaTime;
            epeeRb.AddForce(forceToApply * -Vector3.forward, ForceMode.Impulse);
            
            var intervalTimeElapsed = 0f;
            while (intervalTimeElapsed < epeeInterval)
            {
                intervalTimeElapsed += Time.deltaTime;
                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }
        _throwingEpees = false;
    }

    private void Update()
    {
        // if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.Return))
        // {
        //     if (playMode == AgentPlayMode.Attack || playMode == AgentPlayMode.AttackDefense)
        //     {
        //         if (_targets[0].activeSelf)
        //         {
        //             foreach (var t in _targets)
        //             {
        //                 t.SetActive(false);
        //             }
        //         }
        //         else
        //         {
        //             SetTargets();
        //         }
        //     }
        //
        //     if (playMode == AgentPlayMode.Defense || playMode == AgentPlayMode.AttackDefense)
        //     {
        //         if (!_throwingEpees)
        //         {
        //             StartCoroutine(ThrowEpees());
        //         }
        //     }
        // }
    }

    public Transform fencer, enGardePoint;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
    }

    public void EnvironmentReset()
    {
        fencer.position = enGardePoint.position;
        foreach (var t in _targets)
        {
            t.SetActive(false);
        }

        if (playMode == AgentPlayMode.Attack)
        {
            SetTargets();
        }
    }

}
