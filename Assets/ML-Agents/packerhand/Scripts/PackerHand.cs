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
using Bins;


public class PackerHand : Agent 
{
    public int curriculum_ConfigurationGlobal;  // Depending on this value, different curriculum will be picked
    int curriculum_ConfigurationLocal; // local reference of the above
    public int packSpeed = 20;
    public int seed = 123; // same seed means same set of randomly generated boxes
    public string file_name = "Boxes_30"; // jason file name used in non-curriculum such as production, bin and box information will be read this file
    public string box_type = "mix"; // box type options: "mix", "uniform" (for curriculum)
    public string bin_type = "biniso20"; // bin type options: "random", "biniso20" (for curriculum) // future: add "pallet"
    public int bin_quantity = 1; // bin quantities (for curriculum)
    
    public bool runInference=false; 
    public bool useCurriculum=true; // if false, bin and box sizes and quantity will be read from a json file 
    public bool useAttention=true; // if use attention (default = true)
    public bool useDenseReward=true;

    public bool useBoxReset=false; // if reset box when fails physics test and continue the episode (default=false, which means episode will restart)
    public bool useDiscreteSolution = true;

    BufferSensorComponent m_BufferSensor; // attention sensor
    StatsRecorder m_statsRecorder; // adds stats to tensorboard

    public NNModel discreteBrain;   // Brain to use when all boxes are 1 by 1 by 1
    string m_DiscreteBehaviorName = "Discrete"; 

    EnvironmentParameters m_ResetParams; // Environment parameters
    [HideInInspector] Rigidbody m_Agent; //cache agent rigidbody on initilization
    [HideInInspector] public Vector3 initialAgentPosition; 
    [HideInInspector] public Transform targetBox; // target box selected by agent
    [HideInInspector] public Transform targetBin; // phantom target bin object where the box will be placed

    public int selectedBoxIdx; // Box selected 
    public Vector3 selectedRotation; // Rotation selected
    public Vector3 selectedVertex; // Vertex selected
    public int selectedBin;  // Bin selected (not part of action selection)
    public Vector4 [] verticesArray; // space: 3n Vector3 vertices where n = max num boxes
    int selectedVertexIdx = -1; 
    int VertexCount = 0; // counter for VerticesArray
    int origin_counter; // used to count origin vertices when populating origin boxes
    [HideInInspector] public List<Box> boxPool; 
    [HideInInspector] public List<int> maskedVertexIndices; // list of taken vertex indices
    [HideInInspector] public List<int> maskedBoxIndices; // list of organzed box indices
    [HideInInspector] public List<Vector3> historicalVerticesLog; // list of all used vertices
    [HideInInspector] public Vector3 boxWorldScale; //local scale of selected box
    float total_x_distance; //total x distance between agent and target
    float total_y_distance; //total y distance between agent and target
    float total_z_distance; //total z distance between agent and target
    
    public BoxSpawner boxSpawner; // Box Spawner
    public BinSpawner binSpawner; // Bin Spawner
    [HideInInspector] public SensorCollision sensorCollision; // cache script for checking gravity
    [HideInInspector] public SensorOuterCollision sensorOuterCollision; // cache script for checking protrusion
    [HideInInspector] public SensorOverlapCollision sensorOverlapCollision; // cache script for checking overlap
    [HideInInspector] public bool isAfterInitialization = false;
    [HideInInspector] public bool isEpisodeStart;
    [HideInInspector] public bool isAfterOriginVertexSelected;
    [HideInInspector] public bool isBoxSelected;
    [HideInInspector] public bool isBoxPlacementChecked;
    [HideInInspector] public bool isPickedup;
    [HideInInspector] public bool isDroppedoff;
    [HideInInspector] public bool isStateReset;
    [HideInInspector] public bool isBottomMeshCombined;
    [HideInInspector] public bool isSideMeshCombined;
    [HideInInspector] public bool isBackMeshCombined;
    public float total_bin_volume; // sum of all bins' volume
    public float total_bin_surface_area;
    float current_bin_volume;
    float current_bin_surface_area;
    public float percent_filled_bin_volume;
    public float percent_filled_bin_surface_area;
    public int boxes_packed;




