using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using Box = Boxes2.Box2;
using Boxes2;
using static AgentDetect2;
using static BoxDetect2;
using static BinDetect2;
public class PackerHand : Agent

{
    public GameObject ground;

    public GameObject binArea;


    //cache on initilization
    Rigidbody m_Agent; 

    [HideInInspector]
    public Transform carriedObject;

    [HideInInspector]
    public Transform target; //Target the agent will walk towards during training.


    public PickupScript2 pickupScript;


    EnvironmentParameters m_ResetParams;
    BoxSpawner2 m_Box;

    public override void Initialize()
    {

        m_Box = GetComponentInChildren<BoxSpawner2>();

        Debug.Log("++++++++++++++++++++BOX in INITIALIZE++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);

        
        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        // uncomment when pickupScript2 class is created
        // // Initialize PickupScript
        pickupScript = new PickupScript2();

        //Create boxes
        m_Box.SetUpBoxes();
        
        //Setup AgentDetect that detects when the agent goes inside the bin
        AgentDetect2 agentDetect = this.GetComponent<AgentDetect2>();
        agentDetect.agent = this; 

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // Reset agent and rewards
        SetResetParameters();
    }


    public override void OnEpisodeBegin()
    {

        //m_Box = GetComponentInChildren<BoxSpawner>();
        Debug.Log("++++++++++++++++++++BOX in ONEPISODEBEGIN++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);

        //Reset boxes
        foreach (var box in m_Box.boxPool) {
            box.ResetBoxes(box);
        }

        //Update target and orientation
        Debug.Log("++++++++++++++++++++BOX POOL COUNT++++++++++++++++++++++++++++++++++++++++++");
        Debug.Log(m_Box.boxPool.Count);


        //Reset agent and rewards
        SetResetParameters();

    }


    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
    
        // Bin position
        sensor.AddObservation(binArea.transform.position); //(x, y, z)

        //Box size and position
        foreach (var box in m_Box.boxPool) {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            sensor.AddObservation(box.rb.transform.position); //add box position to sensor observations
        }

        //Agent postiion
        sensor.AddObservation(this.transform.position);

        //Agent velocity
        sensor.AddObservation(m_Agent.velocity.x);
        sensor.AddObservation(m_Agent.velocity.z);

    }

    // dBranch1 = target: select target branch
    // dBranch2 = transform z-axis: move agent z-axis branch
    // dBranch3 = transform x-axis: move agent x-axis branch
    // dBranch4 = transform rotate: rotate agent branch
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        var j = -1;
        //var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        //var continuousActions = actionBuffers.ContinuousActions;
    
        SelectTarget(discreteActions[++j]); 
        // ActionMoveAgent(discreteActions[++j]); // passign ActionSegment instead of int
        ActionMoveAgent(actionBuffers.DiscreteActions); // passign ActionSegment instead of int
        // ActionMoveAgent(discreteActions[++j]); // should pass ActionSegment instead of int

        ////////////////SelectPosition:

        //this.transform.position.Set(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]);
        ///m_Agent.AddForce(new Vector3(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]));
        
        // current - past // Reward
        // current > past // good +Reward
        // current < past // bad -Reward

        // Reward Layers
            // layerX = X + denseX
            // layer1 = 0 + dense1   0.1   0.08  0.14 0.43    1
            // layer2 = 1 + dense2   1.1   1.08  1.14 1.43  >  2
            // layer3 = 2 + dense3   2.1   2.08  2.14 2.43  >  3 

        // Reward Layer 2: MacrostepSparseMilestoneCheckpointEvolution=dropoffbox() MicrostepDenseGradientPathguide=distancetobin
            // if agent has pickedup a box
            // if (target) {
                // SetReward(RewardLayer2()); // vs. refactor as RewardLayer2() containing SetReward(y)
            // }
        // Reward Layer 1: MacrostepSparseMilestoneCheckpointEvolution=pickupbox() MicrostepDenseGradientPathguide=distancetobox
            // if agents hasnt picked up a box
            if (target!=null) {
                // Assign Reward Layer 1
                // AddReward(x);
                // SetReward(x);
                SetReward(RewardLayer1()); // vs. refactor as RewardLayer1() containing SetReward(x)
            }
            
            // // can also try this reward function
            // AddReward(-1f / MaxStep);
    }

