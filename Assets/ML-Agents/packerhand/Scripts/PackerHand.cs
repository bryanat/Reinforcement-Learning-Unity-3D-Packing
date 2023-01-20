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
using System.Linq;

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

    public List<Box> boxPool;

    public List<Vector3> backVertices;
    public List<Vector3> bottomVertices;
    public List<Vector3> sideVertices;

    // public Vector3 [] backVertices;
    // public Vector3 []bottomVertices;
    // public Vector3 [] sideVertices;
    public MeshFilter mf_back;
    public MeshFilter mf_bottom;
    public MeshFilter mf_side;

    public int testn = 0;

    public Dictionary<Vector3, int > allVertices;

    public List<Vector3> intersectingVertices;

    public GameObject blackbox;






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
        // Collider m_Collider = binArea.GetComponent<Collider>();
        // areaBounds = m_Collider.bounds;
        var renderers = binArea.GetComponentsInChildren<Renderer>();
        var areaBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; ++i)
            areaBounds.Encapsulate(renderers[i].bounds);

        Debug.Log($"BIN BOUNDS: {areaBounds}");


        // Get total bin volume from onstart
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;
        //miniBinVolume = miniBounds.extents.x*2 * miniBounds.extents.y*2 * miniBounds.extents.z*2;

        Debug.Log($" BIN VOLUME: {binVolume}");


        // Initialize agent for collision detection and mesh combiner 
        // CombineMesh sensorbin = binArea.GetComponent<CombineMesh>();
        // sensorbin.agent = this;

        CombineMesh [] sensors = binArea.GetComponentsInChildren<CombineMesh>();
        foreach (var sensor in sensors) {
            sensor.agent = this;
        }

        // Make agent unaffected by collision
        var m_c = GetComponent<CapsuleCollider>();
        m_c.isTrigger = true;

        // Get vertices of the bin
        UpdateVertices();


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
            //sensor.AddObservation(binMini.transform.position); 
            // Add Bin size
            //sensor.AddObservation(binArea.transform.localScale);
        }
        else 
        {
            // Add Bin position
            //sensor.AddObservation(binArea.transform.position);
            // Add Bin size
            sensor.AddObservation(binArea.transform.localScale);
        }

        foreach (var box in boxPool) 
        {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            // sensor.AddObservation(box.rb.position); //add box position to sensor observations
            // sensor.AddObservation(box.rb.rotation); // add box rotation to sensor observations
        }

        // // Add Agent postiion
        // sensor.AddObservation(this.transform.position);

        // // Add Agent velocity
        // sensor.AddObservation(m_Agent.velocity.x); // !! does agent need to know his velocity? 
        // sensor.AddObservation(m_Agent.velocity.z); // !! does agent need to know his velocity?
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;
        //var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;

        if (isBoxSelected==false) {
            SelectBox(discreteActions[++j]); 
        }

        if (isPickedup && isRotationSelected==false) {
            SelectRotation(discreteActions[++j]);
        }


        // should select position still be in here???//////
        if (isPickedup && isPositionSelected==false) {
            //SelectPosition(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            SelectPosition();
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
        // if box is dropped off, go for next round of box selection
        if (isDroppedoff) {
            UpdateVertices();
            StateReset();
        }
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
            // if (total_x_distance < 4f && total_z_distance<4f) {
            // note this (based on < distance) results in DropoffBox being called many times
            if (total_x_distance < 2f && total_z_distance<2f) {
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
        // this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        // current_agent_y/100, current_agent_z+total_z_distance/100);    
        this.transform.position = new Vector3(current_agent_x + total_x_distance/100, 
        target.position.y, current_agent_z+total_z_distance/100);   
    }


    /// <summary>
    /// Update carried object position relative to the agent position
    ///</summary>
    void UpdateCarriedObject() 
    {
        var box_x_length = carriedObject.localScale.x;
        var box_z_length = carriedObject.localScale.z;
        // var dist = 0.2f;
         // distance from agent is relative to the box size
        // carriedObject.localPosition = new Vector3(box_x_length, dist, box_z_length);
        carriedObject.localPosition = new Vector3(0,1,1);
        // stop box from rotating
        carriedObject.rotation = Quaternion.identity;
        // dont need to turn off gravity anymore since no more rb, rb is destroyed after pickupbox()
        // carriedObject.GetComponent<Rigidbody>().useGravity = false; 
    }

    /// <summary>
    /// Updates the vertices every time a new mesh is created
    ///</summary>
    public void UpdateVertices() {
        ///////// this for now creates all vertices list and dictionary from scratch every time a new mesh is created/////
        /////// future could add vertices of boxes to preexisting vertices list and dictionary for optimization//////////////
        Transform [] binObjects = binArea.GetComponentsInChildren<Transform>();
        allVertices = new Dictionary<Vector3, int>();
        foreach(Transform binObject in binObjects) {
            if (binObject.name == "BinIso20Back") {
                mf_back = binObject.GetComponent<MeshFilter>();
                // get unique set of back vertices
                RoundAndLocalToWorld(mf_back.mesh.vertices, backVertices);
                Debug.Log($"BACK VERTICES COUNT IS: {backVertices.Distinct().Count()}");
                Debug.Log($"EEE ALL VERTICES DICTIONARY COUNT IN BACK MESH LOOP IS {allVertices.Count()}");
                // backVertices.AddRange();

            }
            else if (binObject.name == "BinIso20Bottom") {
                mf_bottom = binObject.GetComponent<MeshFilter>();
                // get unique set of bottom vertices
                RoundAndLocalToWorld(mf_bottom.mesh.vertices, bottomVertices);
                Debug.Log($"Bottom VERTICES COUNT IS: {bottomVertices.Count}");
                Debug.Log($"EEE ALL VERTICES DICTIONARY COUNT IN BOTTOM MESH LOOP IS {allVertices.Count()}");
            }
            else if (binObject.name == "BinIso20Side") {
                mf_side = binObject.GetComponent<MeshFilter>();
                // get unique set of side vertices
                RoundAndLocalToWorld(mf_side.mesh.vertices, sideVertices);    
                Debug.Log($"Side VERTICES COUNT IS: {sideVertices.Count}");  
                Debug.Log($"EEE ALL VERTICES DICTIONARY COUNT IN SIDE MESH LOOP IS {allVertices.Count()}");  
            }
        }

    }

     /// <summary>
    /// For every mesh, creates a unique set of vertices with world position and add each unique vertex to a counter dictionary
    ///</summary>
    void RoundAndLocalToWorld(Vector3 [] vertices, List<Vector3> verticesList) {
        Matrix4x4 localToWorld = binArea.transform.localToWorldMatrix;
        var tempHashSet = new HashSet<Vector3>();
        // rounding part
        foreach (Vector3 vertex in vertices) {
            // first address vertices that are meant to be the same by rounding
            var roundedVertex = new Vector3((float)(Math.Round(vertex.x, 3)), (float)(Math.Round(vertex.y, 3)), (float)(Math.Round(vertex.z, 3)));
            // remove duplicates by using a hash set
            tempHashSet.Add(roundedVertex);
        }
        // localtoworld part
        foreach (Vector3 vertex in tempHashSet) {
            // convert local scale to world position
            Vector3 worldVertex = localToWorld.MultiplyPoint3x4(vertex);
            // add to vertices list
            verticesList.Add(worldVertex);
            // Add to a counter to check for intersection
            // reduce stage: vertex is key, value is int number which gets increased for each vertex
            if (allVertices.ContainsKey(worldVertex)) {
                allVertices[worldVertex] ++;
            }
            else {
                allVertices.Add(worldVertex, 1);
            }
        }
    }

    public void CreateBlackBox() {
        ////need to create a black box based on intersecting vertices
        //// there might be many intersecting vertices, black box is the subspace with smallest volume
        //// need box size: front, top and left/right
        /// for top: need box's top size
        // Vector3 topSideSize = targetBox.Find("top").localScale;
        // Vector3 topSidePos = targetBox.Find("top").localPosition;
        // Vector3 topVertex1 = Vector3.zero;
        // Vector3 topVertex2 = Vector3.zero;
        // Vector3 topVertex3 = Vector3.zero;
        // Vector3 topVertex4 = Vector3.zero;
        // foreach(Vector3 intersectingVertex in intersectingVertices) {
        //     // minus or add in x depends on placing from left to right or right to left
        //     if (intersectingVertex.x == topSidePos.x - topSideSize.x/2
        //     && intersectingVertex.y == topSidePos.y 
        //     && intersectingVertex.z == topSidePos.z+topSideSize.z/2) {
        //         topVertex1 = intersectingVertex;
        //         topVertex2 = new Vector3(intersectingVertex.x +topSideSize.x, intersectingVertex.y, intersectingVertex.z);
        //         topVertex3 = new Vector3(intersectingVertex.x, intersectingVertex.y, intersectingVertex.z-topSideSize.z);
        //         topVertex4 = new Vector3(intersectingVertex.x, intersectingVertex.z, intersectingVertex.z+??);
        //         // calculate volume
        //         // 
        //     }
        // }
    }


    public Vector3 SelectVertex() {
        //// selectedVertex = math.max(allVertices)
        foreach(KeyValuePair<Vector3, int> vertex in allVertices) {
            ///// the black box restraint can be added here
            ///// right now it's returning the first vertex where all 3 meshes intersect
            if (vertex.Value == 3) {
                Debug.Log($"VVV INTERSECTING VERTEX IS {vertex.Key}");
                intersectingVertices.Add(vertex.Key);
                //return vertex.Key;
            }
        }
        CreateBlackBox();
        // return default if no right vertex found
        return Vector3.zero;
    }

    public void SelectPosition() {
        targetBin  = new GameObject().transform;
        // Update box position
        ///// still needs to account for box rotation and size///
        /////vertex!=position///////////////
        targetBin.position = SelectVertex();

        //vertex: (8.25, 0.50, 79.50)
        Debug.Log($"SELECTED POSITION IS {targetBin.position}");
        isPositionSelected = true;   
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


    public void UpdateBinVolume() {
        // Update bin volume
        if (m_config==0) 
        {
            // miniBinVolume = miniBinVolume - carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;
            //  Debug.Log($"MINI BIN VOLUME IS {miniBinVolume}");
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
        var childrenList = carriedObject.GetComponentsInChildren<Transform>();

        // rotation order of operations key: zxy (z first, x second, y last)
        // [x,     y,       z]
        // [left, bottom, back]
        // [right, top, front]

        // xyz
        // [left, bottom, back]
        // [right, top, front]
        if (action == 1) {
            rotation = new Vector3(0, 0, 0);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
            }      
        }
        
        // xzy
        // [left, back, bottom]
        // [right, front, top]
        else if (action==2) {
            Debug.Log($"SelectRotation() called with rotation (90, 0, 0)");
            rotation = new Vector3(90, 0, 0);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
                if (child.name=="bottom") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "bottom";
                }

                else if (child.name == "top") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "top";
                }
            }
        }

        // zyx
        // [back, bottom, left]
        // [front, top, right]
        else if (action==3) {
            Debug.Log($"SelectRotation() called with rotation (0, 90, 0)");
            rotation = new Vector3(0, 90, 0);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "right";
                }
            }        
        }
      
        // yxz
        // [bottom, left, back]
        // [top, right, front]
        else if (action==4) {
            Debug.Log($"SelectRotation() called with rotation (0, 0, 90)");
            rotation = new Vector3(0, 0, 90);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "top";
                }
                else if (child.name == "top") 
                {
                    child.name = "right";
                }
            }
        }

        // zxy
        // [back, left, bottom]
        // [front, right, top]
        else if (action==5) {
            Debug.Log($"SelectRotation() called with rotation (0, 90, 90)");
            rotation = new Vector3(0, 90, 90 );
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "top";
                }
                else if (child.name == "top") 
                {
                    child.name = "right";
                }
            }      
        }

        // yzx
        //[bottom, back, left] 
        //[top, front, right]
        else {
            Debug.Log($"SelectRotation() called with rotation (0, 90, 90)");
            rotation = new Vector3(90, 0, 90);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "top";
                }
                else if (child.name == "top") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "right";
                }
            }      
        }

        /////// NOTE: No Vector3(90, 90, 90) or Vector3(90, 90, 0) rotations as
                // Vector3(90, 90, 90) == Vector3(90, 0, 0) == xzy
                // Vector3(90, 90, 0)  == Vector3(90, 0, 90) == yzx 

        ///// left -> back or bottom; 
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

        carriedObject.tag = "pickedupbox";
        foreach (Transform child in carriedObject.GetComponentsInChildren<Transform>())
        {
            child.tag = "pickedupbox";
            Debug.Log($"XAC child.name: {child.name}");
            Debug.Log($"XAC child.tag: {child.tag}");
        }
        Debug.Log($"XAB carriedObject.name: {carriedObject.name}");
        Debug.Log($"XAB carriedObject.tag: {carriedObject.tag}");
        // have to give children children.tag = "pickedupbox" 
        isPickedup = true;

        Destroy(carriedObject.GetComponent<BoxCollider>());
        Destroy(carriedObject.GetComponent<Rigidbody>());
    }


    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox() 
    {
      //A: select vertex from heuristic
        // selectedVertex = same vertex present in all three meshes 
      //B: select blackbox 
        // selectedBlackbox = vertices including selectedVertex that creates rectangular prism
      //C: select box from NN/blue/brain
      //D: select position from A+B
        // 1: rotate (reduce to 6) => carriedObject.transform.localRotation => Vector3(x,y,z)
        // 2: magnitude: magnitude = SELECTEDBOX.localScale * 0.5 : Vector3(0.5x, 0.5y, 0.5z) : half of each x,y,z (magnitudeX = SELECTEDBOX.localScale.x * 0.5; magnitudeY = SELECTEDBOX.localScale.y * 0.5; magnitudeZ = SELECTEDBOX.localScale.z * 0.5; )
        // 3: direction: directionX = blackbox.position.x.isPositive (true=1 or false=-1), directionY = blackbox.position.y.isPositive, directionZ = blackbox.position.z.isPositive
        // 4: 1+2+3: selectedPosition = Vector3( (selectedVertex.x + (magnitudeX * directionX)), (selectedVertex.y + (magnitudeY * directionY)), (selectedVertex.z + (magnitudeZ * directionZ)) )


        // Detach box from agent
        carriedObject.SetParent(null);

        // var m_rb =  carriedObject.GetComponent<Rigidbody>();
        //var m_c = carriedObject.GetComponent<Collider>();
        Collider [] m_cList = carriedObject.GetComponentsInChildren<Collider>();
        //m_rb.isKinematic = false;


        // foreach (Collider m_c in m_cList) {
        //     m_c.isTrigger = false;
        // }

        // Lock box position and location
        ///////////////////////COLLISION/////////////////////////
        carriedObject.position = targetBin.position; // COLLISION OCCURS IMMEDIATELY AFTER SET POSITION OCCURS
        ///////////////////////COLLISION/////////////////////////
        carriedObject.rotation = Quaternion.Euler(rotation);
        // m_rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

        //Destroy(carriedObject.GetComponent<Rigidbody>());

        // Enbles OnTriggerEnter in CollideAndCombineMesh 
        //m_c.isTrigger = true;

        foreach (Collider m_c in m_cList) {
            m_c.isTrigger = false;
            // m_c.gameObject.tag = "droppedoff";
        }

        Debug.Log($"DropoffBox(): end of droppedoff function");

    }


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

