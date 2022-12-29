using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using Box = Boxes2.Box2;
using Boxes2;
using static SensorDetectBin;

public class PackerHand : Agent

{
    /// <summary>
    /// The bin area.
    /// This will be set manually in the Inspector
    /// </summary>
    public GameObject binArea;

    Rigidbody m_Agent; //cache agent on initilization

    [HideInInspector]
    public Transform carriedObject;

    [HideInInspector]
    public Transform target; //Target the agent will walk towards during training.

    [HideInInspector]
    public Vector3 position;  // Position of box inside bin

    public Vector3 rotation; // Rotation of box inside bin

    public float total_x_distance; //total x distance between agent and target

    public float total_y_distance; //total y distance between agent and target

    public float total_z_distance; //total z distance between agent and target
    
    public Dictionary<int, Vector3> organizedBoxes = new Dictionary<int, Vector3>();


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

    }


    public override void OnEpisodeBegin()
    {

        Debug.Log("++++++++++++++++++++BOX in ONEPISODEBEGIN++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);


        Debug.Log("++++++++++++++++++++BOX POOL COUNT++++++++++++++++++++++++++++++++++++++++++");
        Debug.Log(m_Box.boxPool.Count);

        // Initialize agent for bin's script
        // SensorDetectBin binDetect= binArea.GetComponent<SensorDetectBin>();
        // binDetect.agent = this; 

        // Reset agent and rewards
        SetResetParameters();

    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
    
        // Add Bin position
        sensor.AddObservation(binArea.transform.position); //(x, y, z)

        // Add Bin size
        sensor.AddObservation(binArea.transform.localScale);


        foreach (var box in m_Box.boxPool) {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            sensor.AddObservation(box.rb.position); //add box position to sensor observations
            sensor.AddObservation(float.Parse(box.rb.tag)); //add box tag to sensor observations
            sensor.AddObservation(box.rb.rotation); // add box rotation to sensor observations
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
        //var i = -1;


        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;
    
        SelectBox(discreteActions[++j]); 
        //MoveAgent(discreteActions[++j]);

        // Restrict to a range so selected position is inside the bin 
        var areaBounds = binArea.GetComponent<Collider>().bounds;
        float xPosition = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        float yPosition = Random.Range(-areaBounds.extents.y, areaBounds.extents.y);
        float zPosition = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
        SelectPosition(new Vector3(binArea.transform.position.x+xPosition, binArea.transform.position.y+yPosition,binArea.transform.position.z+zPosition));

        // Restrict rotation to 90 or 180 or else it'll be too much to learn
        SelectRotation(discreteActions[++j]);


        //SelectPosition(new Vector3(continuousActions[++i], continuousActions[++i], continuousActions[++i]));



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
            // this is where agent has selected an exact position, what is the best way to close the distance? 
            //  if (carriedObject!=null && target!=null) {
            //      SetReward(RLayer2()); // vs. refactor as RLayer2() containing SetReward(y)
            //  }
        // Reward Layer 1: MacrostepSparseMilestoneCheckpointEvolution=pickupbox() MicrostepDenseGradientPathguide=distancetobox
            // if agents hasnt picked up a box
            // if (carriedObject==null && target!=null) {
            //     // Assign Reward Layer 1
            //     // currently the Rlayer1 reward is not efficient
            //     //SetReward(RLayer1()); // vs. refactor as RLayer1() containing SetReward(x)
            //     SetReward(-1f/MaxStep);
            // }
            
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

    void FixedUpdate() {
        //if agent selected a target box, it should move towards the box
        if (target!=null && carriedObject==null) {
            UpdateAgentPosition();
        }
        //if agent selected a position, update box local position relative to the agent
        if (carriedObject!=null && carriedObject.parent!=null) {
            UpdateAgentPosition();
            UpdateCarriedObject();
        }
        //if agent drops off the box, it should pick another one
        if (carriedObject==null && target==null) {
            AgentReset();
        }
        else {return;}
    }

    void UpdateAgentPosition() {
        var current_agent_x = this.transform.position.x;
        var current_agent_y = this.transform.position.y;
        var current_agent_z = this.transform.position.z;
        this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        current_agent_y + total_y_distance/100, current_agent_z+total_z_distance/100);    
    }

    
    void UpdateCarriedObject() {
        var box_x_length = carriedObject.localScale.x;
        var box_z_length = carriedObject.localScale.z;
        var dist = 0.5f;
         // distance from agent is relative to the box size
        carriedObject.localPosition = new Vector3(box_x_length+dist, dist, box_z_length+dist);
        // stop box from rotating
        carriedObject.rotation = Quaternion.identity;
        // stop box from falling 
        carriedObject.GetComponent<Rigidbody>().useGravity = false;

    }


    void OnCollisionEnter(Collision col)
    {
        // Check if agent gets to a box
        if (col.gameObject.CompareTag("0") || col.gameObject.CompareTag("1")) {
            // check if agent is not carrying a box already
            if (carriedObject==null && target!=null) {
                PickupBox();
                RewardPickedupTarget();
            }
    
        }
        // Check if agent goes into bin
        if (col.gameObject.CompareTag("goal"))
        {   
            // Check if drop off information is available
            if (position!=Vector3.zero && rotation!=Vector3.zero && target!=null) {
                DropoffBox();
                RewardGotToBin();
                
            }
        }
        else {
            // the agent bumps into something that's not a target
            return;
        }

    }

    /// <summary>
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int x) {
        // Check if a box has already been selected and if agent is carrying box 
        // this prevents agent from constantly selecting other boxes and selecting an organized box
        if (carriedObject==null && target==null && !organizedBoxes.ContainsKey(x)) {
            target = m_Box.boxPool[x].rb.transform;
            Debug.Log($"SELECTED TARGET AS BOX {x}, TARGET BOX SIZE IS {target.transform.localScale}");
            // Calculate total distance to box
            total_x_distance = target.position.x-this.transform.position.x;
            total_y_distance = 0;
            total_z_distance = target.position.z-this.transform.position.z;
            // Add box to organized list so it won't be selected again
            organizedBoxes.Add(x, position);

        }
   }

    /// <summary>
    /// Agent selects a position for the box
    /// </summary>
    public void SelectPosition(Vector3 pos) {
        // Check if carrying a box and if position is known 
        // this prevents agent from selecting a position before having a box and constantly selecting other positions
        ///////THIS POSITION CHECK PROBABLY SHOULD BE A RANGE WITH RESPECT TO BOX SIZE   /////////////////
        if (carriedObject!=null && position == Vector3.zero && !organizedBoxes.ContainsValue(pos)) {
            position = pos;
            Debug.Log($"SELECTED TARGET POSITION AT: {position}");
            total_x_distance = position.x-this.transform.position.x;
            total_y_distance = position.y-this.transform.position.y;
            total_z_distance = position.z-this.transform.position.z;
        }

    }

    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>

    public void SelectRotation(int action) {
         // Check if carrying a box and if rotation is known 
        // this prevents agent from selecting a rotation before having a box and constantly selecting other rotations
        if (carriedObject!=null && rotation == Vector3.zero) {
            switch (action) 
            {
                case 1:
                    rotation = new Vector3(180, 180, 180);
                    break;
                case 2:
                    rotation = new Vector3(0, 90, 90 );
                    break;
                case 3:
                    rotation = new Vector3(90, 0, 90);
                    break;
                case 4:
                    rotation = new Vector3(90, 90, 0);
                    break;
                case 5:
                    rotation = new Vector3(90, 90, 90);
                    break;
                case 6:
                    rotation = new Vector3(0, 0, 90);
                    break;
                case 7:
                    rotation = new Vector3(90, 0, 0);
                    break;
                case 8:
                    rotation = new Vector3(0, 90, 0);
                    break;
            }
         Debug.Log($"SELECTED TARGET ROTATION: {rotation}");
        }

    }

    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() {
        // Change carriedObject from null to target
        carriedObject = target.transform;
            
        // Attach carriedObject to agent
        carriedObject.SetParent(GameObject.FindWithTag("agent").transform, false);
        //carriedObject.parent = this.transform;

        // Set target to bin
        target = binArea.transform;


    }

    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox() {

        // Detach box from agent
        carriedObject.SetParent(null);

        var rb =  carriedObject.GetComponent<Rigidbody>();

        // // stop box from floating away
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Set box position
        carriedObject.position = position; 

        // Set box rotation
        carriedObject.rotation = Quaternion.Euler(rotation);

        // Set box tag
        carriedObject.tag = "1";

        // Reset postition to "null"
        position = Vector3.zero;

        // Reset carriedObject to null
        carriedObject = null;

        // Reset target
        target = null;
        

    }



    /// <summary>
    /// Rewards agent for reaching target box
    ///</summary>
     public void RewardPickedupTarget()
     {  
        SetReward(2f);
        Debug.Log($"Got to target box!!!!! Total reward: {GetCumulativeReward()}");

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
        SetReward(3f);
        Debug.Log($"Agent got to bin with box!!!! Total reward: {GetCumulativeReward()}");

    }

    public void AgentReset() 
    {
        this.transform.position = new Vector3(0, 2, 0);
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

        //Reset position
        position = Vector3.zero;

        //Reset rotation
        rotation = Vector3.zero;

        //Reset organized Boxes dictionary
        organizedBoxes.Clear();

    }

}






/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage