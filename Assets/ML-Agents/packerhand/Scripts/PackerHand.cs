using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Boxes;

public class PackerHand : Agent
{
    /// <summary>
    /// The bin area.
    /// This will be set manually in the Inspector
    /// </summary>
    public GameObject binArea;

    Rigidbody m_Agent; //cache agent on initilization

    [HideInInspector] public Transform carriedObject; 

    [HideInInspector] public Transform target; //Target the agent will walk towards during training.

    [HideInInspector] public Vector3 position;  // Position of box inside bin

    public Vector3 rotation; // Rotation of box inside bin

    public float total_x_distance; //total x distance between agent and target

    public float total_y_distance; //total y distance between agent and target

    public float total_z_distance; //total z distance between agent and target
    
    public Dictionary<int, Vector3> organizedBoxPositions = new Dictionary<int, Vector3>();

    public int boxIdx;

    public Bounds areaBounds;

    public float binVolume;

    EnvironmentParameters m_ResetParams;
    BoxSpawner m_Box;


    public override void Initialize()
    {
        // Initialize box spawner
        m_Box = GetComponentInChildren<BoxSpawner>();
        
        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        // Create a box pool of boxes
        m_Box.SetUpBoxes();
        
        // Set environment parameters
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }


    public override void OnEpisodeBegin()
    {
        // Gets bounds of bin
        areaBounds = binArea.transform.GetChild(0).GetComponent<Collider>().bounds;

        // Encapsulate the bounds of each additional object in the overall bounds
        for (int i = 1; i < 5; i++)
        {
            areaBounds.Encapsulate(binArea.transform.GetChild(i).GetComponent<Collider>().bounds);
        }

        // Get total bin volumne
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2; // !! fix bin volume equation

        // Reset agent and rewards
        SetResetParameters();
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        // Add Bin position
        sensor.AddObservation(binArea.transform.position); //(x, y, z)

        // Add Bin size
        sensor.AddObservation(binArea.transform.localScale);

        foreach (var box in m_Box.boxPool) 
        {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            sensor.AddObservation(box.rb.position); //add box position to sensor observations
            sensor.AddObservation(box.rb.rotation); // add box rotation to sensor observations
            sensor.AddObservation(float.Parse(box.rb.tag)); //add box tag to sensor observations
        }

        // Add Agent postiion
        sensor.AddObservation(this.transform.position);

        // Add Agent velocity
        sensor.AddObservation(m_Agent.velocity.x); // !! does agent need to know his velocity? 
        sensor.AddObservation(m_Agent.velocity.z); // !! does agent need to know his velocity?
    }


    /// Agent learns which actions to take
    // discreteBranch1 = target: select target branch
    // discreteBranch2 = transform z-axis: move agent z-axis branch
    // discreteBranch3 = transform x-axis: move agent x-axis branch
    // discreteBranch4 = transform rotate: rotate agent branch
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;
        var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;
    
        SelectBox(discreteActions[++j]); 

        SelectRotation(discreteActions[++j]);

