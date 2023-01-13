using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Policies;
using Box = Boxes.Box;
using Boxes;
using System.Collections;

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
    [HideInInspector] public Transform targetBox; // target box selected by agent
    [HideInInspector] public Transform targetBin; // phantom target bin object where the box will be placed

    [HideInInspector] public Transform targetTransformPosition; //Target the agent will walk towards during training.

    public Vector3 rotation; // Rotation of box inside bin

    public float total_x_distance; //total x distance between agent and target
    public float total_y_distance; //total y distance between agent and target
    public float total_z_distance; //total z distance between agent and target
    
    // public List<int> organizedBoxes = new List<int>(); // list of organzed box indices

    // public int boxIdx; // box selected from box pool

    public Bounds areaBounds; // regular bin's bounds

    public Bounds miniBounds; // mini bin's bounds

    public float binVolume; // regular bin's volume
    public float miniBinVolume; // mini bin's volume

    EnvironmentParameters m_ResetParams; // Environment parameters
    public BoxSpawner boxSpawner; // Box Spawner

    [HideInInspector] public Vector3 initialAgentPosition;

    [HideInInspector] public bool isPositionSelected;
    [HideInInspector] public bool isRotationSelected;
    [HideInInspector] public bool isPickedup;
    [HideInInspector] public bool isBoxSelected;
    [HideInInspector] public bool isDroppedoff;
    public int nbox;

    public List<Box> boxPool;




    public override void Initialize()
    {   

        initialAgentPosition = this.transform.position;

        Debug.Log($"BOX SPAWNER IS {boxSpawner}");

        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>(); 
        
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

        // Create boxes according to curriculum
        //boxSpawner.SetUpBoxes(0, m_ResetParams.GetWithDefault("unit_box", 1));
        boxSpawner.SetUpBoxes(2, m_ResetParams.GetWithDefault("regular_box", 0));
        boxPool = Box.GetBoxPool();
    }


    public override void OnEpisodeBegin()
    {   

        Debug.Log("-----------------------NEW EPISODE STARTS------------------------------");

       // Picks which curriculum to train
        // for now 1 is for unit boxes/mini bin, 2 is for similar sized boxes/regular bin, 3 is for regular boxes/regular bin
        // m_Configuration = 0;
        // m_config = 0;
        m_Configuration = 2;
        m_config = 2;

        // Get bin's bounds from onstart
        Collider m_Collider = binArea.GetComponent<Collider>();
        areaBounds = m_Collider.bounds;

        Debug.Log($"REGULAR BIN COLLIDER BOUNDS IS {areaBounds}");


        // Get total bin volume from onstart
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;
        miniBinVolume = miniBounds.extents.x*2 * miniBounds.extents.y*2 * miniBounds.extents.z*2;

        Debug.Log($"ONSTART BIN VOLUME USING REGULAR VOLUME CALCULATING METHOD: {binVolume}");


        // Initialize agent for collision detection and mesh combiner 
        CollideAndCombineMesh sensorbin = binArea.GetComponent<CollideAndCombineMesh>();
        sensorbin.agent = this;

        // Reset agent and rewards
        SetResetParameters();
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        /////once the box combines with the bin, we should also add bin bounds and bin volumne to observation
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

        foreach (var box in boxPool) 
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

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;
        var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;

        if (isBoxSelected==false) {
            SelectBox(discreteActions[++j]); 
        }

        if (isPickedup && isRotationSelected==false) {
            SelectRotation(discreteActions[++j]);
        }

        if (isPickedup && isPositionSelected==false) {
            SelectPosition(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        }
    } 


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
        // if (isDroppedoff) {
        //     StateReset();
        // }
        // if agent selects a box, it should move towards the box
        else if (isBoxSelected && isPickedup == false) 
        {
            UpdateAgentPosition(targetBox);
            if (total_x_distance < 0.1f && total_z_distance<0.1f) {
                PickupBox();
            }
        }
        //if agent is carrying a box it should move towards the selected position
        else if (isPickedup && isPositionSelected && isRotationSelected) 
        {
            UpdateAgentPosition(targetBin);
            UpdateCarriedObject();
            //if agent is close enough to the position, it should drop off the box
            if (total_x_distance < 0.1f && total_z_distance<0.1f) {
                DropoffBox();
            }
    
        }
        //if agent drops off the box, it should pick another one
        else if (isBoxSelected==false) 
        {
            AgentReset();
        }
        else {return;}
    }
    
    /// <summary>
    /// Updates agent position relative to the target position
    ///</summary>
    void UpdateAgentPosition(Transform target) 
    {
        total_x_distance = target.position.x-this.transform.position.x;
        total_y_distance = target.position.y-this.transform.position.y;
        total_z_distance = target.position.z-this.transform.position.z;
        var current_agent_x = this.transform.position.x;
        var current_agent_y = this.transform.position.y;
        var current_agent_z = this.transform.position.z;
        this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        current_agent_y/100, current_agent_z+total_z_distance/100);    
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
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int n) 
    {
        // Check if a box has already been selected
        if (!Box.organizedBoxes.Contains(n)) 
        {
            Box.boxIdx = n;
            Debug.Log($"SELECTED BOX: {Box.boxIdx}");
            targetBox = boxPool[Box.boxIdx].rb.transform;
            // Add box to list so it won't be selected again
            Box.organizedBoxes.Add(Box.boxIdx);
            isBoxSelected = true;

        }
    }



    /// <summary>
    /// Agent selects position for box
    ///</summary>

    public void SelectSpace() {

    }


    /// <summary>
    /// Agent selects position for box
    ///</summary>
    public void SelectPosition(float x, float y, float z) 
    { 
        // Scale x, y, z between 0 and 1 (passed in values are between -1 and 1)
        x = (x + 1f) * 0.5f;
        y = (y + 1f) * 0.5f;
        z = (z + 1f) * 0.5f;
        var x_position = 0f;
        var y_position = 0f;
        var z_position = 0f;
        var l = boxPool[Box.boxIdx].boxSize.x;
        var h = boxPool[Box.boxIdx].boxSize.y;
        var w = boxPool[Box.boxIdx].boxSize.z;
        var test_position = Vector3.zero;
        if (m_config==0) {
            // Interpolate position between x, y, z bounds of the mini bin
            x_position = Mathf.Lerp(binMini.transform.position.x-miniBounds.extents.x+1, binMini.transform.position.x+miniBounds.extents.x-1, x);
            y_position = Mathf.Lerp(binMini.transform.position.y-miniBounds.extents.y+1, binMini.transform.position.y+miniBounds.extents.y-1, y);
            z_position = Mathf.Lerp(binMini.transform.position.z-miniBounds.extents.z+1, binMini.transform.position.z+miniBounds.extents.z-1, z);
            test_position = new Vector3(x_position,y_position,z_position);
            ////////WHY DOESN'T THE ABOVE GIVE US A POSITION INSIDE BIN?????????????///////////
            Debug.Log($"TEST POSITION IS INSIDE BIN: {miniBounds.Contains(test_position)}");
            // check if position inside bin bounds
            if (miniBounds.Contains(test_position)) {
                    targetBin  = new GameObject().transform;
                    targetBin.position = test_position; // teleport.
                    Debug.Log($"SELECTED POSITION IS {targetBin.position}");
                    isPositionSelected = true;
                //     // Update search space
                //     UpdateSearchSpace(l, w, h);
                // }

                }
            }
            else {
                // Interpolate position between x, y, z bounds of the bin
                x_position = Mathf.Lerp(binArea.transform.position.x-areaBounds.extents.x+1, binArea.transform.position.x+areaBounds.extents.x-1, x);
                y_position = Mathf.Lerp(binArea.transform.position.y-areaBounds.extents.y+1, binArea.transform.position.y+areaBounds.extents.y-1, y);
                z_position = Mathf.Lerp(binArea.transform.position.z-areaBounds.extents.z+1, binArea.transform.position.z+areaBounds.extents.z-1, z);
                test_position = new Vector3(x_position, y_position,z_position);
                if (areaBounds.Contains(test_position)) 
                {              
                    targetBin  = new GameObject().transform;
                    // Update box position
                    targetBin.position = test_position; // teleport.
                    Debug.Log($"SELECTED POSITION IS {targetBin.position}");
                    isPositionSelected = true;   
            //    }  
             } 
        }
    }




    public void UpdateBinVolume() {
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
         isRotationSelected = true;
    }


    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() 
    {
        // Change carriedObject to target
        carriedObject = targetBox.transform;
            
        // Attach carriedObject to agent
        //carriedObject.SetParent(GameObject.FindWithTag("agent").transform, false);
        carriedObject.parent = this.transform;

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
        var m_c = carriedObject.GetComponent<Collider>();
        //m_rb.isKinematic = false;

        // Lock box position and location
        carriedObject.position = targetBin.position; 
        carriedObject.rotation = Quaternion.Euler(rotation);
        m_rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

        // Enbles OnTriggerEnter in CollideAndCombineMesh 
        m_c.isTrigger = true;

        //StartCoroutine(IsFinal());
    }

    /// <summary>
    // Wait for 5 sec to see if collision occurs upon drop off
    // if no collision, box will be sent back to spawning area
    ///</summary>
    // IEnumerator IsFinal() {

    //     yield return new WaitForSecondsRealtime(5);
    //     if (isDroppedoff == false) {
    //         carriedObject.position = boxPool[Box.boxIdx].startingPos;
    //         Box.organizedBoxes.Remove(Box.boxIdx); 
    //         StateReset();     
    //     }
    //     else {
    //         nbox+=1;
    //         Debug.Log($"NUMBER OF BOXES PACKED:{nbox}");
    //         StateReset();
    //     }

    // }



    
    /// <summary>
    //// Rewards agent for large contact surface area
    ///</summary>
    public void RewardSurfaceArea(float surface_area)
    { 
        AddReward(0.005f*surface_area);
        Debug.Log($"SurfaceArea is {surface_area} Dropped in bin!!!Total reward: {GetCumulativeReward()}");
    }

    /// <summary>
    //// Rewards agent for select a good position
    ///</summary>
    public void RewardSelectedPosition()
    { 
        SetReward(1f);
        Debug.Log($"Box dropped in bin!!!Total reward: {GetCumulativeReward()}");
    }


    public void AgentReset() 
    {
        this.transform.position = initialAgentPosition; // Vector3 of agents initial transform.position
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }

    public void StateReset() 
    {
        isBoxSelected = false;
        isPositionSelected = false;
        isRotationSelected = false;
        isPickedup = false;
        isDroppedoff = false;
        targetBin = null;
        targetBox = null;
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
        foreach (var box in boxPool) 
        {
            Box.ResetBoxes(box);
        }

        // Reset organized Boxes dictionary
        Box.organizedBoxes.Clear();

        // Reset states;
        StateReset();

    }


    /// <summary>
    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    /// A different reward system needs to be designed for each level
    /// </summary>
    void ConfigureAgent(int n) 
    {
        /////////////CURRENTLY IT'S NOT POSSIBLE TO CHANGE THE VECTOR OBSERVATION SPACE SIZE AT RUNTIME/////////////////////
        /////IMPLIES IF WE CHANGE NUMBER OF BOXES DURING EACH CURRICULUM LEARNING, OBSERVATION WILL EITHER BE PADDED OR TRUNCATED//////////////////
        if (n==0) 
        {
            SetModel(m_UnitBoxBehaviorName, unitBoxBrain);
            Debug.Log($"BOX POOL SIZE: {boxPool.Count}");
        }
        if (n==1) 
        {
            // boxSpawner.SetUpBoxes(n, 1);
            SetModel(m_SimilarBoxBehaviorName, similarBoxBrain);
            Debug.Log($"BOX POOL SIZE: {boxPool.Count}");
        }
        else 
        {
            SetModel(m_RegularBoxBehaviorName, regularBoxBrain);    
            Debug.Log($"BOX POOL SIZE: {boxPool.Count}");
        }
    }
    

}

