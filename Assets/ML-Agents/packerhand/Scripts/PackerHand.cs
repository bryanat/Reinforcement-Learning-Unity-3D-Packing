using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;
using Unity.MLAgentsExamples;
using Boxes;

public class PackerHand : Agent 
{
    int m_Configuration;  // Depending on this value, different curriculum will be picked
    int m_config; // local reference of the above

    public NNModel unitBoxBrain;   // Brain to use when all boxes are 1 by 1 by 1
    public NNModel similarBoxBrain;     // Brain to use when boxes are of similar sizes
    public NNModel regularBoxBrain;     // Brain to use when boxes size vary

    string m_UnitBoxBehaviorName = "UnitBox"; // 
    string m_SimilarBoxBehaviorName = "SimilarBox";
    string m_RegularBoxBehaviorName = "RegularBox";

    public GameObject binArea; // The bin container, which will be manually selected in the Inspector
    public GameObject binMini; // The mini bin container, used for lower lessons of Curriculum learning

    Rigidbody m_Agent; //cache agent rigidbody on initilization

    [HideInInspector] public Transform carriedObject; // local reference to box picked up by agent

    [HideInInspector] public Transform target; //Target the agent will walk towards during training.

    [HideInInspector] public Transform targetTransformPosition; //Target the agent will walk towards during training.

    public Vector3 rotation; // Rotation of box inside bin

    public float total_x_distance; //total x distance between agent and target
    public float total_y_distance; //total y distance between agent and target
    public float total_z_distance; //total z distance between agent and target
    
    public Dictionary<int, Vector3> organizedBoxPositions = new Dictionary<int, Vector3>(); // dictionary of organzed boxes and their positions

    public int boxIdx; // box selected from box pool

    public Bounds areaBounds; // regular bin's bounds

    public Bounds miniBounds; // mini bin's bounds

    public float binVolume; // regular bin's volume
    public float miniBinVolume; // mini bin's volume

    public List<List<float>> x_space = new List<List<float>>(); // x-axix search space
    public List<List<float>> y_space = new List<List<float>>(); // y-axis search space
    public List<List<float>> z_space = new List<List<float>>(); // z-axis search space

    EnvironmentParameters m_ResetParams; // Environment parameters
    public BoxSpawner boxSpawner; // Box Spawner

    [HideInInspector] public Vector3 initialAgentPosition;

    [HideInInspector] public bool isPositionSelected;
    [HideInInspector] public bool isRotationSelected;
    [HideInInspector] public bool isDroppedoff;
    [HideInInspector] public bool isPickedup;



    public override void Initialize()
    {   

        initialAgentPosition = this.transform.position;

        Debug.Log($"BOX SPAWNER IS {boxSpawner}");

        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        // Picks which curriculum to train
        m_Configuration = 0;
        m_config = 0;
        
        // Set environment parameters
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // Update model references if we're overriding
        var modelOverrider = GetComponent<ModelOverrider>();
        if (modelOverrider.HasOverrides)
        {
            unitBoxBrain = modelOverrider.GetModelForBehaviorName(m_UnitBoxBehaviorName);
            m_UnitBoxBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_UnitBoxBehaviorName);

            similarBoxBrain = modelOverrider.GetModelForBehaviorName(m_SimilarBoxBehaviorName);
            m_SimilarBoxBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_SimilarBoxBehaviorName);

