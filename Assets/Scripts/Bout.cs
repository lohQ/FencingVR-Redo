using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public enum FencerColor
{
    Green = 0,
    Red = 1
}

public class Bout : MonoBehaviour
{
    public int boutPoints = 5;
    public float boutTime = 180;    // in seconds
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

    [HideInInspector]
    public int[] points;
    [HideInInspector]
    public bool withinRound;

    private int _startSignal;
    private float _boutRemainingTime;
    private float _doubleTouchRemainingTime;
    private int _withinDoubleTimeframeOf;   // if not within double timeframe, 0; else value will be Red/Green + 1
    private bool _redZMinusGreenZIsPositive;

    private const int Red = (int) FencerColor.Red;
    private const int Green = (int) FencerColor.Green;

    public float GetRemainingTime()
    {
        return _boutRemainingTime;
    }
    
    void Start()
    {
        points = new int[2];
        _startSignal = 0;
        withinRound = false;
        _withinDoubleTimeframeOf = 0;
        _redZMinusGreenZIsPositive = (fencers[Red].position.z - fencers[Green].position.z) > 0;
    }

    private void Update()
    {
        if (withinRound)
        {
            bool roundEnded = _boutRemainingTime <= 0;

            // make sure both in piste
            if (!roundEnded)
            {
                // var pistePos = transform.position;
                var redPos = fencers[Red].position;
                var greenPos = fencers[Green].position;
                //
                // if (redPos.x > pistePos.x + xHalfWidth 
                //     || redPos.x < pistePos.x - xHalfWidth
                //     || redPos.z > pistePos.z + zHalfLength 
                //     || redPos.z < pistePos.z - zHalfLength)
                // {
                //     agentFencers[Red].AddReward(-1);
                //     points[Green] += 1;
                //     scoreBoards[Green].SetText(points[Green] + "");
                //     roundEnded = true;
                // }
                // if (greenPos.x > pistePos.x + xHalfWidth
                //     || greenPos.x < pistePos.x - xHalfWidth
                //     || greenPos.z > pistePos.z + zHalfLength
                //     || greenPos.z < pistePos.z - zHalfLength)
                // {
                //     agentFencers[Green].AddReward(-1);
                //     points[Red] += 1;
                //     scoreBoards[Red].SetText(points[Red] + "");
                //     roundEnded = true;
                // }

                if ((redPos.z - greenPos.z) > 0 != _redZMinusGreenZIsPositive)
                {
                    // fencers are back to back now, have to reset position
                    roundEnded = true;
                }
            }
            
            // if in double timeframe then also decrement the time
            if (!roundEnded && _withinDoubleTimeframeOf != 0)
            {
                if (_doubleTouchRemainingTime <= 0)
                {
                    _withinDoubleTimeframeOf = 0;
                    roundEnded = true;
                }
                else
                {
                    _doubleTouchRemainingTime -= Time.deltaTime;
                }
            }

            if (roundEnded)
            {
                withinRound = false;
                StartCoroutine(EndRound());
            }
            else
            {
                _boutRemainingTime -= Time.deltaTime;
                timerBoard.SetText($"{_boutRemainingTime:0.00}");
            }
        }
        
    }
    
    public void SignalStartRound()
    {
        if (withinRound) return;

        // used by agent in OnEpisodeBegin to start the first round
        _startSignal += 1;

        if (_startSignal == 2)
        {
            _startSignal = 0;
            points[Green] = 0;
            points[Red] = 0;
            scoreBoards[Green].SetText(points[Green] + "");
            scoreBoards[Red].SetText(points[Red] + "");
            _boutRemainingTime = boutTime;
            StartCoroutine(StartRound());
        }
    }
    
    // quick patch to incorrect start rotation issue
    private bool _startingRound = false;

    public float randomXScale = 10f;
    public float randomZScale = 10f;
    