    public override void Initialize()
    {   
        // switching off automatic brain stepping for manual control
        Academy.Instance.AutomaticSteppingEnabled = false;

        // local copy of curriculum configuration number, global will change to -1 but need original copy for state management
        curriculum_ConfigurationLocal = curriculum_ConfigurationGlobal; 
        
        // initialize stats recorder to add stats to tensorboard
        m_statsRecorder = Academy.Instance.StatsRecorder;

        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        // store initial agent position
        initialAgentPosition = m_Agent.position;

        // Set environment parameters
        m_ResetParams = Academy.Instance.EnvironmentParameters;


        // Update model references if we're overriding
        var modelOverrider = GetComponent<ModelOverrider>();
        if (modelOverrider.HasOverrides && useCurriculum)
        {
            discreteBrain = modelOverrider.GetModelForBehaviorName(m_DiscreteBehaviorName);
            m_DiscreteBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_DiscreteBehaviorName);

        }

        // Make agent unaffected by collision
        CapsuleCollider m_c = GetComponent<CapsuleCollider>();
        m_c.isTrigger = true;

        // Set up bins
        if (!useCurriculum)
        {
            binSpawner.SetUpBins(file_name);
        }
        else 
        {
            binSpawner.SetUpBins(bin_type, bin_quantity, seed);
        }
        // Get bin info
        total_bin_volume = binSpawner.total_bin_volume;
        total_bin_surface_area = binSpawner.total_bin_surface_area;
        origin_counter = binSpawner.total_bin_num;
        // initalize mesh scripts' agent
        foreach (CombineMesh script in binSpawner.m_BackMeshScripts)
        {
            script.agent = this;
        }
        foreach (CombineMesh script in binSpawner.m_SideMeshScripts)
        {
            script.agent = this;
        }
        foreach (CombineMesh script in binSpawner.m_BottomMeshScripts)
        {
            script.agent = this;
        }

        if (useAttention){
            m_BufferSensor = GetComponent<BufferSensorComponent>();
        }

        isEpisodeStart = true;

