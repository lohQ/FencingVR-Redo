using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class GameController: MonoBehaviour
{
    public abstract void StartGame(AgentFencer agentFencer);
    public abstract bool Started();
    public abstract void AddObservations(VectorSensor sensor);   // add environment observations
    public abstract void EndGame();
}