    // returns reward amount for layer 2
    public float RewardLayer2() {
        // distance between target (box) and goalarea (bin)
        float distance = Vector3.Distance(target.transform.position, binArea.transform.position);
        // y: value of microreward
        var y = 1/(distance*distance);
        // Reward Layer 2 = RewardLayer1 + microstepRewardLayer2
        return 1.618f + y;
    }

    // returns reward amount for layer 1
    public float RewardLayer1() {
        // distance between agent and target (box)
        float distance = Vector3.Distance(target.transform.position, this.transform.position);
        Debug.Log($"Distance to target: {distance}");
        // x: value of microreward, quadratic
        var x = 1/(distance*distance);
        //Debug.Log($"Reward for moving towards target:{x}");
        // cap microstep reward as less than macrostep reward (1) (want to remove this in future to make more natural/automated)
        if (x>1.618f) {
            x=1.618f;
        }
        // return the value of the reward (dense reward acting as a pathguidestepwisegradient)
        return x;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[1] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[3] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[3] = 2;
        }
    }

    

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    // public void ActionMoveAgent(int action)
    public void ActionMoveAgent(ActionSegment<int> action)
    {

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        // log the movement actions
        Debug.Log("" + string.Join(",", action.Array[1..4]));
        var zBlueAxis = action[1];
        var xRedAxis = action[2];
        var xzRotateAxis = action[3];

        switch(zBlueAxis){
            // forward
            case 1:
                dirToGo = transform.forward * 2f;
                break;
            // backward
            case 2:
                dirToGo = transform.forward * -2f;
                break;
        }
        switch(xRedAxis){
            // right
            case 1:
                dirToGo = transform.right * 2f;
                break;
            // left
            case 2:
                dirToGo = transform.right * -2f;
                break;
        }
        // refactor: rotational axis 
        switch(xzRotateAxis){
            // turn clockwise (right)
            case 1:
                rotateDir = transform.up * 2f;
                break;
            // turn counterclockwise (left)
            case 2:
                rotateDir = transform.up * -2f;
                break;
        }

        transform.Rotate(rotateDir, Time.fixedDeltaTime * 180f);
        m_Agent.AddForce(dirToGo, ForceMode.VelocityChange);
    }

    public void SelectPosition() {

        //////////////////////////////////////////////////
        ///////////////////////TBD////////////////////////
        //////////////////////////////////////////////////

        pickupScript.DropoffBox(carriedObject);
        
    }

    // action? ActionSelectTarget?
    public void SelectTarget(int x) {
        target = m_Box.boxPool[x].rb.transform;
   }

    

    /// <summary>
    ////Agent touched the target
    ///may need to change to when the distance is close enough so agent does not bump into it and fall down
    ///</summary>
     public void RewardTouchedTarget()
     {
         SetReward(2f);
         print($"Got to box!!!!! Total reward: {GetCumulativeReward()}");
         pickupScript.Pickup(target);
         target = binArea.transform;
     }


    /// <summary>
    ////Box got dropped off
    ///</summary>
    public void RewardDroppedBox()
    { 
        SetReward(5f);
        carriedObject = null;
        print($"Box dropped in bin!!!Total reward: {GetCumulativeReward()}");

        // By marking an agent as done AgentReset() will be called automatically.
        // EndEpisode();
    }

    /// <summmary>
    ////Agent got to the bin
    /// </summary>
    public void RewardGotToBin() 
    {
        if (carriedObject!=null) {
            SetReward(3f);
            SelectPosition();
        }
        else {SetReward(-1f);}

        //////// if the agent moves to the bin without a box, it should have a negative reward /////
        print($"Agent got to bin!!!! Total reward: {GetCumulativeReward()}");

    }
    public void AgentReset() 
    {
        m_Agent.position = new Vector3(5, 0, 5);
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }

    public void TotalRewardReset()
    {
        //SetReward(-100f);
    }


    public void SetResetParameters()
    {
        AgentReset();
        TotalRewardReset();
    }
}






/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage