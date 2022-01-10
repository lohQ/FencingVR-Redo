using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// TODO: within piste
// TODO: next is AgentFencer

public enum FencerColor
{
    Green = 0,
    Red = 1
}

public class Bout : MonoBehaviour
{
    public int boutPoints = 5;
    // in seconds
    public float boutTime = 180;
    private const float DoubleTouchTime = 0.04f;

    [Header("Piste Components")]
    public Transform[] startPoints;
    public TextMeshPro[] scoreBoards;
    public TextMeshPro timerBoard;
    public float zHalfLength = 8f;
    public float xHalfWidth = 1.2f;

    [Header("Fencers")]
    public Transform[] fencers;
    public AvatarController[] fencerControllers;
    public AgentFencer[] agentFencers;

    public int[] points;
    private float _boutRemainingTime;
    [HideInInspector]
    public bool withinRound;
    private float _doubleTouchRemainingTime;
    private bool _withinDoubleTimeframe;

    private const int Red = (int) FencerColor.Red;
    private const int Green = (int) FencerColor.Green;

    void Start()
    {
        // agentFencers[Green].MaxStep = (int) (boutTime / Time.fixedDeltaTime);
        // agentFencers[Red].MaxStep = (int) (boutTime / Time.fixedDeltaTime);

        points = new int[2];

        // StartCoroutine(StartRound());
    }

    private void Update()
    {
        if (_withinDoubleTimeframe)
        {
            if (_doubleTouchRemainingTime <= 0)
            {
                StartCoroutine(EndRound());
                return;
            }
            _doubleTouchRemainingTime -= Time.deltaTime;
        }

        if (withinRound)
        {
            var pistePos = transform.position;
            var redPos = fencers[Red].position;
            var greenPos = fencers[Green].position;
            if (redPos.x > pistePos.x + xHalfWidth 
                || redPos.x < pistePos.x - xHalfWidth
                || redPos.z > pistePos.z + zHalfLength 
                || redPos.z < pistePos.z - zHalfLength)
            {
                // Debug.Log("redFencer is outside of the piste, green wins!");
                // points[Green] += 1;
                // scoreBoards[Green].SetText(points[Green] + "");
                agentFencers[Red].AddReward(-1);
                StartCoroutine(EndRound());
                return;
            }
            if (greenPos.x > pistePos.x + xHalfWidth
                || greenPos.x < pistePos.x - xHalfWidth
                || greenPos.z > pistePos.z + zHalfLength
                || greenPos.z < pistePos.z - zHalfLength)
            {
                // Debug.Log("greenFencer is outside of the piste, red wins!");
                // points[Red] += 1;
                // scoreBoards[Red].SetText(points[Red] + "");
                agentFencers[Green].AddReward(-1);
                StartCoroutine(EndRound());
                return;
            }

            _boutRemainingTime -= Time.deltaTime;
            timerBoard.SetText($"{_boutRemainingTime:0.00}");
        }
        
        if (_boutRemainingTime <= 0)
        {
            StartCoroutine(EndRound());
        }
    }

    public void RegisterHit(FencerColor fencerColor)
    {
        points[(int)fencerColor] += 1;
        scoreBoards[(int)fencerColor].SetText(points[(int)fencerColor] + "");
        if (fencerColor == FencerColor.Green)
        {
            agentFencers[Green].AddReward(1);
            agentFencers[Red].AddReward(-1);
        }
        else
        {
            agentFencers[Red].AddReward(1);
            agentFencers[Green].AddReward(-1);
        }

        if (_withinDoubleTimeframe)
        {
            StartCoroutine(EndRound());
        }
        else
        {
            _withinDoubleTimeframe = true;
            _doubleTouchRemainingTime = DoubleTouchTime;
        }
    }

    private int _startSignal = 0;

    public void SignalStartRound()
    {
        // used by agent in OnEpisodeBegin to start the first round
        _startSignal += 1;
        // Debug.Log("_startSignal += 1: " + _startSignal);
        if (_startSignal == 2)
        {
            _startSignal = 0;
            points[Green] = 0;
            points[Red] = 0;
            scoreBoards[Green].SetText(points[Green] + "");
            scoreBoards[Red].SetText(points[Red] + "");
            _boutRemainingTime = boutTime;
            _withinDoubleTimeframe = false;
            StartCoroutine(StartRound());
        }
    }
    
    IEnumerator StartRound()
    {
        // used in every round to reset points, time, position, rotation, etc.
        fencers[Green].position = new Vector3(startPoints[Green].position.x, 0, startPoints[Green].position.z);
        fencers[Green].rotation = Quaternion.identity;
        fencers[Red].position = new Vector3(startPoints[Red].position.x, 0, startPoints[Red].position.z);
        fencers[Red].rotation = Quaternion.Euler(0, 180, 0) * Quaternion.identity;

        yield return new WaitForSeconds(0.1f);
        var redEnter = StartCoroutine(fencerControllers[Red].EnterEnGarde());
        var greenEnter = StartCoroutine(fencerControllers[Green].EnterEnGarde());
        yield return redEnter;
        yield return greenEnter;

        withinRound = true;
    }
    
    IEnumerator EndRound()
    {
        withinRound = false;
        _withinDoubleTimeframe = false;

        var redExit = StartCoroutine(fencerControllers[Red].ExitEnGarde());
        var greenExit = StartCoroutine(fencerControllers[Green].ExitEnGarde());
        yield return redExit;
        yield return greenExit;

        if (_boutRemainingTime <= 0)
        {
            EndBout();
        } else if (points[Green] == boutPoints || points[Red] == boutPoints)
        {
            if (points[Green] != points[Red])
            {
                EndBout();
            }
            else
            {
                points[Green] -= 1;
                points[Red] -= 1;
                scoreBoards[Green].SetText(points[Green] + "");
                scoreBoards[Red].SetText(points[Red] + "");
                yield return StartCoroutine(StartRound());
            }
        }
        else
        {
            yield return StartCoroutine(StartRound());
        }
    }

    void EndBout()
    {
        withinRound = false;
        _withinDoubleTimeframe = false;
        if (points[Green] > points[Red])
        {
            agentFencers[Green].AddReward(1);
        }
        else if (points[Red] > points[Green])
        {
            agentFencers[Red].AddReward(1);
        }
        agentFencers[Green].EndEpisode();
        agentFencers[Red].EndEpisode();
    }
    
}
