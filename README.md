# FencingVR-Redo

## Intro to Project
Some vids here: https://youtube.com/playlist?list=PLg18vY_ub_zdp3iNF64-XTIYhi7Nt22nW

Not much, but it's honest work...
- Wrist translation 
  - In `Motion Control/FinalHandController`
  - Have a number of points to move to
  - Accelerate at first and get slower when nearer to target (like a log curve)
- Wrist rotation
  - In `Motion Control/FinalHandController`
  - Composed of palm rotation + point-to rotation
  - Palm rotation is also called suppination, the rotation along forward elbow axis
    - Determined by computing the angle between current orientation and a reference orientation
  - Point-to rotation
    - Applied on top of palm rotation, point to a target relative to the neutral wrist position, or a target on opponent agent's body
  - Rotation done in Coroutine and repeatedly rotate until reach target
  - Currently rotate with linear speed, perhaps can have acceleration like in translation
- Elbow rotation
  - In `Motion Control/FinalHandController`
  - Translates the hint position for the hand TwoBoneIK
- Bladework controller
  - In `Motion Control/BladeworkController`
  - For now it is just a proxy between the AgentFencer and the FinalHandController
  - Could be used to code higher-level bladework using wrist translation and rotation
- Record & Apply Footowrk Animation on EpeeTarget in FixedUpdate
  - In `Motion Control/FollowFootwork`
  - Can offset it so it applies the data earlier
  - Kinda reduce the impact of animation on the physical movement and make it smoother
- Energy controller
  - In `Motion Control/EnergyController`
  - Decrease energy when wrist translates, regenerate energy per time step, show the energy level on the green half-ring on the epee
  - Because running short of time and don't have time to debug more things, so not really used. Only consume energy when translation, ideally should consume energy also during rotation and perhaps also have a separate EnergyController for footwork
  - (idea borrowed from here: https://evanfletcher42.com/2018/12/29/sword-mechanics-for-vr/)
- Detect Hit
  - In `Env Codes/NewHitDetector`
  - Check the impulse magnitude and the hit angle
  - Registers the hit to the GameController so can end the game
- Reset Agent Position + Detect Out-of-Bound + Assign Reward
  - In `Env Codes/TrainingEnv`
  - Also do initialization, e.g., assigning target area on Agent2 to hit target of Agent1, vice versa 
- PD Controller
  - In `Borrowed Codes/FollowOther`
  - Script attached on `New Epee/Physical Pivot`, adjust the parameters to tweak the physics movement behavior of the epee
- Observer and Normalizer
  - In `Agent Codes/Observer` and `Agent Codes/Normalizer`
  - Normalizer is shared across the training envs, while Observer is associated with the GameController per training env
  - Normalizer can be used to record the min/max values, and then later read from it
    - Copy the values before ending the game, then end game and paste the values back
  - Observer is used by both agents to collect observations. This is because quite some observations require accessing attribute of the other agent, so if collect these observations in each agent, the agent will need to access many objects of the other agent, which could make it a bit messy. So better to have a shared Observer. 
- Agent
  - In `Agent Codes/NewAgentFencer`
  - Request decision, collection observation, and perform action
  - Also contains a Heuristic to control the agent's action manually
  - Per-timestep rewards are assigned here


## Models
Models are located in the Assets/Models directory, each associated with its own configuration file. `SPWithCurriculum` stores configs and results of experiment 3, `SPWithoutCurriculum` stores that of experiment 1 and 2. 

## Re-record Animation Joint Data
Sometimes when reload the scene the animation joint data stored in ScriptableObject will be gone. This data is used to smoothen the physical movements from animation. If you see the arm pulling back when step forward, or arm staying in front when step backward, then possibly this data is lost (see the scriptable objects in ScriptableObjects folder). To re-record it, disable avatar animation actions in `NewAgentFencer.OnActionReceived` function, and uncomment the commented codes in `FollowFootwork.FixedUpdate` function. Then press X for recording StepForward, LargeStepForward, StepBackward, LargeStepBackward, and press Y for recording Lunge + LungeRecover. Then check the ScriptableObject again to validate that the data is successfully recorded. 

## Training 1 & 2 <-> Training 3
As the observation space and reward functions of experiment 1 & 2 is different from experiment 3, some modifications need to be made before running the training / the trained model. 

Currently `TrainingEnv!!!(1)` is adjusted to experiment 1 and 2, while others are for experiment 3. Can look at the prefab to see what to change in the scene. 

For the reward function, go to `TrainingEnv` and change the commented codes in `FixedUpdate`.
- Experiment 1 and 2 do SetReward(0), and EndGame()
- Experiment 3 does AddReward(-0.5f), and StartCoroutine(ResetFencersPosition())
