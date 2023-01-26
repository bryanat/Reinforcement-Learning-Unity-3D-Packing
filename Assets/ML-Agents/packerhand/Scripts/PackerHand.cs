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
    int curriculum_ConfigurationGlobal;  // Depending on this value, different curriculum will be picked
    int curriculum_ConfigurationLocal; // local reference of the above

    public NNModel unitBoxBrain;   // Brain to use when all boxes are 1 by 1 by 1
    public NNModel similarBoxBrain;     // Brain to use when boxes are of similar sizes
    public NNModel regularBoxBrain;     // Brain to use when boxes size vary

    string m_UnitBoxBehaviorName = "UnitBox"; // 
    string m_SimilarBoxBehaviorName = "SimilarBox";
    string m_RegularBoxBehaviorName = "RegularBox";

    public GameObject binArea; // The bin container, which will be manually selected in the Inspector

    //public GameObject binMini; // The mini bin container, used for lower lessons of Curriculum learning

    Rigidbody m_Agent; //cache agent rigidbody on initilization

    [HideInInspector] public Transform carriedObject; // local reference to box picked up by agent
    [HideInInspector] public Transform targetBox; // target box selected by agent
    [HideInInspector] public Transform targetBin; // phantom target bin object where the box will be placed

    public int boxIdx; // box selected from box pool
    public Vector3 rotation; // Rotation of box inside bin
    public Vector3 selectedVertex; // Vertex of box inside bin

    public List<Vector3> selectedVertices = new List<Vector3>();
    public Dictionary<Vector3, int > allVerticesDictionary = new Dictionary<Vector3, int>();
    public List<Vector3> backMeshVertices = new List<Vector3>();
    public List<Vector3> sideMeshVertices = new List<Vector3>();
    public List<Vector3> bottomMeshVertices = new List<Vector3>();
    public List<GameObject> blackbox_list; 

    public float total_x_distance; //total x distance between agent and target
    public float total_y_distance; //total y distance between agent and target
    public float total_z_distance; //total z distance between agent and target
    
    // public List<int> organizedBoxes = new List<int>(); // list of organzed box indices

    public Bounds areaBounds; // regular bin's bounds

    //public Bounds miniBounds; // mini bin's bounds

    public float binVolume; // regular bin's volume
    //public float miniBinVolume; // mini bin's volume

    EnvironmentParameters m_ResetParams; // Environment parameters
    public BoxSpawner boxSpawner; // Box Spawner

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

    public List<Vector3> allSelectedVertices;


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
        // curriculum_Configuration = 0;
        // m_config = 0;
        curriculum_ConfigurationGlobal = 2;
        curriculum_ConfigurationLocal = 2; // local copy of curriculum configuration number, global will change to -1 but need original copy for state management

        Renderer [] renderers = binArea.GetComponentsInChildren<Renderer>();
        areaBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; ++i)
            areaBounds.Encapsulate(renderers[i].bounds);

        Debug.Log($"BIN BOUNDS: {areaBounds}");

        // Get total bin volume from onstart
        binVolume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;
        //miniBinVolume = miniBounds.extents.x*2 * miniBounds.extents.y*2 * miniBounds.extents.z*2;

        Debug.Log($" BIN VOLUME: {binVolume}");

        CombineMesh [] meshScripts = binArea.GetComponentsInChildren<CombineMesh>();
        foreach (CombineMesh meshScript in meshScripts) 
        {
            meshScript.agent = this;
            binBottom = meshScript.binBottom;
            binBack = meshScript.binBack;       
            binSide = meshScript.binSide;
            
        }

        // Make agent unaffected by collision
        var m_c = GetComponent<CapsuleCollider>();
        m_c.isTrigger = true;

        // Reset agent and rewards
        SetResetParameters();

        selectedVertex = new Vector3(8.25f, 0.50f, 10.50f); // refactor to select first vertex
        isVertexSelected = true;
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        /////once the box combines with the bin, we should also add bin bounds and bin volumne to observation
        if (curriculum_ConfigurationLocal==0) 
        {
            // Add Bin size
            //sensor.AddObservation(binArea.transform.localScale);
        }

        else 
        {
            // Add Bin size
            sensor.AddObservation(binArea.transform.localScale);
        }

        // array of all boxes
        foreach (var box in boxPool) 
        {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            // sensor.AddObservation(box.rb.rotation); // add box rotation to sensor observations
        }

        // // array of vertices
        // sensor.AddObservation(verticesArray); //add vertices to sensor observations

        // // array of blackboxes 
        // sensor.AddObservation(blackboxesArray); //add vertices to sensor observations
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;
        //var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;

        if (isBlackboxUpdated && isVertexSelected == false) 
        {
            //SelectPosition(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            SelectVertex(); 
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
        // if (isBackMeshCombined && isBottomMeshCombined && isSideMeshCombined && targetBox!=null) 
        if (isBackMeshCombined && isBottomMeshCombined && isSideMeshCombined && isStateReset==false) 
        {
            StateReset();
            UpdateTriPoints();
            UpdateBinVolume();
            UpdateVertices();
            UpdateBlackBox();
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
            UpdateCarriedObject();
            //if agent is close enough to the position, it should drop off the box
            if ( Math.Abs(total_x_distance) < 2f && Math.Abs(total_z_distance) < 2f ) 
            {
                DropoffBox();
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
    }

    /// <summary>
    /// Updates the vertices every time a new mesh is created
    ///</summary>
    void UpdateVertices() 
    {
        ///////// this for now creates all vertices list and dictionary from scratch every time a new mesh is created/////
        //Transform [] binObjects = binArea.GetComponentsInChildren<Transform>();
        allVerticesDictionary.Clear();
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
            if (allVerticesDictionary.ContainsKey(worldVertex)) 
            {
                allVerticesDictionary[worldVertex] ++;
            }
            else 
            {
                allVerticesDictionary.Add(worldVertex, 1);
            }
        }
    }

    void UpdateTriPoints() 
    {
        selectedVertices.Clear();
        Vector3 V1= new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y, selectedVertex.z);
        Vector3 V2 = new Vector3(selectedVertex.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z);
        Vector3 V3 = new Vector3(selectedVertex.x, selectedVertex.y, selectedVertex.z+boxWorldScale.z);
        selectedVertices.Add(V1);
        selectedVertices.Add(V2);
        selectedVertices.Add(V3);
        /// allSelectedVertices list is for visualization of all tri points with gizmos, will be deleted after black box is done
        allSelectedVertices.Add(V1);
        allSelectedVertices.Add(V2);
        allSelectedVertices.Add(V3);
        Debug.Log($"SSV Selected Vertices :{selectedVertices[0]}");
        Debug.Log($"SSV Selected Vertices :{selectedVertices[1]}");
        Debug.Log($"SSV Selected Vertices :{selectedVertices[2]}");
    }


    public void UpdateBlackBox() 
    {
        Debug.Log($"UBX Update BlackboX running");

        foreach (Vector3 vertex in selectedVertices) 
        {
            //bottomVertices.Find(v=>  v[1]==vertex[1] && v[2]==vertex[2]).MinBy(v => Math.Abs(v[0]-vertex[0]));
            Vector3 closest_x_vertex = backMeshVertices.Aggregate(new Vector3(float.MaxValue, 0, 0), (min, next) => 
            vertex[0]<next[0] && Math.Abs(next[0]-vertex[0]) < Math.Abs(min[0] - vertex[0]) && next[1]==vertex[1] && next[2] == vertex[2] ? next : min);
            Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES X VERTEX IS {closest_x_vertex}");

            Vector3 closest_y_vertex = sideMeshVertices.Aggregate(new Vector3(0, float.MaxValue, 0), (min, next) => 
            vertex[1]<next[1] && Math.Abs(next[1]-vertex[1]) < Math.Abs(min[1] - vertex[1]) && next[0]==vertex[0] && next[2] == vertex[2] ? next : min);
            Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Y VERTEX IS {closest_y_vertex}");

            Vector3 closest_z_vertex = sideMeshVertices.Aggregate(new Vector3(0, 0, float.MaxValue), (min, next) => 
            vertex[2]<next[2] && Math.Abs(next[2]-vertex[2]) < Math.Abs(min[2] - vertex[2]) && next[1]==vertex[1] && next[0] == vertex[0] ? next : min);
            Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Z VERTEX IS {closest_z_vertex}");

            float blackbox_x_size = Math.Abs(closest_x_vertex[0] - vertex[0]);
            float blackbox_y_size = Math.Abs(closest_y_vertex[1] - vertex[1]);
            float blackbox_z_size = Math.Abs(closest_z_vertex[2] - vertex[2]);
            Vector3 blackbox_position = new Vector3(blackbox_x_size*0.5f+vertex[0], blackbox_y_size*0.5f+vertex[1], blackbox_z_size*0.5f+vertex[2]);
            Debug.Log($"BPS BLACK BOX POSITION {blackbox_position} SIZES {blackbox_x_size}, {blackbox_y_size}, {blackbox_z_size}");

            GameObject blackbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blackbox.name = "blackbox";
            blackbox.transform.position = blackbox_position;
            blackbox.transform.localScale = new Vector3(blackbox_x_size, blackbox_y_size, blackbox_z_size);
            Renderer cubeRenderer = blackbox.GetComponent<Renderer>();
            cubeRenderer.material = clearPlastic;

            blackbox_list.Add(blackbox);
        }
        isBlackboxUpdated = true;
    }


    public void SelectVertex() 
    {
        //// selectedVertex = math.max(allVertices)
        foreach(Vector3 vertex in selectedVertices) 
        {
            ///// right now it's returning the first vertex where all 3 meshes intersect
            selectedVertex = vertex; 
            Debug.Log($"SVX Selected VerteX is selectedVertex: {selectedVertex}");
            // selectedVertex = intersectingVertices[0]; // debug statement do not keep (final need something like: selectedVertex = vertex; )
        }
        isVertexSelected = true;
    }

    public void UpdateBoxPosition() 
    {
        // Packerhand.cs  : deals parent box : position (math)
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)

        if (targetBin==null) 
        {
            targetBin  = new GameObject().transform;

            //D: select position from A+B
            // 1: magnitude: magnitude = SELECTEDBOX.localScale * 0.5 : Vector3(0.5x, 0.5y, 0.5z) : half of each x,y,z (magnitudeX = SELECTEDBOX.localScale.x * 0.5; magnitudeY = SELECTEDBOX.localScale.y * 0.5; magnitudeZ = SELECTEDBOX.localScale.z * 0.5; )
            // 2: direction: directionX = blackbox.position.x.isPositive (true=1 or false=-1), directionY = blackbox.position.y.isPositive, directionZ = blackbox.position.z.isPositive
            // 3: position: (1+2=3) = Vector3( (selectedVertex.x + (magnitudeX * directionX)), (selectedVertex.y + (magnitudeY * directionY)), (selectedVertex.z + (magnitudeZ * directionZ)) )

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

            targetBin.position = position;
        }
    }


    /// <summary>
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int n) 
    {
        // Check if a box has already been selected
        if (!Box.organizedBoxes.Contains(n))
        {
            boxIdx = n;
            Debug.Log($"Selected Box boxIdx: {boxIdx}");
            targetBox = boxPool[boxIdx].rb.transform;
            // Add box to list so it won't be selected again
            Box.organizedBoxes.Add(boxIdx);
            isBoxSelected = true;
        }
    }


    public void UpdateBinVolume() 
    {
        // Update bin volume
        if (curriculum_ConfigurationLocal==0) 
        {
            // miniBinVolume = miniBinVolume - carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;
            //  Debug.Log($"MINI BIN VOLUME IS {miniBinVolume}");
        }
        else 
        {
            binVolume = binVolume-carriedObject.localScale.x*carriedObject.localScale.y*carriedObject.localScale.z;
            Debug.Log($"RBV Regular Bin Volume is binVolume: {binVolume}");
        }
        
    }


    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>
    public void SelectRotation(int action) 
    {   
        var childrenList = carriedObject.GetComponentsInChildren<Transform>();
        boxWorldScale = carriedObject.localScale;
        Debug.Log($"ROTATION SELECTED IS : {action}");
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
            
        // Attach carriedObject (the box) as a child of the agent parent, effectively attaching the box's movement to the agent's movement  
        carriedObject.parent = this.transform;

        isPickedup = true;

        Destroy(carriedObject.GetComponent<BoxCollider>());
        Destroy(carriedObject.GetComponent<Rigidbody>());   

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
        carriedObject.SetParent(null);

        Collider [] m_cList = carriedObject.GetComponentsInChildren<Collider>();

        // Lock box position and location
        ///////////////////////COLLISION/////////////////////////
        carriedObject.position = targetBin.position; // COLLISION OCCURS IMMEDIATELY AFTER SET POSITION OCCURS
        ///////////////////////COLLISION/////////////////////////

        carriedObject.rotation = Quaternion.Euler(rotation);
        // carriedObject.Rotate(rotation[0], rotation[1], rotation[2], Space.World);
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

