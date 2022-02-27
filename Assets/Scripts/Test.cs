using System.Collections;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Test : GameController
{
    public AvatarController controlledFencerAvatar;
    public HandEffectorController controlledHandEffectorController;
    private AgentFencer _agentFencer;
    private bool _started;

    private void Start()
    {
        _started = false;
    }
    
    public override void StartGame(AgentFencer agentFencer)
    {
        IEnumerator StartGameCor()
        {
            var a = StartCoroutine(agentFencer.avatarController.EnterEnGarde());
            var b = StartCoroutine(controlledFencerAvatar.EnterEnGarde());
            yield return a; yield return b;
            _started = true;
        }
        _agentFencer = agentFencer;
        StartCoroutine(StartGameCor());
    }

    public override bool Started()
    {
        return _started;
    }

    public override void AddObservations(VectorSensor sensor)
    {
        // throw new System.NotImplementedException();
    }

    public override void EndGame()
    {
        IEnumerator EndGameCor()
        {
            _started = false;
            var a = StartCoroutine(_agentFencer.avatarController.ExitEnGarde());
            var b = StartCoroutine(controlledFencerAvatar.ExitEnGarde());
            yield return a; yield return b;
            _agentFencer.EndEpisode();
        }
        StartCoroutine(EndGameCor());
    }
}
