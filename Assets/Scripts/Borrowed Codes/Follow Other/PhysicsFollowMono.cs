using UnityEngine;

namespace Playcraft
{
    /// Follow a target by applying physical forces.  
    /// Allows for more reliable rotational collision detection than Configurable Joints
    /// and more reliable translational collision detection than Articulation Bodies.
    public class PhysicsFollowMono : MonoBehaviour
    {
        [SerializeField] public PhysicsFollow process;
        void OnValidate() { process.OnValidate(); }

        void Start()
        {
            var energyLevel = GetComponent<EnergyController>();
            process.energyController = energyLevel;
            process.Start();
        }
        void FixedUpdate() { process.FixedUpdate(); }
        
    }
}