        SelectPosition(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        //this.transform.position.Set(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]);
        ///m_Agent.AddForce(new Vector3(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]));

        //////////////////////////temporary work on selecting specific positions in bin for box///////////////////////////////////
        // // currently SelectPosition does not use any ActionBuffers from brain
        // // feed in SensorVector > ActionBuffer.SelectPosition
        // // Restrict to a range so selected position is inside the bin 
        // float xPosition = Mathf.Clamp(binArea.transform.position.x, -(binArea.transform.localScale.x)/2, (binArea.transform.localScale.x)/2); // range > select from range in line: 129
        // float yPosition = Mathf.Clamp(binArea.transform.position.y, -(binArea.transform.localScale.y)/2, (binArea.transform.localScale.y)/2);
        // float zPosition = Mathf.Clamp(binArea.transform.position.z, -(binArea.transform.localScale.z)/2, (binArea.transform.localScale.z)/2);
        // SelectPosition(new Vector3(xPosition, yPosition, zPosition));

        // // Range(start, end)
        // // Range(leftedge, rightedge)
        // var xRange = new Range((binArea.transform.position.x-((binArea.transform.localScale.x)/2)), (binArea.transform.position.x+((binArea.transform.localScale.x)/2))) // return Range(start,end);
        // continuousActions[++i] // to select from Range(start,end)

        // var xRangeLow = binArea.transform.position.x-((binArea.transform.localScale.x)/2); // return Range(start,end);
        // var xRangeHigh = binArea.transform.position.x+((binArea.transform.localScale.x)/2); // return Range(start,end);

        // var yRangeLow = binArea.transform.position.y-((binArea.transform.localScale.y)/2); // return Range(start,end);
        // var yRangeHigh = binArea.transform.position.y+((binArea.transform.localScale.y)/2); // return Range(start,end);

        // var zRangeLow = binArea.transform.position.z-((binArea.transform.localScale.z)/2); // return Range(start,end);
        // var zRangeHigh = binArea.transform.position.z+((binArea.transform.localScale.z)/2); // return Range(start,end);

        // // float[] range = Enumerable.Range(0, (int)(end - start) + 1).Select(i => (float)i).ToArray();
        // float[] xRange = Enumerable.Range(0, (int)(xRangeHigh - xRangeLow) + continuousActions[++i] ).Select(i => (float)i).ToArray(); // continuousActions[++i] within Select() => Select(i => (float)continuousActions[++i])
        // float[] xRange = Enumerable.Range(0, (int)(xRangeHigh - xRangeLow) + 1).Select(continuousActions[++i] => (float)continuousActions[++i]).ToArray(); // continuousActions[++i] within Select() => Select(i => (float)continuousActions[++i])
        // float[] range = Enumerable.Range(0, (int)(13.0f - 1.0f) + 1).Select(i => (float)i).ToArray(); // returns ??
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// Reward Relativity
            // current - past // Reward
            // current - past // Reward
            // current < past // bad -Reward

        // Reward Layers
            // layerX = X + denseX
            // layer1 = 0 + dense1   0.1   0.08  0.14 0.43    1
            // layer2 = 1 + dense2   1.1   1.08  1.14 1.43  >  2
            // layer3 = 2 + dense3   2.1   2.08  2.14 2.43  >  3 

        // Reward Layer 2: MacrostepSparseMilestoneCheckpointEvolution=dropoffbox() MicrostepDenseGradientPathguide=distancetobin
            // if agent has pickedup a box
            // this is where agent has selected an exact position, what is the best way to close the distance? 
            //  if (carriedObject!=null && target!=null) 
            //  {
            //      SetReward(RLayer2()); // vs. refactor as RLayer2() containing SetReward(y)
            //  }
        // Reward Layer 1: MacrostepSparseMilestoneCheckpointEvolution=pickupbox() MicrostepDenseGradientPathguide=distancetobox
            // if agents hasnt picked up a box
            // if (carriedObject==null && target!=null) 
            // {
            //     // Assign Reward Layer 1
            //     // currently the Rlayer1 reward is not efficient
            //     //SetReward(RLayer1()); // vs. refactor as RLayer1() containing SetReward(x)
            //     SetReward(-1f/MaxStep);
            // }
            // // can also try this reward function
            // AddReward(-1f / MaxStep);
    }


    // public float RLayer2() 
    // {
    //     // distance between target (box) and goalarea (bin)
    //     float distance = Vector3.Distance(target.transform.position, binArea.transform.position);
    //     // y: value of microreward
    //     var y = 1/(distance*distance);
    //     // Reward Layer 2 = RewardLayer1 + microstepRewardLayer2
    //     return 1.618f + y;
    // }