        //Debug.Log("INITIALIZE ENDS");
    }


    public override void OnEpisodeBegin()
    {   
        //Debug.Log("-----------------------NEW EPISODE STARTS------------------------------");
      
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        //Debug.Log("OBSERVATION");
        // Add updated bin volume
        sensor.AddObservation(percent_filled_bin_volume);
        sensor.AddObservation(percent_filled_bin_surface_area);

        int j = 0;
        maskedBoxIndices = new List<int>();
        // Add all boxes sizes (selected boxes have sizes of 0s)
        foreach (Box box in boxPool) 
        {   
            if (useAttention){
                // Used for variable size observations
                float[] listVarObservation = new float[boxSpawner.maxBoxQuantity+9];
                int boxNum = int.Parse(box.rb.name);
                // The first boxPool.Count are one hot encoding of the box
                listVarObservation[boxNum] = 1.0f;
                // Add box dimensions, updated after placements 
                // box size is zero after placement since this box cannot be placed again
                listVarObservation[boxSpawner.maxBoxQuantity] = box.boxSize.x;
                listVarObservation[boxSpawner.maxBoxQuantity +1] = box.boxSize.y;
                listVarObservation[boxSpawner.maxBoxQuantity +2] = box.boxSize.z;
                // relative ratio of placed box and bin size will appear after placement
                listVarObservation[boxSpawner.maxBoxQuantity +3] = box.boxBinScale.x;
                listVarObservation[boxSpawner.maxBoxQuantity +4] = box.boxBinScale.y;
                listVarObservation[boxSpawner.maxBoxQuantity +5] = box.boxBinScale.z;
                
                //Debug.Log($"XVD box:{box.rb.name}  |  vertex:{box.boxVertex}  |  x: {box.boxVertex.x * 23.5}  |  y: {box.boxVertex.y * 23.9}  |  z: {box.boxVertex.z * 59}");
                //Debug.Log($"XVB box:{box.rb.name}  |  vertex:{box.boxVertex}  |  dx: {scaled_continuous_boxsize.x*23.5}  |  dy: {scaled_continuous_boxsize.y*23.9}  |  dz: {scaled_continuous_boxsize.z*59}");
                //Debug.Log($"XVR box:{box.rb.name}  |  vertex:{box.boxVertex}  |  1: {box.boxRot[0]}  |  2: {box.boxRot[1]}  |  3: {box.boxRot[2]} | 4: {box.boxRot[3]}");
                // Add scaled vertex, (0, 0, 0) before placement
                // listVarObservation[boxSpawner.maxBoxQuantity +6] = box.boxVertex.x;
                // listVarObservation[boxSpawner.maxBoxQuantity +7] = box.boxVertex.y;
                // listVarObservation[boxSpawner.maxBoxQuantity +8] = box.boxVertex.z;
                // Add rotation,  (0, 0, 0) before placement
                // listVarObservation[boxSpawner.maxBoxQuantity+9] = box.boxRot[0];
                // listVarObservation[boxSpawner.maxBoxQuantity+10] = box.boxRot[1];
                // listVarObservation[boxSpawner.maxBoxQuantity+11] = box.boxRot[2];
                // Add [box volume]/[bin volume] 
                listVarObservation[boxSpawner.maxBoxQuantity +6] = (box.boxSize.x* box.boxSize.y *box.boxSize.z)/(total_bin_volume);
                // Add [box surface area]/[bin surface area]
                listVarObservation[boxSpawner.maxBoxQuantity +7] = (2*box.boxSize.x*box.boxSize.y + 2*box.boxSize.z*box.boxSize.x + 2*box.boxSize.y*box.boxSize.z)/(total_bin_surface_area);
                // Add if box is placed already: 1 if placed already and 0 otherwise
                listVarObservation[boxSpawner.maxBoxQuantity +8] = box.isOrganized ? 1.0f : 0.0f;
                m_BufferSensor.AppendObservation(listVarObservation);
            }
            else{

                // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                // Add scaled box size 
                sensor.AddObservation(box.boxSize);
                // Add updated [volume]/[w*h*l] added to state vector
                //sensor.AddObservation( (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z) );
                // Add scaled vertex
                // Add associated bin dimensions
                sensor.AddObservation(box.boxBinScale);
                // if box has not been placed, they'll be zeros
                sensor.AddObservation (box.boxVertex);
                sensor.AddObservation(box.boxRot);
                // Add if box is placed or not
                sensor.AddObservation(box.isOrganized ? 1.0f : 0.0f);
            }
            // add placed boxes to action ask
            if (box.isOrganized)
            {
                maskedBoxIndices.Add(j);
                //Debug.Log($"ORGANIZED BOX LIST SELECTED BOX IS: {j}");
            }
            j++;
        }

        // add all zero padded boxes to action mask
        for (int m=boxPool.Count(); m< boxSpawner.maxBoxQuantity; m++)
        {
            // Debug.Log($"MASK ZERO PADDING {m}");
            maskedBoxIndices.Add(m);
        }

        // Add array of vertices (selected vertices are 0s)
        int i = 0;
        maskedVertexIndices = new List<int>();
        foreach (Vector4 vertex in verticesArray) 
        {   
            Vector3 scaled_vertex = new Vector3(vertex.x, vertex.y, vertex.z);
            //Debug.Log($"XYX scaled_continuous_vertex: {scaled_continuous_vertex}");
            if (!useAttention)
            {
                sensor.AddObservation(scaled_vertex);
            }
            // origins after scaled and selected vertices will be (0, 0, 0)
            // since cannot be selected again, they will be masked in action space
            if (scaled_vertex == Vector3.zero)
            {
                //Debug.Log($"MASK VERTEX LOOP INDEX:{i}");
                maskedVertexIndices.Add(i);
            }
            i++;
        }
        
    }


    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // vertices action mask
        if (useDiscreteSolution)
        {
            if (isAfterOriginVertexSelected) {
                foreach (int vertexIdx in maskedVertexIndices) 
                {
                    //Debug.Log($"MASK VERTEX {vertexIdx}");
                    actionMask.SetActionEnabled(1, vertexIdx, false);
                }
            }
        }
        // box action mask
        foreach (int selectedBoxIdx in maskedBoxIndices)
        {
            //Debug.Log($"MASK BOX {selectedBoxIdx}");
            actionMask.SetActionEnabled(0, selectedBoxIdx, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Debug.Log("ACTION");
        var j = -1;

        var discreteActions = actionBuffers.DiscreteActions;

        SelectBox(discreteActions[++j]); 
        SelectVertex(discreteActions[++j]);
        SelectRotation(discreteActions[++j]);
    }


    /// <summary>
    /// This function is called at every time step
    ///</summary>
    void FixedUpdate() 
    {
        // if (runInference)
        // {
        //     if (CompletedEpisodes==1)
        //     {
        //         binSpawner.ExportBins();
        //         // stop mlagents-learn
        //     }
        // }
        // if all boxes packed, reset episode
        if (maskedBoxIndices.Count == boxSpawner.maxBoxQuantity)
        {
            if (!useDenseReward)
            {
                AddReward(percent_filled_bin_volume*10);
                //Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
                AddReward(percent_filled_bin_surface_area);
            }
                EndEpisode();
            curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;
            isEpisodeStart = true;
            Debug.Log($"ALL BOXES PACKED!!! EPISODE {CompletedEpisodes} START TRUE AFTER ALL BOXES PACKED");
        }
        // to reset episode manually, need to reset a little before MaxStep is reached, or else episode will be reset automatically
        // (this loop is not reached if conditional check is StepCount >= MaxStep)
        if (StepCount >= MaxStep-5) 
        {
            if (!useDenseReward)
            {
                AddReward(percent_filled_bin_volume*10);
                //Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
                AddReward(percent_filled_bin_surface_area);
            }
            EndEpisode();
            curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;
            isEpisodeStart = true;
            // Debug.Log($"EPISODE {CompletedEpisodes}  START TRUE AFTER MAXIMUM STEP");
        }
        // start of episode
        if (isEpisodeStart)
        {
            isEpisodeStart = false;

            // Reset agent and rewards
            SetResetParameters();

            // Initialize curriculum and brain
            if (useCurriculum)
            {
                if (curriculum_ConfigurationGlobal != -1)
                {
                    ConfigureAgent(curriculum_ConfigurationGlobal);
                    curriculum_ConfigurationGlobal = -1;
                    isAfterInitialization = true;
                }
            }
            else
            {
                boxSpawner.SetUpBoxes(file_name);
                isAfterInitialization = true;
            }
            
            // initialize local reference to box pool
            boxPool = boxSpawner.boxPool;

            // // initialize total box number
            // total_box_number = boxPool.Count();

            isAfterOriginVertexSelected = false;
            //Debug.Log("REQUEST DECISION AT START OF EPISODE"); 
            GetComponent<Agent>().RequestDecision(); 
            Academy.Instance.EnvironmentStep();

            // overrides brain selected first box position
            // discrete or not, first box will be placed at a bin's origin
            origin_counter--;
            selectedVertex = binSpawner.origins[origin_counter];
        }
        // if meshes are combined, reset states and go for next round of box selection 
        if ((isBackMeshCombined | isBottomMeshCombined | isSideMeshCombined) && isStateReset==false) 
        {
            // Reset states for next round of picking
            StateReset();

            // Update vertices array 
            if (useDiscreteSolution)
            {
                UpdateVerticesArray();
            }

            // recalculate bin surface area and percent filled
            current_bin_surface_area = current_bin_surface_area - sensorCollision.totalContactSA;
            percent_filled_bin_surface_area = (1-(current_bin_surface_area/total_bin_surface_area))*100;

            // recalculate bin volume and percent filled
            current_bin_volume = current_bin_volume - (boxWorldScale.x * boxWorldScale.y * boxWorldScale.z);
            percent_filled_bin_volume = (1 - (current_bin_volume/total_bin_volume)) * 100;

            // Add volume reward
            if (useDenseReward)
            {
                AddReward(((boxWorldScale.x * boxWorldScale.y * boxWorldScale.z)/total_bin_volume) * 1000f);
                //Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{((boxWorldScale.x * boxWorldScale.y * boxWorldScale.z)/total_bin_volume) * 1000f} reward | current_bin_volume: {current_bin_volume} | percent bin filled: {percent_filled_bin_volume}%");
                AddReward(sensorCollision.totalContactSA/total_bin_surface_area*100f);
            }
            
            // Increment stats recorder to match reward
            m_statsRecorder.Add("% Bin Volume Filled", percent_filled_bin_volume, StatAggregationMethod.Average);

            // REGUEST DECISION FOR NEXT ROUND OF PICKING
            if (origin_counter<=0) 
            {
                isAfterOriginVertexSelected = true;
            }

            GetComponent<Agent>().RequestDecision();
            Academy.Instance.EnvironmentStep();

            // override brain selected origin boxes positions
            // discrete or not, the origin box placements will be forced
            if (isAfterOriginVertexSelected==false)
            {
                origin_counter--;
                selectedVertex = binSpawner.origins[origin_counter];
            }
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
        else if (isPickedup && isDroppedoff == false) 
        {
            UpdateBoxPosition();
            UpdateAgentPosition(targetBin);
            UpdateTargetBox();
            //if agent is close enough to the position, it should drop off the box
            if (isBoxPlacementChecked && Math.Abs(total_x_distance) < 2f && Math.Abs(total_z_distance) < 2f) 
            {
                // if box passes all physics test, it will be placed
                if (sensorCollision.passedGravityCheck && sensorOuterCollision.passedBoundCheck && sensorOverlapCollision.passedOverlapCheck)
                {
                    DropoffBox();
                    boxes_packed++;
                }
                else
                {
                    // if box fails physics test and to be repacked, it will be reset and episde will continue
                    if (useBoxReset)
                    {
                        BoxReset("failedPhysicsCheck");
                    }
                    // if not to be repacked, episode will end
                    else
                    {
                        if (useDenseReward)
                        {
                            AddReward(-100f);
                        }
                        else
                        {
                            AddReward(percent_filled_bin_volume*10);   
                            //Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
                            AddReward(percent_filled_bin_surface_area);
                        }
                        EndEpisode();
                        curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;
                        isEpisodeStart = true;
                        //Debug.Log($"EPISODE {CompletedEpisodes} START TRUE AFTER FAILING PHYSICS TEST");
                    }
                }
            }
        }

        else { return;}
    }
    

    /// <summary>
    /// Updates agent position relative to the target position
    ///</summary>
    void UpdateAgentPosition(Transform target) 
    {
        total_x_distance = target.position.x- m_Agent.position.x;
        total_y_distance = target.position.y- m_Agent.position.y;
        total_z_distance = target.position.z- m_Agent.position.z;
        var current_agent_x = m_Agent.position.x;
        var current_agent_y = m_Agent.position.y;
        var current_agent_z = m_Agent.position.z;   
        this.transform.position = new Vector3(current_agent_x + total_x_distance/packSpeed, 
        target.position.y, current_agent_z + total_z_distance/packSpeed);   

    }


    /// <summary>
    /// Update carried object position relative to the agent position
    ///</summary>
    void UpdateTargetBox() 
    {   
         // distance from agent is relative to the box size
        targetBox.localPosition = new Vector3(0,1,1);
        // stop box from rotating
        targetBox.rotation = Quaternion.identity;
    }

    void UpdateVerticesArray() 
    {
        List<Vector3> tripoints_list = new List<Vector3>();
        var tripoint_redx = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y, selectedVertex.z); // x red side tripoint
        var tripoint_greeny = new Vector3(selectedVertex.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z); // y green bottom tripoint 
        var tripoint_bluez = new Vector3(selectedVertex.x, selectedVertex.y, selectedVertex.z+boxWorldScale.z); // z blue back tripoint 

        tripoints_list.Add(tripoint_redx);   
        tripoints_list.Add(tripoint_greeny);
        tripoints_list.Add(tripoint_bluez);


        for (int idx = 0; idx<tripoints_list.Count(); idx++) 
        {
            //Debug.Log($"TPB tripoints_list[idx]: {tripoints_list[idx]} | areaBounds.min: {areaBounds.min} | areaBounds.max: {areaBounds.max} ");
            // only if historicVerticesArray doesnt already contain the tripoint, add it to the verticesArray
            Vector3 scaled_continuous_vertex = new Vector3((tripoints_list[idx].x - binSpawner.origins[selectedBin].x)/binSpawner.binscales_x[selectedBin],  (tripoints_list[idx].y - binSpawner.origins[selectedBin].y)/binSpawner.binscales_y[selectedBin],  (tripoints_list[idx].z - binSpawner.origins[selectedBin].z)/binSpawner.binscales_z[selectedBin]);
            //Debug.Log($"VACx historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false: {historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false} | scaled_continuous_vertex: {scaled_continuous_vertex} ");
            if ( historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false )
            {
                // Debug.Log($"TPX idx:{idx} | tripoint add to tripoints_list[idx]: {tripoints_list[idx]} | selectedVertex: {selectedVertex}") ;
                // Add scaled tripoint_vertex to verticesArray
                verticesArray[VertexCount] = new Vector4(scaled_continuous_vertex.x, scaled_continuous_vertex.y, scaled_continuous_vertex.z, selectedBin);
                historicalVerticesLog.Add(scaled_continuous_vertex);
                VertexCount ++;
                //Debug.Log($"VERTEX COUNT IS {VertexCount}");
            }
        }
    }


    public void SelectVertex(int action_SelectedVertexIdx) 
    {
        // assign selected vertex where next box will be placed, selected from brain's actionbuffer (inputted as action_SelectedVertex)
        selectedVertexIdx = action_SelectedVertexIdx;
        // get scaled vertex from vertices array
        Vector3 scaled_selectedVertex = new Vector3(verticesArray[action_SelectedVertexIdx].x, verticesArray[action_SelectedVertexIdx].y, verticesArray[action_SelectedVertexIdx].z);
        // selected bin is stored as 4th vector space (w) in vertices array for non-origin vertices
        if (origin_counter<=0)
        {
            selectedBin = Mathf.RoundToInt(verticesArray[action_SelectedVertexIdx].w);
        }
        // since origin vertices are (0,0,0) and are not added into vertices array, information about which bin for origin vertices has to be specified
        else
        {
            selectedBin = origin_counter-1;
        }
        // store updated box info
        boxPool[selectedBoxIdx].boxVertex = scaled_selectedVertex;
        boxPool[selectedBoxIdx].isOrganized = true;
        // selected vertex is unscaled vertex
        selectedVertex =  new Vector3(((scaled_selectedVertex.x* binSpawner.binscales_x[selectedBin]) + binSpawner.origins[selectedBin].x), ((scaled_selectedVertex.y* binSpawner.binscales_y[selectedBin]) + binSpawner.origins[selectedBin].y), ((scaled_selectedVertex.z* binSpawner.binscales_z[selectedBin]) + binSpawner.origins[selectedBin].z));
        // Debug.Log($"SVB selected vertex is {selectedVertex}");
        // Debug.Log($"SVB selected bin is {selectedBin}");
    }


    public void UpdateBoxPosition() 
    {
        // Packerhand.cs  : deals parent box : position (math)
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)

        if (targetBin==null) 
        { 
            // initialize the reference to where box is to be placed
            targetBin  = new GameObject().transform;

            // 1. Magnitude: size of box after rotation
            float magnitudeX = boxWorldScale.x * 0.5f; 
            float magnitudeY = boxWorldScale.y * 0.5f; 
            float magnitudeZ = boxWorldScale.z * 0.5f; 
            // 2: Direction
            int directionX = 1; 
            int directionY = 1;
            int directionZ = 1;       
            // 3: Position: the center position where the box will be placed inside bin
            Vector3 position = new Vector3( (selectedVertex.x + (magnitudeX * directionX)), (selectedVertex.y + (magnitudeY * directionY)), (selectedVertex.z + (magnitudeZ * directionZ)) );
            //Debug.Log($"UVP Updated Vertex Position position: {position}");

            // Physics test for box
            CheckBoxPlacementPhysics(position);

            // set reference's position to box's placement position
            targetBin.position = position;
        }
    }


    public void CheckBoxPlacementPhysics(Vector3 testPosition) 
    {
        // create a clone test box to check physics of placement
        // teleported first before actual box is placed so gravity check comes before mesh combine

        GameObject testBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Rigidbody rb = testBox.AddComponent<Rigidbody>();
        testBox.transform.localScale = boxWorldScale;
        // test position has to be slightly elevated or else raycast doesn't detect the layer directly below
        testBox.transform.position = new Vector3(testPosition.x, testPosition.y+0.1f, testPosition.z);
        rb.constraints = RigidbodyConstraints.FreezeAll;
        // rb.mass = 300f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        sensorCollision = testBox.AddComponent<SensorCollision>();
        // probably don't need agent  in the scripts
        sensorCollision.agent = this; // agent reference used by component to set rewards on collision
        testBox.name = $"testbox{targetBox.name}";
        testBox.tag = "testbox";
        
        // Setup child test boxes for physics check which check overlapping boxes (impossible placements)
        GameObject testBoxChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Rigidbody rbChild = testBoxChild.AddComponent<Rigidbody>();
        // make child test box slightly smaller than parent test box, used to detect overlapping boxes on collision in SensorOverlapCollision.cs
        testBoxChild.transform.localScale = new Vector3((boxWorldScale.x - 0.15f), (boxWorldScale.y - 0.15f), (boxWorldScale.z - 0.15f));
        testBoxChild.transform.position = testPosition;
        rbChild.constraints = RigidbodyConstraints.FreezeAll;
        rbChild.interpolation = RigidbodyInterpolation.Interpolate;
        sensorOverlapCollision = testBoxChild.AddComponent<SensorOverlapCollision>();
        sensorOuterCollision = testBoxChild.AddComponent<SensorOuterCollision>();
        sensorOverlapCollision.agent = this; // agent reference used by component to set rewards on collision
        sensorOuterCollision.agent = this; // agent reference used by component to set rewards on collision
        testBoxChild.name = $"testboxChild{targetBox.name}";
        testBoxChild.tag = "testboxChild";

        isBoxPlacementChecked = true;
    }


    /// <summary>
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int action_SelectedBox) 
    {
        selectedBoxIdx = action_SelectedBox;
        targetBox = boxPool[selectedBoxIdx].rb.transform;
        isBoxSelected = true;
        //Debug.Log($"SBB Selected Box selectedBoxIdx: {selectedBoxIdx}");
    }


    /// <summary>
    /// Agent selects rotation for the box
    /// </summary>
    public void SelectRotation(int action) 
    {   
        // get sides of the boxes
        var sidesList = targetBox.GetComponentsInChildren<Transform>();
        // box world scale is the selected box's local scale which will be adjusted according to rotation
        boxWorldScale = targetBox.localScale;
        // Rotation: (0, 0, 0)
        if (action == 0 ) 
        {
            selectedRotation = new Vector3(0, 0, 0);
            // store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList)
            {
                child.tag = "pickupbox";
            }      
        }  
        // Rotation: (90 ,0, 0)
        else if (action==1) 
        {
            //Debug.Log($"SelectRotation() called with rotation (90, 0, 0)");
            selectedRotation = new Vector3(90, 0, 0);
            boxWorldScale = new Vector3(boxWorldScale[0], boxWorldScale[2], boxWorldScale[1]); // actual rotation of object transform
            // store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        // Rotation: (0, 90, 0)
        else if (action==2) 
        {
            //Debug.Log($"SelectRotation() called with rotation (0, 90, 0)");
            selectedRotation = new Vector3(0, 90, 0);
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[1], boxWorldScale[0]); // actual rotation of object transform
            // store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        // Rotation: (0, 0, 90)
        else if (action==3) 
        {
            //Debug.Log($"SelectRotation() called with rotation (0, 0, 90)");
            selectedRotation = new Vector3(0, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[0], boxWorldScale[2]); // actual rotation of object transform
            /// store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        // Rotation: (0, 90, 90)
        else if (action==4 ) 
        {
            //Debug.Log($"SelectRotation() called with rotation (0, 90, 90)");
            selectedRotation = new Vector3(0, 90, 90 ); 
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[0], boxWorldScale[1]); // actual rotation of object transform
            // store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        // Rotation: (90, 0, 90)
        else 
        {
            //Debug.Log($"SelectRotation() called with rotation (90, 0, 90)");
            selectedRotation = new Vector3(90, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[2], boxWorldScale[0]); // actual rotation of object transform
            // store updated box info
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxBinScale = new Vector3(boxWorldScale.x/binSpawner.binscales_x[selectedBin], boxWorldScale.y/binSpawner.binscales_y[selectedBin], boxWorldScale.z/binSpawner.binscales_z[selectedBin]);
            boxPool[selectedBoxIdx].boxSize = Vector3.zero;
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
    }


    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() 
    {
        // Attach the box as a child of the agent parent, effectively attaching the box's movement to the agent's movement  
        targetBox.parent = m_Agent.transform;

        isPickedup = true;

        Destroy(targetBox.GetComponent<BoxCollider>());  

        // Would be best if moved isCollidedColor=false state reset to StateReset(), but current issue
        // find the bin that the box is put in
        for (int i=0; i< binSpawner.m_BackMeshScripts.Count; i++) {
            binSpawner.m_BackMeshScripts[i].isBoxPlaced = false;
            binSpawner.m_BottomMeshScripts[i].isBoxPlaced = false;
            binSpawner.m_SideMeshScripts[i].isBoxPlaced = false;
        }
        isBackMeshCombined = false;
        isBottomMeshCombined = false;
        isSideMeshCombined = false;
        isStateReset = false; // should be refactored into a end state reset function with isBlankMeshCombined's

        //Debug.Log("PDB end of PickupBox()");
    }


    /// <summmary>
    //// Agent drops off the box
    /// </summary>
    public void DropoffBox() 
    {
        // Detach box from agent, preventing the placed box from moving again when the agent moves to pickup a new box 
        targetBox.SetParent(null);

        Collider [] m_cList = targetBox.GetComponentsInChildren<Collider>().Skip(1).ToArray();

        foreach (Collider m_c in m_cList) 
        {
            m_c.isTrigger = false;
        }
        // Lock box position and location
        ///////////////////////COLLISION/////////////////////////
        targetBox.position = targetBin.position; // COLLISION OCCURS IMMEDIATELY AFTER SET POSITION OCCURS
        ///////////////////////COLLISION/////////////////////////

        targetBox.rotation = Quaternion.Euler(selectedRotation);
        // dont need to freeze position on the rigidbody anymore because instead we just remove the rigidbody, preventing movement from collisions

        isDroppedoff = true;

        //Debug.Log($"PDB Box(): end of droppedoff function");
    }




    public void AgentReset() 
    {
        m_Agent.position = initialAgentPosition;
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }


    public void StateReset() 
    {
        // remove consumed selectedVertex from verticesArray (since another box cannot be placed there)
        if (isBackMeshCombined | isSideMeshCombined | isBottomMeshCombined) 
        {
            if (useDiscreteSolution)
            {
                if (isAfterOriginVertexSelected)
                {
                    //Debug.Log($"SRS SELECTED VERTEX IDX {selectedVertexIdx} RESET");
                    verticesArray[selectedVertexIdx] = Vector3.zero;               
                }
            }
        }
        isBoxSelected = false;
        isBoxPlacementChecked = false;
        isPickedup = false;
        isDroppedoff = false;
        if (targetBin!=null)
        {
            DestroyImmediate(targetBin.gameObject);
        }
        targetBox = null;
        //outerbinfront.tag = "binopening";

        isStateReset = true;
    }


    public void SetResetParameters()
    {

        isBackMeshCombined = false;
        isSideMeshCombined = false;
        isBottomMeshCombined = false;

        // Reset reward
        SetReward(0f);

        // Reset number of packed boxes
        boxes_packed = 0;

        // Reset vertex count
        VertexCount = 0;

        // Reset current bin volume
        current_bin_volume = total_bin_volume;

        // Reset current bin surface area
        current_bin_surface_area = total_bin_surface_area;

        // Reset origin counter to the number of origins 
        origin_counter = binSpawner.total_bin_num;

        // Destroy old boxes
        if (isAfterInitialization)
        {
            foreach (Box box in boxPool)
            {
                DestroyImmediate(box.gameobjectBox);
            } 
            // Reset box pool
            boxPool.Clear();

            // Reset meshes
            for (int i=0; i< binSpawner.m_BackMeshScripts.Count; i++) {
                binSpawner.m_BottomMeshScripts[i].MeshReset();
                binSpawner.m_SideMeshScripts[i].MeshReset();
                binSpawner.m_BackMeshScripts[i].MeshReset();
            }
            if (useDiscreteSolution)
            {
                // Reset vertices array
                Array.Clear(verticesArray, 0, verticesArray.Length);
                historicalVerticesLog.Clear();
            }
        }   
    
        // Reset states;
        StateReset();
        // Reset agent
        AgentReset();
    }


    /// <summary>
    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    /// </summary>
    void ConfigureAgent(int n) 
    {
        // DISCRETE
        if (n==0) 
        {
            if (isAfterInitialization==false)
            {
                SetModel(m_DiscreteBehaviorName, discreteBrain);
            }
            //Debug.Log($"BBN BRAIN BEHAVIOR NAME: {m_DiscreteBehaviorName}");
            useDiscreteSolution = true;
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 0.0f) == 0.0f)
            {
                boxSpawner.SetUpBoxes(box_type, seed);
                //Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 1.0f) == 1.0f)
            {
                boxSpawner.SetUpBoxes(box_type, seed+1);
                //Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 2.0f) == 2.0f)
            {
                boxSpawner.SetUpBoxes(box_type, seed+2);
                //Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
        }
    }


       public void ReverseSideNames(int id) 
    {
        var sidesList = boxPool[id].rb.gameObject.GetComponentsInChildren<Transform>();
        if (selectedRotation==new Vector3(90, 0, 0))
        {
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        else if (selectedRotation == new Vector3(0, 90, 0)) 
        {
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        else if (selectedRotation == new Vector3(0, 0, 90))
        {
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        else if (selectedRotation== new Vector3(0, 90, 90)) 
        {
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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
        else if (selectedRotation == new Vector3(90, 0, 90))
        {
            foreach (Transform child in sidesList) // only renames the side NAME to correspond with the rotation
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


    public void BoxReset(string cause)
    {
        if (cause == "failedPhysicsCheck") 
        {
            // Debug.Log($"SCS BOX {selectedBoxIdx} RESET LOOP, BOX POOL COUNT IS {boxPool.Count}");
            // detach box from agent
            targetBox.parent = null;
            // add back rigidbody and collider
            Rigidbody rb = boxPool[selectedBoxIdx].rb;
            BoxCollider bc = boxPool[selectedBoxIdx].rb.gameObject.AddComponent<BoxCollider>();
            // not be affected by forces or collisions, position and rotation will be controlled directly through script
            rb.isKinematic = true;
            // reset to starting position
            rb.transform.localScale = boxPool[selectedBoxIdx].startingSize;
            rb.transform.rotation = boxPool[selectedBoxIdx].startingRot;
            rb.transform.position = boxPool[selectedBoxIdx].startingPos;
            ReverseSideNames(selectedBoxIdx);
            // remove from organized list to be picked again
            maskedBoxIndices.Remove(selectedBoxIdx);
            // restore box information
            boxPool[selectedBoxIdx].boxSize = boxPool[selectedBoxIdx].startingSize;
            boxPool[selectedBoxIdx].boxRot = Quaternion.identity;
            boxPool[selectedBoxIdx].boxBinScale = Vector3.zero;
            boxPool[selectedBoxIdx].boxVertex = Vector3.zero;
            boxPool[selectedBoxIdx].isOrganized = false;
            // reset states
            StateReset();
            // REQUEST DECISION FOR THE NEXT ROUND OF PICKING
            GetComponent<Agent>().RequestDecision();
            Academy.Instance.EnvironmentStep();
        }
    }

}
