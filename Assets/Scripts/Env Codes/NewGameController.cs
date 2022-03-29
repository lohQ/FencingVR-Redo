using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class NewGameController: MonoBehaviour
{
    public abstract void StartGame();
    public abstract bool Started();
    public abstract void EndGame();
    public abstract void RegisterHit(int fencerNum);
}