    // public float RLayer1() 
    // {
    //     // distance between agent and target (box)
    //     float distance = Vector3.Distance(target.transform.position, this.transform.position);
    //     // x: value of microreward, quadratic
    //     var x = 1/(distance*distance);
    //     // cap microstep reward as less than macrostep reward (1) (want to remove this in future to make more natural/automated)
    //     if (x>1.618f) 
    //     {
    //         x=1.618f;
    //     }
    //     // return the value of the reward (dense reward acting as a pathguidestepwisegradient)
    //     return x;
    // }


    void FixedUpdate() // FixedUpdate (per-physics/frame-independent) vs. Update (per-frame/frame-dependent) for agent movement
    {
        //if agent selects a target box, it should move towards the box
        if (target!=null && carriedObject==null) 
        {
            UpdateAgentPosition();
        }
        //if agent selects a position, update box local position relative to the agent
        if (carriedObject!=null && carriedObject.parent!=null) 
        {
            UpdateAgentPosition();
            UpdateCarriedObject();
        }
        //if agent drops off the box, it should pick another one
        if (carriedObject==null && target==null) 
        {
            AgentReset();
        }
        else {return;}
    }


    void UpdateAgentPosition() 
    {
        var current_agent_x = this.transform.position.x;
        var current_agent_y = this.transform.position.y;
        var current_agent_z = this.transform.position.z;
        this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        current_agent_y + total_y_distance/100, current_agent_z+total_z_distance/100);    
    }

    
    void UpdateCarriedObject() 
    {
        var box_x_length = carriedObject.localScale.x;
        var box_z_length = carriedObject.localScale.z;
        var dist = 0.5f;
         // distance from agent is relative to the box size
        carriedObject.localPosition = new Vector3(box_x_length, dist, box_z_length);
        // stop box from rotating
        carriedObject.rotation = Quaternion.identity;
        // stop box from falling 
        carriedObject.GetComponent<Rigidbody>().useGravity = false;
    }


    void OnCollisionEnter(Collision col)
    {
        // Check if agent gets to a box
        if (col.gameObject.CompareTag("0") || col.gameObject.CompareTag("1")) 
        {
            // check if agent is not carrying a box already
            if (carriedObject==null && target!=null) 
            {
                PickupBox();
            }
        }
        // Check if agent goes into bin
        if (col.gameObject.CompareTag("goal")) 
        {   
            // Check if drop off information is available
            if (position!=Vector3.zero && rotation!=Vector3.zero && carriedObject!=null) 
            {
                DropoffBox();        
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
    public void SelectBox(int x) 
    {
        boxIdx = x;
        // Check if a box has already been selected and if agent is carrying box 
        // this prevents agent from constantly selecting other boxes and selecting an organized box
        if (carriedObject==null && target==null && !organizedBoxPositions.ContainsKey(boxIdx)) 
        {
            target = m_Box.boxPool[boxIdx].rb.transform;
            // Calculate total distance to box
            // Move total distance calculation to FixedUpdate
            total_x_distance = target.position.x-this.transform.position.x;
            total_y_distance = 0;
            total_z_distance = target.position.z-this.transform.position.z;
            // Add box to dictionary so it won't be selected again
            organizedBoxPositions.Add(boxIdx, position);
        }
    }


    public void SelectPosition(float x, float y, float z)
    {
        // Check if carrying a box and if position is known 
        // this prevents agent from selecting a position before having a box and constantly selecting other positions
        if (carriedObject!=null && position == Vector3.zero)
        {
            // Normalize x, y, z between 0 and 1 (passed in values are between -1 and 1)
            x = (x + 1f) * 0.5f;
            y = (y + 1f) * 0.5f;
            z = (z + 1f) * 0.5f;
            // Interpolate position between x, y, z bounds of the bin
            var x_position = Mathf.Lerp(-areaBounds.extents.x+1, areaBounds.extents.x-1, x);
            var y_position = Mathf.Lerp(-areaBounds.extents.y+1, areaBounds.extents.y-1, y);
            var z_position = Mathf.Lerp(-areaBounds.extents.z+1, areaBounds.extents.z-1, z);
            var testPosition = new Vector3(binArea.transform.position.x+x_position,
            binArea.transform.position.y+y_position, binArea.transform.position.z+z_position);

            if (!organizedBoxPositions.ContainsValue(testPosition) && areaBounds.Contains(testPosition)) 
            {
                Debug.Log($"SELECTED TARGET POSITION INSIDE BIN: {areaBounds.Contains(testPosition)}");
                // Move to FixedUpdate
                total_x_distance = binArea.transform.position.x-this.transform.position.x;
                total_y_distance = 0;
                total_z_distance = binArea.transform.position.z-this.transform.position.z;
                // Update box position
                position = testPosition;
                // Add updated box position to dictionary
                organizedBoxPositions[boxIdx] = position;
            }
        }
    }


    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>
    public void SelectRotation(int action) 
    {
         // Check if carrying a box and if rotation is known 
        // this prevents agent from selecting a rotation before having a box and constantly selecting other rotations
        if (carriedObject!=null && rotation == Vector3.zero) 
        {
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
        }
    }


    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() 
    {
        // Change carriedObject to target
        carriedObject = target.transform;
            
        // Attach carriedObject to agent
        carriedObject.SetParent(GameObject.FindWithTag("agent").transform, false);
        //carriedObject.parent = this.transform;

        // Set target to bin
        target = binArea.transform;

        // Reward agent for picking up box
        RewardPickedupTarget();
    }


    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox() 
    {
        // Detach box from agent
        carriedObject.SetParent(null);

        var m_rb =  carriedObject.GetComponent<Rigidbody>();

        // Set box physics
        m_rb.useGravity = true;
        //m_rb.isKinematic = false;
        m_rb.mass = 5f;
        m_rb.drag = 0.5f;

        // Set box position and rotation
        carriedObject.position = position; 
        carriedObject.rotation = Quaternion.Euler(rotation);

        // Update bin volume
        binVolume = binVolume-carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;

        // Set box tag
        carriedObject.tag = "1";

        // Reset position and rotation
        position = Vector3.zero;
        rotation = Vector3.zero;

        // Reset carriedObject and target
        carriedObject = null;
        target = null;

        // Reward agent for dropping off box
        RewardDroppedBox();
    }


    /// <summary>
    /// Rewards agent for reaching target box
    ///</summary>
    public void RewardPickedupTarget()
    {  
        AddReward(0.1f);
        Debug.Log($"Agent picked up target box! Total reward: {GetCumulativeReward()}");
    }

    
    /// <summary>
    //// Rewards agent for dropping off box
    ///</summary>
    public void RewardDroppedBox()
    { 
        AddReward(1f/binVolume);
        Debug.Log($"Agent dropped box in bin! Total reward: {GetCumulativeReward()}");
    }


    /// <summmary>
    /// Rewards agent for getting to bin
    /// </summary>
    public void RewardGotToBin() 
    {
        AddReward(1f);
        Debug.Log($"Agent got to bin with box! Total reward: {GetCumulativeReward()}");
    }


    public void AgentReset() 
    {
        this.transform.position = new Vector3(10f, 1.2f, 10f);
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }


    public void TotalRewardReset()
    {
        //SetReward(0f);
    }


    /// <summary>
    /// Agent moves according to selected action.
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


    public void SetResetParameters()
    {
        ///Reset agent
        AgentReset();

        //Reset rewards
        TotalRewardReset();

        //Reset boxes
        foreach (var box in m_Box.boxPool) 
        {
            box.ResetBoxes(box);
        }

        //Reset position
        position = Vector3.zero;

        //Reset rotation
        rotation = Vector3.zero;

        //Reset organized Boxes dictionary
        organizedBoxPositions.Clear();
    }


}


/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage