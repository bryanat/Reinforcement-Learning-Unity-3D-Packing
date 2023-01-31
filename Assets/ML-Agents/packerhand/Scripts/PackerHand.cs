using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Policies;
using Box = Boxes.Box;
using Boxes;

public class PackerHand : Agent 
{
    public int packSpeed = 20;

    int curriculum_ConfigurationGlobal;  // Depending on this value, different curriculum will be picked
    //int curriculum_ConfigurationLocal; // local reference of the above

    public NNModel unitBoxBrain;   // Brain to use when all boxes are 1 by 1 by 1
    public NNModel similarBoxBrain;     // Brain to use when boxes are of similar sizes
    public NNModel regularBoxBrain;     // Brain to use when boxes size vary

    string m_UnitBoxBehaviorName = "UnitBox"; // 
    string m_SimilarBoxBehaviorName = "SimilarBox";
    string m_RegularBoxBehaviorName = "RegularBox";

    Rigidbody m_Agent; //cache agent rigidbody on initilization
    [HideInInspector] public Transform targetBox; // target box selected by agent
    [HideInInspector] public Transform targetBin; // phantom target bin object where the box will be placed

    public int boxIdx; // box selected from box pool
    public Vector3 rotation; // Rotation of box inside bin
    public Vector3 selectedVertex; // Vertex of box inside bin

    //public Dictionary<Vector3, int > allVerticesDictionary = new Dictionary<Vector3, int>();
    public List<Vector3> backMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    public List<Vector3> sideMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    public List<Vector3> bottomMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    public Vector3 [] verticesArray; // space: 2n + 1 Vector3 vertices where n = num boxes

    public int VertexCount = 0;
    public List<GameObject> blackbox_list; 

    public float total_x_distance; //total x distance between agent and target
    public float total_y_distance; //total y distance between agent and target
    public float total_z_distance; //total z distance between agent and target
    
    public List<int> organizedBoxes = new List<int>(); // list of organzed box indices

    public GameObject binArea; // The bin container, which will be manually selected in the Inspector
    public Bounds areaBounds; // regular bin's bounds
    public float binVolume; // regular bin's volume

    EnvironmentParameters m_ResetParams; // Environment parameters
    public BoxSpawner boxSpawner; // Box Spawner

    public SensorCollision sensorCollision;

    [HideInInspector] public Vector3 initialAgentPosition;

    [HideInInspector] public bool isBlackboxUpdated;
    [HideInInspector] public bool isVertexSelected;
    [HideInInspector] public bool isBoxSelected;
    [HideInInspector] public bool isRotationSelected;
    [HideInInspector] public bool isPickedup;
    [HideInInspector] public bool isDroppedoff;
    [HideInInspector] public bool isStateReset;
    public bool isBottomMeshCombined;
    public bool isSideMeshCombined;
    public bool isBackMeshCombined;

    public List<Box> boxPool;
    public GameObject binBottom;
    public GameObject binBack;
    public GameObject binSide;

    public Vector3 boxWorldScale;

    public Material clearPlastic;




    public override void Initialize()
    {   
        initialAgentPosition = this.transform.position;

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

        boxPool = BoxSpawner.boxPool;
    }


    public override void OnEpisodeBegin()
    {   
        Debug.Log("-----------------------NEW EPISODE STARTS------------------------------");

        // Picks which curriculum to train
        curriculum_ConfigurationGlobal = 2;
        //curriculum_ConfigurationLocal = 2; // local copy of curriculum configuration number, global will change to -1 but need original copy for state management

        Renderer [] renderers = binArea.GetComponentsInChildren<Renderer>();
        areaBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; ++i)
            areaBounds.Encapsulate(renderers[i].bounds);

        Debug.Log($"BIN BOUNDS: {areaBounds}");
        // Get total bin volume from onstart
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;

        Debug.Log($" BIN VOLUME: {binVolume}");

        // Make agent unaffected by collision
        CapsuleCollider m_c = GetComponent<CapsuleCollider>();
        m_c.isTrigger = true;

        // Reset agent and rewards
        SetResetParameters();

        // Reset boxes on new episode by spawning new boxes
        boxSpawner.SetUpBoxes(2, m_ResetParams.GetWithDefault("regular_box", 0));

        selectedVertex = new Vector3(8.25f, 0.50f, 10.50f); // refactor to select first vertex
        // selectedVertex = new Vector3(where the three trimesh meet init);
        isVertexSelected = true;
        
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        /////once the box combines with the bin, we should also add bin bounds and bin volumne to observation

        // Add Bin size
        sensor.AddObservation(binArea.transform.localScale);

        // array of all boxes
        foreach (Box box in boxPool) 
        {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            // sensor.AddObservation(box.rb.rotation); // add box rotation to sensor observations
        }
        //sensor.AddObserv

        // // array of vertices
        foreach (Vector3 vertex in verticesArray) {
            sensor.AddObservation(vertex); //add vertices to sensor observations
        }

        // // array of blackboxes 
        // sensor.AddObservation(blackboxesArray); //add vertices to sensor observations
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;
        //var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        //var continuousActions = actionBuffers.ContinuousActions;

        if (isBlackboxUpdated && isVertexSelected == false) 
        {
            //SelectVertex(); 
            SelectVertex(discreteActions[++j]);
        }

        if (isVertexSelected && isBoxSelected==false) 
        {
            SelectBox(discreteActions[++j]); 
        }

        if (isPickedup && isRotationSelected==false) 
        {
            j = 0; // set discrete actions incrementor to 0 in case the SelectBox if loop isnt triggered 
            SelectRotation(discreteActions[++j]);
        }
    }



    /// <summary>
    /// This function is called at every time step
    ///</summary>
    void FixedUpdate() 
    {
        // Initialize curriculum and brain
        if (curriculum_ConfigurationGlobal != -1)
        {
            ConfigureAgent(curriculum_ConfigurationGlobal);
            curriculum_ConfigurationGlobal = -1;
        }

        // if meshes are combined, reset states, update vertices and black box, and go for next round of box selection 
        if (isBackMeshCombined && isBottomMeshCombined && isSideMeshCombined && isStateReset==false) 
        {
            StateReset();
            // vertices array of tripoints doesn't depend on the trimesh
            // only update vertices list and vertices array when box is placed
            UpdateVerticesArray();
            // side, back, and bottom vertices lists depends on the trimesh
            UpdateVerticesList();
            // both vertices array and vertices list are used to find black boxes
            UpdateBlackBox();
            UpdateBinVolume();
        }

        // if agent selects a box, it should move towards the box
        else if (isBoxSelected && isPickedup == false) 
        {
            UpdateAgentPosition(targetBox);
            if ( Math.Abs(total_x_distance) < 0.1f && Math.Abs(total_z_distance) < 0.1f ) 
            {
                PickupBox();
            }
        }

        //if agent is carrying a box it should move towards the selected position
        else if (isPickedup && isRotationSelected && isDroppedoff == false) 
        {
            UpdateBoxPosition();
            UpdateAgentPosition(targetBin);
            UpdateTargetBox();
            //if agent is close enough to the position, it should drop off the box
            if ( Math.Abs(total_x_distance) < 2f && Math.Abs(total_z_distance) < 2f ) 
            {
                if (sensorCollision.passedGravityCheck)
                {
                    DropoffBox();
                }
                else
                {
                    BoxReset("failedPhysicsCheck");
                }
            }

        }
        //if agent drops off the box, it should pick another one
        else if (isBoxSelected==false) 
        {
            AgentReset();
        }

        else { return;}
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
        this.transform.position = new Vector3(current_agent_x + total_x_distance/packSpeed, 
        target.position.y, current_agent_z+total_z_distance/packSpeed);   
    }


    /// <summary>
    /// Update carried object position relative to the agent position
    ///</summary>
    void UpdateTargetBox() 
    {
        var box_x_length = targetBox.localScale.x;
        var box_z_length = targetBox.localScale.z;
        // var dist = 0.2f; 
         // distance from agent is relative to the box size
        // targetBox.localPosition = new Vector3(box_x_length, dist, box_z_length);
        targetBox.localPosition = new Vector3(0,1,1);
        // stop box from rotating
        targetBox.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Updates the vertices every time a new mesh is created
    ///</summary>
    void UpdateVerticesList() 
    {
        ///////// this for now creates all vertices list and dictionary from scratch every time a new mesh is created/////
        //allVerticesDictionary.Clear();
        MeshFilter mf_back = binBack.GetComponent<MeshFilter>();
        AddVertices(mf_back.mesh.vertices, backMeshVertices);
        Debug.Log($"OOO BACK MESH VERTICES COUNT IS {backMeshVertices.Count()}");
        MeshFilter mf_bottom = binBottom.GetComponent<MeshFilter>();
        AddVertices(mf_bottom.mesh.vertices, bottomMeshVertices);
        Debug.Log($"OOO BOTTOM MESH VERTICES COUNT IS {bottomMeshVertices.Count()}");
        MeshFilter mf_side = binSide.GetComponent<MeshFilter>();
        AddVertices(mf_side.mesh.vertices, sideMeshVertices);  
        Debug.Log($"OOO SIDE MESH VERTICES COUNT IS {sideMeshVertices.Count()}");
    }

    /// <summary>
    /// For every mesh, add each unique vertex to a mesh list and a counter dictionary
    ///</summary>
    // Vertices used for constructing blackbox
    // AddVertices( input: ALL_LOCAL_VerticesFromMesh, output: UNIQUE_GLOBAL_VerticesFromMesh )
    void AddVertices(Vector3 [] vertices, List<Vector3> verticesList) 
    {
        Matrix4x4 localToWorld = binArea.transform.localToWorldMatrix;
        var tempHashSet = new HashSet<Vector3>();
        // rounding part
        foreach (Vector3 vertex in vertices) 
        {
            // first address vertices that are meant to be the same by rounding
            var roundedVertex = new Vector3((float)(Math.Round(vertex.x, 2)), (float)(Math.Round(vertex.y, 2)), (float)(Math.Round(vertex.z, 2)));
            // remove duplicates by using a hash set
            tempHashSet.Add(roundedVertex);
        }
        // localtoworld part
        foreach (Vector3 vertex in tempHashSet) 
        {
            // convert local scale to world position
            Vector3 worldVertex = localToWorld.MultiplyPoint3x4(vertex);
            verticesList.Add(worldVertex);
            // Add to a counter dictionary to check for intersection
            // reduce stage: vertex is key, value is int number which gets increased for each vertex
            // if (allVerticesDictionary.ContainsKey(worldVertex)) 
            // {
            //     allVerticesDictionary[worldVertex] ++;
            // }
            // else 
            // {
            //     allVerticesDictionary.Add(worldVertex, 1);
            // }
        }
    }


    void UpdateVerticesArray() 
    {
        List<Vector3> tripoints_list = new List<Vector3>();
        var tripoint_redx = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y, selectedVertex.z); // x red side tripoint
        var tripoint_greeny = new Vector3(selectedVertex.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z); // y green bottom tripoint 
        var tripoint_bluez = new Vector3(selectedVertex.x, selectedVertex.y, selectedVertex.z+boxWorldScale.z); // z blue back tripoint 

        // purpose of split in 3: only need to compare tripoint_redx to sideMeshVertices (1/3 = n complexity) instead of tripoint_redx to all 3 meshes (3/3 = 3n complexity)
        bool is_tripoint_redx_sameAsVertex = false;
        bool is_tripoint_greeny_sameAsVertex = false;
        bool is_tripoint_bluez_sameAsVertex = false;

        // RED / X / SIDE-LEFTRIGHT
        // loop over all vertexes in tripoints corresponding mesh first to check that tripoint is not an existing vertex from mf_side.mesh.vertices (which would be a bad stability score vertex)
        foreach ( Vector3 vertex in sideMeshVertices)
        {
            // stateflag is_tripoint_redx_sameAsVertex set to true will prevent tripoint being added to tripoint_list since its an existing vertex from mf_side.mesh.vertices
            if (tripoint_redx == vertex){
                is_tripoint_redx_sameAsVertex = true;
                break;
            }
        }
        // if tripoint is not a shared vertex point add tripoint to list (effectively, creating an illegal tripoint and bad stability score placement)
        if (!is_tripoint_redx_sameAsVertex){
            tripoints_list.Add(tripoint_redx);
        }

        // GREEN / Y / BOTTOM-TOP
        foreach ( Vector3 vertex in bottomMeshVertices)
        {
            if (tripoint_greeny == vertex){
                is_tripoint_greeny_sameAsVertex = true;
                break;
            }
        }
        if (!is_tripoint_greeny_sameAsVertex){
            tripoints_list.Add(tripoint_greeny);
        }

        // BLUE / Z / BACK-FRONT
        foreach ( Vector3 vertex in backMeshVertices)
        {
            if (tripoint_bluez == vertex){
                is_tripoint_bluez_sameAsVertex = true;
                break;
            }
        }
        if (!is_tripoint_bluez_sameAsVertex){
            tripoints_list.Add(tripoint_bluez);
        }
        
        for (int idx = 0; idx<tripoints_list.Count(); idx++) 
        {
            Debug.Log($"TPB tripoints_list[idx]: {tripoints_list[idx]} | areaBounds.min: {areaBounds.min} | areaBounds.max: {areaBounds.max} ");
            if (tripoints_list[idx].x >= areaBounds.min.x && tripoints_list[idx].x < areaBounds.max.x) {
            if (tripoints_list[idx].y >= areaBounds.min.y && tripoints_list[idx].y < areaBounds.max.y) {
            if (tripoints_list[idx].z >= areaBounds.min.z && tripoints_list[idx].z < areaBounds.max.z) {
                Debug.Log($"TPX idx:{idx} | tripoint add to tripoints_list[idx]: {tripoints_list[idx]} | selectedVertex: {selectedVertex}") ;
                verticesArray[VertexCount] = tripoints_list[idx];
                VertexCount ++;
                Debug.Log($"VERTEX COUNT IS {VertexCount}");
            }
            }
            }
        }
    }


    public void UpdateBlackBox() 
    {
        Debug.Log($"UBX Update BlackboX running");

        // foreach (Vector3 vertex in verticesArray) 
        // {
        //     //bottomVertices.Find(v=>  v[1]==vertex[1] && v[2]==vertex[2]).MinBy(v => Math.Abs(v[0]-vertex[0]));
        //     Vector3 closest_x_vertex = backMeshVertices.Aggregate(new Vector3(float.MaxValue, 0, 0), (min, next) => 
        //     vertex[0]<next[0] && Math.Abs(next[0]-vertex[0]) < Math.Abs(min[0] - vertex[0]) && next[1]==vertex[1] && next[2] == vertex[2] ? next : min);
        //     Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES X VERTEX IS {closest_x_vertex}");

        //     Vector3 closest_y_vertex = sideMeshVertices.Aggregate(new Vector3(0, float.MaxValue, 0), (min, next) => 
        //     vertex[1]<next[1] && Math.Abs(next[1]-vertex[1]) < Math.Abs(min[1] - vertex[1]) && next[0]==vertex[0] && next[2] == vertex[2] ? next : min);
        //     Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Y VERTEX IS {closest_y_vertex}");

        //     Vector3 closest_z_vertex = sideMeshVertices.Aggregate(new Vector3(0, 0, float.MaxValue), (min, next) => 
        //     vertex[2]<next[2] && Math.Abs(next[2]-vertex[2]) < Math.Abs(min[2] - vertex[2]) && next[1]==vertex[1] && next[0] == vertex[0] ? next : min);
        //     Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Z VERTEX IS {closest_z_vertex}");

        //     float blackbox_x_size = Math.Abs(closest_x_vertex[0] - vertex[0]);
        //     float blackbox_y_size = Math.Abs(closest_y_vertex[1] - vertex[1]);
        //     float blackbox_z_size = Math.Abs(closest_z_vertex[2] - vertex[2]);
        //     Vector3 blackbox_position = new Vector3(blackbox_x_size*0.5f+vertex[0], blackbox_y_size*0.5f+vertex[1], blackbox_z_size*0.5f+vertex[2]);
        //     Debug.Log($"BPS BLACK BOX POSITION {blackbox_position} SIZES {blackbox_x_size}, {blackbox_y_size}, {blackbox_z_size}");

        //     GameObject blackbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //     blackbox.name = "blackbox";
        //     blackbox.transform.position = blackbox_position;
        //     blackbox.transform.localScale = new Vector3(blackbox_x_size, blackbox_y_size, blackbox_z_size);
        //     Renderer cubeRenderer = blackbox.GetComponent<Renderer>();
        //     cubeRenderer.material = clearPlastic;

        //     blackbox_list.Add(blackbox);
        // }
        isBlackboxUpdated = true;
    }


    public void SelectVertex(int action_SelectedVertex) 
    {
        // Don't select empty vertex (0,0,0) from actionBuffer. Punish to teach it to learn not to pick empty ~ give negative reward and force to repick.
        if (verticesArray[action_SelectedVertex] == new Vector3(0, 0, 0))
        {
            // give negative reward
            isVertexSelected = false; // to make repick SelectVertex(discreteActions[++j])
            return; // to end function call
        }

        // assign selected vertex where next box will be placed, selected from brain's actionbuffer (inputted as action_SelectedVertex)
        selectedVertex = verticesArray[action_SelectedVertex];
        // remove consumed selectedVertex from verticesArray (since another box cannot be placed there)
        if (isBackMeshCombined && isSideMeshCombined && isBottomMeshCombined) {
            verticesArray[action_SelectedVertex] = new Vector3(0, 0, 0);
        }
        Debug.Log($"SVX Selected VerteX: {selectedVertex}");

        // Range( 0f, 2*organizedBoxes.Count() ) // 2n + 1, keeping this comment in case organizedBoxes.Count() is useful later

        isVertexSelected = true;
    }





    public void UpdateBoxPosition() 
    {
        // Packerhand.cs  : deals parent box : position (math)
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)

        if (targetBin==null) 
        {
            targetBin  = new GameObject().transform;

            float magnitudeX = boxWorldScale.x * 0.5f; 
            float magnitudeY = boxWorldScale.y * 0.5f; 
            float magnitudeZ = boxWorldScale.z * 0.5f; 

            // 2: Direction
            int directionX = 1; 
            int directionY = 1;
            int directionZ = 1;

            // var directionX = blackbox.position.x > 0 : 1 : -1; 
            // var directionY = blackbox.position.y > 0 : 1 : -1;
            // var directionZ = blackbox.position.z > 0 : 1 : -1;
                
            // 3: Calc Position
            Vector3 position = new Vector3( (selectedVertex.x + (magnitudeX * directionX)), (selectedVertex.y + (magnitudeY * directionY)), (selectedVertex.z + (magnitudeZ * directionZ)) );
            Debug.Log($"UVP Updated Vertex Position position: {position}");

            CheckBoxPlacementPhysics(position);

            targetBin.position = position;
        }
    }


    public void CheckBoxPlacementPhysics(Vector3 testPosition) {
        // create a clone test box to check physics of placement
        // teleported first before actual box is placed so gravity check comes before mesh combine

        // extra large cardboard boxes (~6dm * 6dm * 6dm) weight 1 kg, large boxes weight 0.5 kg, medium boxes ( < 3dm * 3dm * 3dm) weight 0.1kg
        //The maximum weights range from around 20 pounds for standard cardboard boxes to 60–150 pounds 
        //corrugated and double-walled boxes, with some corrugated triple-walled boxes carrying up to 300 pounds
        GameObject testBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Rigidbody rb = testBox.AddComponent<Rigidbody>();
        testBox.transform.localScale = new Vector3(boxWorldScale.x, boxWorldScale.y, boxWorldScale.z);
        // test position has to be slightly elevated or else raycast doesn't detect the layer directly below
        testBox.transform.position = new Vector3(testPosition.x, testPosition.y+0.1f, testPosition.z);
        //rb.MovePosition(testPosition);
        rb.constraints = RigidbodyConstraints.FreezeAll;
        // BoxCollider bc = testBox.GetComponent<BoxCollider>();
        // bc.isTrigger = false;
        // bc.center = Vector3.zero;
        // rb.mass = 300f;
        // rb.velocity = Vector3.zero;
        // rb.angularVelocity = Vector3.zero;
        // rb.drag = 1f;
        //rb.angularDrag = 2f;
        // rb.angularDrag = 1f;
        // bc.material.bounciness = 0f;
        // bc.material.dynamicFriction = 1f;
        // bc.material.staticFriction = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        sensorCollision = testBox.AddComponent<SensorCollision>();
        sensorCollision.agent = this;
        testBox.name = $"testbox{targetBox.name}";
        testBox.tag = "testbox";
    }


    /// <summary>
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int n) 
    {
        // Check if a box has already been selected
        if (!organizedBoxes.Contains(n))
        {
            boxIdx = n;
            Debug.Log($"Selected Box boxIdx: {boxIdx}");
            targetBox = boxPool[boxIdx].rb.transform;
            // Add box to list so it won't be selected again
            organizedBoxes.Add(boxIdx);
            isBoxSelected = true;
        }
    }


    public void UpdateBinVolume() 
    {
        // Update bin volume
        binVolume = binVolume-boxWorldScale.x * boxWorldScale.y * boxWorldScale.z;
        Debug.Log($"RBV Regular Bin Volume is binVolume: {binVolume}");
        
    }




    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>
    public void SelectRotation(int action) 
    {   
        var childrenList = targetBox.GetComponentsInChildren<Transform>();
        boxWorldScale = targetBox.localScale;
        if (action == 0 ) 
        {
            rotation = new Vector3(0, 0, 0);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
            }      
        }  
        else if (action==1) 
        {
            Debug.Log($"SelectRotation() called with rotation (90, 0, 0)");
            rotation = new Vector3(90, 0, 0);
            boxWorldScale = new Vector3(boxWorldScale[0], boxWorldScale[2], boxWorldScale[1]); // actual rotation of object transform
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                child.tag = "pickupbox";
                if (child.name=="bottom") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "top";
                }

                else if (child.name == "top") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "bottom";
                }
            }
        }
        else if (action==2) 
        {
            Debug.Log($"SelectRotation() called with rotation (0, 90, 0)");
            rotation = new Vector3(0, 90, 0);
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[1], boxWorldScale[0]); // actual rotation of object transform
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "back";
                }
                else if (child.name == "back") 
                {
                    child.name = "right";
                }

                else if (child.name == "right") 
                {
                    child.name = "front";
                }
                else if (child.name == "front") 
                {
                    child.name = "left";
                }
            }        
        }
        else if (action==3) 
        {
            Debug.Log($"SelectRotation() called with rotation (0, 0, 90)");
            rotation = new Vector3(0, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[0], boxWorldScale[2]); // actual rotation of object transform
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "top";
                }
                else if (child.name == "top") 
                {
                    child.name = "right";
                }

                else if (child.name == "right") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "left";
                }
            }
        }
        else if (action==4 ) 
        {
            Debug.Log($"SelectRotation() called with rotation (0, 90, 90)");
            rotation = new Vector3(0, 90, 90 ); 
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[0], boxWorldScale[1]); // actual rotation of object transform
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "top";
                }
                else if (child.name == "back") 
                {
                    child.name = "right";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "back";
                }

                else if (child.name == "right") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "front") 
                {
                    child.name = "left";
                }
                else if (child.name == "top") 
                {
                    child.name = "front";
                }
            }      
        }
        else 
        {
            Debug.Log($"SelectRotation() called with rotation (90, 0, 90)");
            rotation = new Vector3(90, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[2], boxWorldScale[0]); // actual rotation of object transform
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                child.tag = "pickupbox";
                if (child.name=="left") 
                {
                    child.name = "front";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "left";
                }
                else if (child.name == "back") 
                {
                    child.name = "top";
                }
                else if (child.name == "right") 
                {
                    child.name = "back";
                }
                else if (child.name == "top") 
                {
                    child.name = "right";
                }
                else if (child.name == "front") 
                {
                    child.name = "bottom";
                }
             }      
        }

        // /////// NOTE: No Vector3(90, 90, 90) or Vector3(90, 90, 0) rotations as
        //               // Vector3(90, 90, 90) == Vector3(90, 0, 0) == xzy
        //               // Vector3(90, 90, 0)  == Vector3(90, 0, 90) == yzx 

        Debug.Log($"SELECTED TARGET ROTATION FOR BOX {boxIdx}: {rotation}");
        isRotationSelected = true;
    }


    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() 
    {
            
        // Attach the box as a child of the agent parent, effectively attaching the box's movement to the agent's movement  
        targetBox.parent = this.transform;

        isPickedup = true;

        Destroy(targetBox.GetComponent<BoxCollider>()); 
        //Destroy(targetBox.GetComponent<Rigidbody>());   

        // Would be best if moved isCollidedColor=false state reset to StateReset(), but current issue
        GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().isCollidedGreen = false;
        GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().isCollidedBlue = false;
        GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().isCollidedRed = false;
        isBackMeshCombined = false;
        isBottomMeshCombined = false;
        isSideMeshCombined = false;
        isStateReset = false; // should be refactored into a end state reset function with isBlankMeshCombined's

        Debug.Log("PDB end of PickupBox()");
    }


    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox() 
    {
        // Detach box from agent, preventing the placed box from moving again when the agent moves to pickup a new box 
        targetBox.SetParent(null);

        Collider [] m_cList = targetBox.GetComponentsInChildren<Collider>();

        // Lock box position and location
        ///////////////////////COLLISION/////////////////////////
        targetBox.position = targetBin.position; // COLLISION OCCURS IMMEDIATELY AFTER SET POSITION OCCURS
        ///////////////////////COLLISION/////////////////////////

        targetBox.rotation = Quaternion.Euler(rotation);
        // dont need to freeze position on the rigidbody anymore because instead we just remove the rigidbody, preventing movement from collisions

        foreach (Collider m_c in m_cList) 
        {
            m_c.isTrigger = false;
            // m_c.gameObject.tag = "droppedoff";
        }

        isDroppedoff = true;

        Debug.Log($"PDB Box(): end of droppedoff function");
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



    public void ReverseSideNames(int id) 
    {
        var childrenList = boxPool[id].rb.gameObject.GetComponentsInChildren<Transform>();
        if (rotation==new Vector3(90, 0, 0))
        {
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                if (child.name=="bottom") 
                {
                    child.name = "front";
                }
                else if (child.name == "back") 
                {
                    child.name = "bottom";
                }

                else if (child.name == "top") 
                {
                    child.name = "back";
                }
                else if (child.name == "front") 
                {
                    child.name = "top";
                }
            }
        }
        else if (rotation == new Vector3(0, 90, 0)) 
        {
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                if (child.name=="left") 
                {
                    child.name = "front";
                }
                else if (child.name == "back") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "back";
                }
                else if (child.name == "front") 
                {
                    child.name = "right";
                }
            }        
        }
        else if (rotation == new Vector3(0, 0, 90))
        {
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                if (child.name=="left") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "top") 
                {
                    child.name = "left";
                }

                else if (child.name == "right") 
                {
                    child.name = "top";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "right";
                }
            }                
        }
        else if (rotation == new Vector3(0, 90, 90)) 
        {
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
                if (child.name=="back") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "right") 
                {
                    child.name = "back";
                }
                else if (child.name == "top") 
                {
                    child.name = "left";
                }
                else if (child.name == "front") 
                {
                    child.name = "top";
                }
                else if (child.name == "left") 
                {
                    child.name = "front";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "right";
                }

            }      
        }
        else if (rotation == new Vector3(90, 0, 90))
        {
            foreach (Transform child in childrenList) // only renames the side NAME to correspond with the rotation
            {
               if (child.name=="top") 
                {
                    child.name = "back";
                }
                else if (child.name == "left") 
                {
                    child.name = "bottom";
                }
                else if (child.name == "front") 
                {
                    child.name = "left";
                }
                else if (child.name == "bottom") 
                {
                    child.name = "front";
                }
                else if (child.name == "right") 
                {
                    child.name = "top";
                }
                else if (child.name == "back") 
                {
                    child.name = "right";
                }
             }      
        }
    }


    // BoxReset is called in SensorCollision.cs (currently bad practice not modular but will refactor when have time)
    public void BoxReset(string cause)
    {
        if (cause == "failedPhysicsCheck") 
        {
            Debug.Log($"SCS BOX {boxIdx} RESET LOOP");
            // detach box from agent
            targetBox.parent = null;
            // add back rigidbody and collider
            Rigidbody rb = boxPool[boxIdx].rb;
            BoxCollider bc = boxPool[boxIdx].rb.gameObject.AddComponent<BoxCollider>();
            // not be affected by forces or collisions, position and rotation will be controlled directly through script
            rb.isKinematic = true;
            // reset to starting position
            ReverseSideNames(boxIdx);
            rb.transform.rotation = boxPool[boxIdx].startingRot;
            rb.transform.position = boxPool[boxIdx].startingPos;
            // remove from organized list to be picked again
            organizedBoxes.Remove(boxIdx);
            // reset states
            StateReset();
            // settting isBlackboxUpdated to true allows another vertex to be selected
            isBlackboxUpdated = true;
            // setting isVertexSelected to true keeps the current vertex and allows another box to be selected
            // isVertexSelected = true;
        }
    }


    public void AgentReset() 
    {
        this.transform.position = initialAgentPosition; // Vector3 of agents initial transform.position
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }
    public void StateReset() 
    {
        isBlackboxUpdated = false;
        isVertexSelected = false;
        isBoxSelected = false;
        isRotationSelected = false;
        isPickedup = false;
        isDroppedoff = false;
        targetBin = null;
        targetBox = null;
        isStateReset = true;
    }

    public void MeshReset()
    {
        
        if (binBottom == null | binSide == null | binBack == null) {
            binBottom = GameObject.Find("BinIso20Bottom");
            binSide = GameObject.Find("BinIso20Side");
            binBack = GameObject.Find("BinIso20Back");
        }

        while (binBottom.transform.childCount > 2) 
        {
            DestroyImmediate(binBottom.transform.GetChild(binBottom.transform.childCount-1).gameObject);
        }   
        while (binSide.transform.childCount > 2) 
        {
            DestroyImmediate(binSide.transform.GetChild(binSide.transform.childCount-1).gameObject);
        }  
        while (binBack.transform.childCount > 1) 
        {
            DestroyImmediate(binBack.transform.GetChild(binBack.transform.childCount-1).gameObject);
        } 

    
        // // Combine meshes
        CombineMesh [] meshScripts = binArea.GetComponentsInChildren<CombineMesh>();
        foreach (CombineMesh meshScript in meshScripts) 
        {
            CombineMesh meshScriptInstance = new CombineMesh();
            var meshList = meshScript.GetComponentsInChildren<MeshFilter>();
            Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            meshScriptInstance.MeshCombiner(meshList, meshScript.gameObject); 
            meshScript.agent = this;
          
        }
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


    public void SetResetParameters()
    {
        // Reset mesh
        MeshReset();

        // Reset agent
        AgentReset();

        // Reset rewards
        TotalRewardReset();

        // Reset box pool 
        boxPool.Clear();

        // Reset organized Boxes list
        organizedBoxes.Clear();

        // Reset vertices array
        Array.Clear(verticesArray, 0, verticesArray.Length);

        // Reset states;
        StateReset();
    }


    /// <summary>
    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    /// A different reward system needs to be designed for each level
    /// </summary>
    void ConfigureAgent(int n) 
    {

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