    IEnumerator StartRound()
    {
        if (_startingRound) yield break;

        _startingRound = true;
        // used in every round to reset points, time, position, rotation, etc.
        fencers[Green].position = new Vector3(
            Random.value * randomXScale + startPoints[Green].position.x, 
            0, 
            Random.value * randomZScale + startPoints[Green].position.z);
        fencers[Red].position = new Vector3(
            Random.value * randomXScale + startPoints[Red].position.x, 
            0, 
            Random.value * randomZScale + startPoints[Red].position.z);
        fencers[Green].rotation = Quaternion.identity;
        fencers[Red].rotation = Quaternion.Euler(0, 180, 0) * Quaternion.identity;

        // yield return new WaitForSeconds(0.1f);
        var redEnter = StartCoroutine(fencerControllers[Red].EnterEnGarde());
        var greenEnter = StartCoroutine(fencerControllers[Green].EnterEnGarde());
        yield return redEnter;
        yield return greenEnter;

        yield return new WaitForSeconds(0.1f);
        if (Quaternion.Angle(fencers[Green].rotation, Quaternion.identity) < 1f)
        {
            Debug.Log("Detected incorrect start rotation! Manually exit en garde again");
            var redExit = StartCoroutine(fencerControllers[Red].ExitEnGarde());
            var greenExit = StartCoroutine(fencerControllers[Green].ExitEnGarde());
            yield return redExit;
            yield return greenExit;
        
            Debug.Log("restart enter en garde");
            fencers[Green].position = new Vector3(startPoints[Green].position.x, 0, startPoints[Green].position.z);
            fencers[Green].rotation = Quaternion.identity;
            fencers[Red].position = new Vector3(startPoints[Red].position.x, 0, startPoints[Red].position.z);
            fencers[Red].rotation = Quaternion.Euler(0, 180, 0) * Quaternion.identity;
        
            yield return new WaitForSeconds(0.1f);
            redEnter = StartCoroutine(fencerControllers[Red].EnterEnGarde());
            greenEnter = StartCoroutine(fencerControllers[Green].EnterEnGarde());
            yield return redEnter;
            yield return greenEnter;
        }

        withinRound = true;
        _startingRound = false;
    }
    
    IEnumerator EndRound()
    {
        // withinRound = false;
        // _withinDoubleTimeframeOf = 0;

        // yield return new WaitForSeconds(0.1f);
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
        // withinRound = false;
        // _withinDoubleTimeframeOf = 0;
        if (points[Green] > points[Red])
        {
            agentFencers[Green].SetReward(1);
            agentFencers[Red].SetReward(-1);
        }
        else if (points[Red] > points[Green])
        {
            agentFencers[Red].SetReward(1);
            agentFencers[Green].SetReward(-1);
        }
        else
        {
            agentFencers[Red].SetReward(0);
            agentFencers[Green].SetReward(0);
        }
        agentFencers[Green].EndEpisode();
        agentFencers[Red].EndEpisode();
    }
    
    public void RegisterHit(FencerColor fencerColor, Vector3 position, Vector3 normal)
    {
        if (!withinRound) return;
        
        // if is the hit from same fencer who started this double timeframe, ignore
        if ((int)fencerColor == (_withinDoubleTimeframeOf - 1)) return;
        
        if (fencerColor == FencerColor.Green)
        {
            points[Green] += 1;
            scoreBoards[Green].SetText(points[Green] + "");
            agentFencers[Green].AddReward(1);
            agentFencers[Red].AddReward(-1);
        }
        else
        {
            points[Red] += 1;
            scoreBoards[Red].SetText(points[Red] + "");
            agentFencers[Red].AddReward(1);
            agentFencers[Green].AddReward(-1);
        }

        if (_withinDoubleTimeframeOf != 0)
        {
            withinRound = false;
            _withinDoubleTimeframeOf = 0;
            StartCoroutine(EndRound());
        }
        else
        {
            _withinDoubleTimeframeOf = (int)fencerColor + 1;
            _doubleTouchRemainingTime = DoubleTouchTime;
        }
    }

}
