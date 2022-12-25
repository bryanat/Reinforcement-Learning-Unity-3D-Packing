using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using Box = Boxes2.Box2;
using Boxes2;
using static PickupScript;
using static SensorDetectBin;

public class PackerHand : Agent

{
    public GameObject ground;

    public GameObject binArea;


    //cache agent on initilization
    Rigidbody m_Agent; 

    [HideInInspector]
    public Transform carriedObject;

    [HideInInspector]
    public Transform target; //Target the agent will walk towards during training.


    EnvironmentParameters m_ResetParams;
    BoxSpawner2 m_Box;

    public override void Initialize()
    {

        // Initialize box spawner
        m_Box = GetComponentInChildren<BoxSpawner2>();

        Debug.Log("++++++++++++++++++++BOX in INITIALIZE++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);

        
        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        // Create a box pool of boxes
        m_Box.SetUpBoxes();
        
        // Set environment parameters
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // Reset agent and rewards
        //SetResetParameters();
    }


    public override void OnEpisodeBegin()
    {

        Debug.Log("++++++++++++++++++++BOX in ONEPISODEBEGIN++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);


        Debug.Log("++++++++++++++++++++BOX POOL COUNT++++++++++++++++++++++++++++++++++++++++++");
        Debug.Log(m_Box.boxPool.Count);

        // Initialize agent for box's script
        foreach (var box in m_Box.boxPool) {
            box.ps.agent = this;
        }


        // Initialize agent for bin's script
        SensorDetectBin binDetect= binArea.GetComponent<SensorDetectBin>();
        binDetect.agent = this; 

        // Reset agent and rewards
        SetResetParameters();

    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
    
        // Add Bin position
        sensor.AddObservation(binArea.transform.position); //(x, y, z)

        // Add Box size and position
        foreach (var box in m_Box.boxPool) {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            sensor.AddObservation(box.rb.transform.position); //add box position to sensor observations
        }

        // Add Agent postiion
        sensor.AddObservation(this.transform.position);

        // Add Agent velocity
        sensor.AddObservation(m_Agent.velocity.x);
        sensor.AddObservation(m_Agent.velocity.z);

    }

    /// <summary>
    /// Agent learns which actions to take
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        var j = -1;
        var i = -1;


        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;
    
        SelectTarget(discreteActions[++j]); 
        MoveAgent(discreteActions[++j]);


        SelectPosition(new Vector3(continuousActions[++i], continuousActions[++i], continuousActions[++i]));



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
                // SetReward(RLayer2()); // vs. refactor as RLayer2() containing SetReward(y)
            // }
        // Reward Layer 1: MacrostepSparseMilestoneCheckpointEvolution=pickupbox() MicrostepDenseGradientPathguide=distancetobox
            // if agents hasnt picked up a box
            if (target!=null) {
                // Assign Reward Layer 1
                // AddReward(x);
                // SetReward(x);
                SetReward(RLayer1()); // vs. refactor as RLayer1() containing SetReward(x)
            }
            
            // // can also try this reward function
            // AddReward(-1f / MaxStep);

    }

        public float RLayer2() {
        // distance between target (box) and goalarea (bin)
        float distance = Vector3.Distance(target.transform.position, binArea.transform.position);
        // y: value of microreward
        var y = 1/(distance*distance);
        // Reward Layer 2 = RewardLayer1 + microstepRewardLayer2
        return 1.618f + y;
    }

    public float RLayer1() {
        // distance between agent and target (box)
        float distance = Vector3.Distance(target.transform.position, this.transform.position);
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

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("box"))
        {
            if (CheckTarget()) {
                PickupBox();
                RewardPickedupTarget();
            }
        }

        if (col.gameObject.CompareTag("goal"))
        {
            RewardGotToBin();
        }
        else {
            return;
        }

    }

    /// <summary>
    /// Agent moves box to selected position in bin.
    /// </summary>
    public void SelectPosition(Vector3 position) {

        if (carriedObject!=null) {
            DropoffBox(position);
        }

    }

    /// <summary>
    /// Agent moves according to selected action.
    /// </summary>
    public void MoveAgent(int action)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_Agent.AddForce(dirToGo, 
            ForceMode.VelocityChange);
    }

    /// <summary>
    /// Agent selects target box
    ///</summary>
    public void SelectTarget(int x) {
        target = m_Box.boxPool[x].rb.transform;
   }


    /// <summmary>
    /// Agent checks if target is box, outside the bin, and not being held
    /// </summary>
    public bool CheckTarget() {
        PickupScript2 pickupScript = target.GetComponent<PickupScript2>();
        return pickupScript!=null && !pickupScript.isHeld && !pickupScript.isOrganized;
    }   

    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() {
        // Change carriedObject from null to target
        carriedObject = target.transform;

        Debug.Log($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~`Agent POSITION IS: {this.transform.position}~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

        // Attach carriedObject to agent
        carriedObject.SetParent(GameObject.FindWithTag("agent").transform, false);
        // Move carriedObject to the same position as the parent
        carriedObject.localPosition=Vector3.zero;
        // Prevent carriedObject from falling to the ground
        carriedObject.gameObject.GetComponent<Rigidbody>().useGravity = false;
        carriedObject.gameObject.GetComponent<Rigidbody>().mass = 0;
        carriedObject.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        carriedObject.gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;


        Debug.Log($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~`CARRIED OBJECT POSITION IS: {carriedObject.transform.position}~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

        // Change box property to isHeld 
        PickupScript2 pickupScript = carriedObject.GetComponent<PickupScript2>();
        pickupScript.isHeld = true;

        // Set target to bin
        target = binArea.transform;
    }

    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox(Vector3 position) {

        // Detach box from agent
        //carriedObject.SetParent(null);

        // Set box position
        //carriedObject.position = position; 

        // Change box property to not held and organized
        // PickupScript pickupScript = carriedObject.GetComponent<PickupScript>();
        // pickupScript.isHeld = false;
        // pickupScript.isOrganized = true;

        // Reset carriedObject to null
        //carriedObject = null;

    }



    /// <summary>
    /// Rewards agent for reaching target box
    ///</summary>
     public void RewardPickedupTarget()
     {  
        if (carriedObject!=null) {
            SetReward(2f);
            Debug.Log($"Got to target box!!!!! Total reward: {GetCumulativeReward()}");

        }
        else { 
            SetReward(-1f);
            Debug.Log($"Agent failed to pick up target!! Total reward: {GetCumulativeReward()}");
         }

     }
    /// <summary>
    //// Rewards agent for dropping off box
    ///</summary>
    public void RewardDroppedBox()
    { 
        SetReward(5f);
        Debug.Log($"Box dropped in bin!!!Total reward: {GetCumulativeReward()}");

    }

    /// <summmary>
    /// Rewards agent for getting to bin
    /// </summary>
    public void RewardGotToBin() 
    {
        if (carriedObject!=null) {
            SetReward(3f);
            Debug.Log($"Agent got to bin with box!!!! Total reward: {GetCumulativeReward()}");

        }
        else {
            SetReward(-1f);
            Debug.Log($"Agent got to bin without box!!!! Total reward: {GetCumulativeReward()}");
        }

    }
    public void AgentReset() 
    {
        this.transform.position = new Vector3(5, 0, 5);
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }

    public void TotalRewardReset()
    {
        //SetReward(-100f);
    }


    public void SetResetParameters()
    {
        ///Reset agent
        AgentReset();

        //Reset rewards
        TotalRewardReset();

        //Reset boxes
        foreach (var box in m_Box.boxPool) {
            box.ResetBoxes(box);
        }
    }

    //EndEpisode() 
}






/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage