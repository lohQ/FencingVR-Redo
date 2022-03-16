using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class NewGameController: MonoBehaviour
{
    public abstract void StartGame(NewAgentFencer agentFencer);
    public abstract bool Started();
    public abstract void AddObservations(VectorSensor sensor);   // add environment observations
    public abstract void EndGame();
}