            regularBoxBrain = modelOverrider.GetModelForBehaviorName(m_RegularBoxBehaviorName);
            m_RegularBoxBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_RegularBoxBehaviorName);
        }
    }


    public override void OnEpisodeBegin()
    {   
        // Get bin bounds
        UpdateBinBounds();

        // Get total bin volume from onstart
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;
        miniBinVolume = miniBounds.extents.x*2 * miniBounds.extents.y*2 * miniBounds.extents.z*2;

        // Reset agent and rewards
        SetResetParameters();
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        if (m_config==0) 
        {
            // Add Bin position
            sensor.AddObservation(binMini.transform.position); 
            // Add Bin size
            sensor.AddObservation(binMini.transform.localScale);
        }
        else 
        {
            // Add Bin position
            sensor.AddObservation(binArea.transform.position);
            // Add Bin size
            sensor.AddObservation(binArea.transform.localScale);
        }

        foreach (var box in boxSpawner.boxPool) 
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

        if (isPickedup==false) {
            SelectBox(discreteActions[++j]); 
        }

        if (isPickedup && isRotationSelected==false) {
            SelectRotation(discreteActions[++j]);
        }

        if (isPickedup && isPositionSelected==false) {
            SelectPosition(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        }


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



    /// <summary>
    /// This function is called at every time step
    ///</summary>
    void FixedUpdate() 
    {
        // Initialize curriculum and brain
        if (m_Configuration != -1)
        {
            ConfigureAgent(m_Configuration);
            m_Configuration = -1;
        }
        //if agent selects a target box, it should move towards the box
        if (isPickedup == false) 
        {
            UpdateAgentPosition();
        }
        //if agent is carrying a target box, it should move towards the bin
        else if (isPickedup && isDroppedoff==false) 
        {
            UpdateAgentPosition();
            UpdateCarriedObject();
            if (total_x_distance<0.1f && total_z_distance<0.1f) {
                DropoffBox();
            }
        }
        //if agent drops off the box, it should pick another one
        // else if (carriedObject==null && target==null) 
        // {
        //     AgentReset();
        // }
        else {return;}
    }
    
    /// <summary>
    /// Updates agent position relative to the target position
    ///</summary>
    void UpdateAgentPosition() 
    {
        total_x_distance = target.position.x-this.transform.position.x;
        total_y_distance = target.position.y-this.transform.position.y;
        total_z_distance = target.position.z-this.transform.position.z;
        var current_agent_x = this.transform.position.x;
        var current_agent_y = this.transform.position.y;
        var current_agent_z = this.transform.position.z;
        this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        current_agent_y, current_agent_z+total_z_distance/100);    
    }

    /// <summary>
    /// Update carried object position relative to the agent position
    ///</summary>
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


    /// <summary>
    /// This function is called whenever agent collides into something
    ///</summary>
    void OnCollisionEnter(Collision col)

    {
        // Check if agent gets to a box outside the bin
        if (col.gameObject.CompareTag("0"))// || col.gameObject.CompareTag("1")) 
        {
            // check if agent is not carrying a box already
            if (isPickedup==false && isPositionSelected) 
            {
                PickupBox();
            }
        }
        else 
        {        
            return; // the agent bumps into something that's not a target
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
        if (!organizedBoxPositions.ContainsKey(boxIdx)) 
        {
            Debug.Log($"SELECTED BOX: {boxIdx}");
            target = boxSpawner.boxPool[boxIdx].rb.transform;
            // Add box to dictionary so it won't be selected again
            organizedBoxPositions.Add(boxIdx, Vector3.zero);
        }
    }


    /// <summary>
    /// Agent selects position for box
    ///</summary>
    public void SelectPosition(float x, float y, float z) 
    {
        // Check if carrying a box and if position is known 
        // this prevents agent from selecting a position before having a box and constantly selecting other positions
        // Normalize x, y, z between 0 and 1 (passed in values are between -1 and 1)
        x = (x + 1f) * 0.5f;
        y = (y + 1f) * 0.5f;
        z = (z + 1f) * 0.5f;
        var x_position = 0f;
        var y_position = 0f;
        var z_position = 0f;
        // var l = boxSpawner.boxPool[boxIdx].boxSize.x;
        // var w = boxSpawner.boxPool[boxIdx].boxSize.y;
        // var h = boxSpawner.boxPool[boxIdx].boxSize.z;
        var test_position = Vector3.zero;
        if (m_config==0) {
            // Interpolate position between x, y, z bounds of the mini bin
            x_position = Mathf.Lerp(-miniBounds.extents.x+1, miniBounds.extents.x-1, x);
            y_position = Mathf.Lerp(-miniBounds.extents.y+1, miniBounds.extents.y-1, y);
            z_position = Mathf.Lerp(-miniBounds.extents.z+1, miniBounds.extents.z-1, z);
            test_position = new Vector3(binMini.transform.position.x+x_position, binMini.transform.position.y+y_position, binMini.transform.position.z+z_position);
            // check if position inside bin bounds
            if (!organizedBoxPositions.ContainsValue(test_position) && miniBounds.Contains(test_position)) {
                // var overlap = false;
                // // check for overlap with preexisting boxes
                // //if (x_space.Count>0) {
                //     for (int i = 1; i < x_space.Count; i++) {
                //         if (test_position[0]+l/2>x_space[i][0] && test_position[0]-l/2<x_space[i][1]) {
                //             Debug.Log("x space overlap");
                //             overlap = true;
                //             break;
                //         }
                //         if (test_position[1]+w/2>y_space[i][0] && test_position[1]-w/2<y_space[i][1]) {
                //             Debug.Log("y space overlap");
                //             overlap = true;
                //             break;
                //         }
                //         if (test_position[2]+h/2>z_space[i][0] && test_position[2]-h/2<z_space[i][1]) {
                //             Debug.Log("z space overlap");
                //             overlap = true;
                //             break;
                //         }
                //     }
                //     //}
                //     // Update box position
                //     if (overlap==false) 
                //     {
                        var targetTransformPositionGameObject = new GameObject();
                        targetTransformPosition = targetTransformPositionGameObject.GetComponent<Transform>();
                        target = targetTransformPosition;
                        target.position = test_position; // teleport.
                        Debug.Log($"SELECTED POSITION IS {target.position}");
                        // Add updated box position to dictionary
                        organizedBoxPositions[boxIdx] = target.position;
                        isPositionSelected = true;

                        // Update search space
                        // UpdateSearchSpace(l, w, h);
                   // }

                }
            }
            else {
            // Interpolate position between x, y, z bounds of the bin
                x_position = Mathf.Lerp(-areaBounds.extents.x+1, areaBounds.extents.x-1, x);
                y_position = Mathf.Lerp(-areaBounds.extents.y+1, areaBounds.extents.y-1, y);
                z_position = Mathf.Lerp(-areaBounds.extents.z+1, areaBounds.extents.z-1, z);
                test_position = new Vector3(binArea.transform.position.x+x_position,
                binArea.transform.position.y+y_position, binArea.transform.position.z+z_position);
                if (!organizedBoxPositions.ContainsValue(test_position) && areaBounds.Contains(test_position)) 
                {                 
                    var targetTransformPositionGameObject = new GameObject();
                    targetTransformPosition = targetTransformPositionGameObject.GetComponent<Transform>();
                    target = targetTransformPosition;
                    // Update box position
                    target.position = test_position; // teleport.
                    Debug.Log($"SELECTED POSITION IS {target.position}");
                    // Add updated box position to dictionary
                    organizedBoxPositions[boxIdx] = target.position;
                    isPositionSelected = true;
                }     
        }
    }

    /// <summary>
    /// Decrease search space as boxes get added
    /// this adds x, y, z ranges of spaces boxes have taken up
    ///</summary>
    void UpdateSearchSpace(float l, float w, float h) 
    {
        var position = organizedBoxPositions[boxIdx];
        var x_range = new List<float> {position.x-l/2, position.x+l/2};
        var y_range = new List<float> {position.y-w/2, position.x+w/2};
        var z_range = new List<float> {position.z-h/2, position.x+h/2};
        x_space.Add(x_range);
        y_space.Add(y_range);
        z_space.Add(z_range);
    }

    void UpdateBinBounds() {
        // Gets bounds of bin
        areaBounds = binArea.transform.GetChild(0).GetComponent<Collider>().bounds;

        // Gets bounds of mini bin
        miniBounds = binMini.transform.GetChild(0).GetComponent<Collider>().bounds;

        var num_sides = 5;

        // Encapsulate the bounds of each additional object in the overall bounds
        for (int i = 1; i < num_sides; i++)
        {
            areaBounds.Encapsulate(binArea.transform.GetChild(i).GetComponent<Collider>().bounds);
            miniBounds.Encapsulate(binMini.transform.GetChild(i).GetComponent<Collider>().bounds);
        }
        Debug.Log($"REGULAR BIN BOUNDS IS {areaBounds}");
        Debug.Log($"MINI BIN BOUNDS IS {miniBounds}");
    }

    void UpdateBinVolume() {
        // Update bin volume
        if (m_config==0) 
        {
            miniBinVolume = miniBinVolume - carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;
             Debug.Log($"MINI BIN VOLUME IS {miniBinVolume}");
        }
        else 
        {
            binVolume = binVolume-carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;
            Debug.Log($"REGULAR BIN VOLUME IS {binVolume}");
        }
        
    }



    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>
    public void SelectRotation(int action) 
    {
         // Check if carrying a box and if rotation is known 
        // this prevents agent from selecting a rotation before having a box and constantly selecting other rotations
        switch (action) 
            {
            case 1:
                rotation = new Vector3(0, 0, 0);
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

        isPickedup = true;

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
        // m_rb.mass = 1000f;
        // m_rb.drag = 0.5f;

        // Set box position and rotation
        carriedObject.position = target.position; 
        carriedObject.rotation = Quaternion.Euler(rotation);

        // m_rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

        // Update bin volume
        UpdateBinVolume();

        // Update bin bounds
        //UpdateBinBounds();

        // Set box tag
        carriedObject.tag = "1";

        // Reset carriedObject and target
        carriedObject = null;
        target = null;

        // Enable new position and rotation to be selected
        isPositionSelected = false;
        isRotationSelected = false;
        isDroppedoff = true;

    }


    /// <summary>
    /// Rewards agent for reaching target box
    ///</summary>
     public void RewardPickedupTarget()
     {  
        AddReward(0.01f);
        Debug.Log($"Got to target box!!!!! Total reward: {GetCumulativeReward()}");
    }

    
    /// <summary>
    //// Rewards agent for dropping off box
    ///</summary>
    public void RewardDroppedBox(float surface_area)
    { 
        AddReward(0.05f);
        Debug.Log($"Box dropped in bin!!!Total reward: {GetCumulativeReward()}");
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
        this.transform.position = initialAgentPosition; // Vector3 of agents initial transform.position
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
        // Reset agent
        AgentReset();

        // Reset rewards
        TotalRewardReset();

        // Reset boxes
        foreach (var box in boxSpawner.boxPool) 
        {
            box.ResetBoxes(box);
        }

        // Reset organized Boxes dictionary
        organizedBoxPositions.Clear();

        // Reset position and rotation
        isPositionSelected = false;
        isRotationSelected = false;
        isDroppedoff = false;
        isPickedup = false;
    }


    /// <summary>
    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    /// A different reward system needs to be designed for each level
    /// </summary>
    void ConfigureAgent(int n) 
    {
        if (n==0) 
        {
            boxSpawner.SetUpBoxes(n, m_ResetParams.GetWithDefault("unit_box", 1));
            SetModel(m_UnitBoxBehaviorName, unitBoxBrain);
            Debug.Log($"BOX POOL HAS {boxSpawner.boxPool.Count} BOXES");
        }
        if (n==1) 
        {
            SetModel(m_SimilarBoxBehaviorName, similarBoxBrain);
        }
        else 
        {
            boxSpawner.SetUpBoxes(n, 0);
            SetModel(m_RegularBoxBehaviorName, regularBoxBrain);    
        }
    }
    

}



/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage