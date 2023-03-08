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
//using Blackbox = Boxes.Blackbox;
using Boxes;

public class PackerHand : Agent 
{
    public int curriculum_ConfigurationGlobal=2;  // Depending on this value, different curriculum will be picked
    private int curriculum_ConfigurationLocal=-1; // local reference of the above
    
    public int packSpeed = 20;
    public int seed = 123;
    public string box_file = "Boxes_30";
    
    public bool useCurriculum=true;
    public bool useAttention=true; // use attention by default (default = true)
    private bool useVerticesArray;
    public bool useDenseReward=true;
    public bool useSurfaceAreaReward_Discrete;
    public bool useSurfaceAreaReward_Continuous;
    public bool useDenseReward_Distance;
    public bool useDiscreteSolution=true;
    private bool isFirstLayerContinuous;
    public bool useContinuousSolution;
    private bool useOneHotEncoding;
    private bool useSparseReward;
    private bool usePenaltyReward;


    BufferSensorComponent m_BufferSensor;
    StatsRecorder m_statsRecorder; // adds stats to tensorboard

    public NNModel discreteBrain;   // Brain to use when all boxes are 1 by 1 by 1
    public NNModel continuousBrain;     // Brain to use when boxes are of similar sizes
    public NNModel mixBrain;     // Brain to use when boxes size vary
    private string m_DiscreteBehaviorName = "Discrete"; // 
    private string m_ContinuousBehaviorName = "Continuous";
    private string m_MixBehaviorName = "Mix";
    EnvironmentParameters m_ResetParams; // Environment parameters
    [HideInInspector] Rigidbody m_Agent; //cache agent rigidbody on initilization
    [HideInInspector] CombineMesh m_BackMeshScript;
    [HideInInspector] CombineMesh m_SideMeshScript;
    [HideInInspector] CombineMesh m_BottomMeshScript;

    [HideInInspector] public Vector3 initialAgentPosition;
    [HideInInspector] public Transform targetBox; // target box selected by agent
    [HideInInspector] public Transform targetBin; // phantom target bin object where the box will be placed

    public int selectedBoxIdx; // Box selected 
    public Vector3 selectedRotation; // selectedRotation selected
    public Vector3 selectedVertex; // Vertex selected
    public Vector3 [] verticesArray; // (2*num_boxes + 1) vertices, each vertex a Vextor3 vector: total size = (2*num_boxes + 1)*3
    [HideInInspector] public int selectedVertexIdx = -1; 
    [HideInInspector] public List<Box> boxPool; // space: num boxes
    [HideInInspector] private List<int> maskedVertexIndices;
    [HideInInspector] public List<int> maskedBoxIndices; // list of organzed box indices
    [HideInInspector] public List<Vector3> historicalVerticesLog;
    [HideInInspector] public int VertexCount = 0;
    [HideInInspector] public Vector3 boxWorldScale;
    [HideInInspector] public int maxBoxNum;
   // [HideInInspector] public List<Blackbox> blackboxPool  = new List<Blackbox>();

    //public Dictionary<Vector3, int > allVerticesDictionary = new Dictionary<Vector3, int>();
    // [HideInInspector] public List<Vector3> backMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    // [HideInInspector] public List<Vector3> sideMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    // [HideInInspector] public List<Vector3> bottomMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    [HideInInspector] public float total_x_distance; //total x distance between agent and target
    [HideInInspector] public float total_y_distance; //total y distance between agent and target
    [HideInInspector] public float total_z_distance; //total z distance between agent and target
    
    public BoxSpawner boxSpawner; // Box Spawner
    [HideInInspector] public SensorCollision sensorCollision;
    [HideInInspector] public SensorOuterCollision sensorOuterCollision;
    [HideInInspector] public SensorOverlapCollision sensorOverlapCollision;

    [HideInInspector] public bool isInitialization = true;
    [HideInInspector] public bool isEpisodeStart;
    [HideInInspector] public bool isAfterOriginVertexSelected;
    //[HideInInspector] public bool isBlackboxUpdated;
    // public bool isVertexSelected;
    [HideInInspector] public bool isBoxSelected;
    [HideInInspector] public bool isRotationSelected;
    [HideInInspector] public bool isPickedup;
    [HideInInspector] public bool isDroppedoff;
    [HideInInspector] public bool isStateReset;
    [HideInInspector] public bool isBottomMeshCombined;
    [HideInInspector] public bool isSideMeshCombined;
    [HideInInspector] public bool isBackMeshCombined;
    public GameObject binArea; // The bin container, which will be manually selected in the Inspector
    public GameObject binBottom;
    public GameObject binBack;
    public GameObject binSide;
    public GameObject outerbinfront;
    public Material clearPlastic;

    public GameObject Origin;

    private float total_bin_volume; // regular bin's volume
    private float total_bin_area; // regular bin's area
    private Bounds areaBounds; // regular bin's bounds
    private int boxes_packed = 0;
    private float current_bin_volume;
    private float percent_filled_bin_volume;
    private float box_volume;
    [HideInInspector] public float binscale_x;
    [HideInInspector] public float binscale_y;
    [HideInInspector] public float binscale_z;
    [HideInInspector] public Vector3 origin;




    public override void Initialize()
    {   

        // ****************** TEMPORARY settings (delete when merging with master branch)
        // Setup curriculum
        useCurriculum = false;
        curriculum_ConfigurationGlobal=2;
        // Setup reward scheme
        useSurfaceAreaReward_Continuous=true;
        useSurfaceAreaReward_Discrete=false;
        useDenseReward_Distance=false;
        useDenseReward=false;
        useSparseReward=false;
        usePenaltyReward=false;
        // For continuous implementation
        isFirstLayerContinuous=false;
        // WHen using attention, you may use one hot encoding
        useOneHotEncoding=false;
        // Setup Continuous solution
        useContinuousSolution=true;
        if (useContinuousSolution){useDiscreteSolution=false;}
        // Setup discrete solution
        if (useDiscreteSolution){useVerticesArray=true;}
        // ******************

        Academy.Instance.AutomaticSteppingEnabled = false;

        if (useCurriculum){
            curriculum_ConfigurationLocal = curriculum_ConfigurationGlobal; // local copy of curriculum configuration number, global will change to -1 but need original copy for state management
        }

        // initialize stats recorder to add stats to tensorboard
        m_statsRecorder = Academy.Instance.StatsRecorder;

        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        initialAgentPosition = m_Agent.position;
        //Debug.Log($"INITIAL AGENT POSITION {initialAgentPosition}");

        // Set environment parameters
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // Cache meshes' scripts
        m_BottomMeshScript = binBottom.GetComponent<CombineMesh>();
        m_SideMeshScript = binSide.GetComponent<CombineMesh>();
        m_BackMeshScript = binBack.GetComponent<CombineMesh>();
        m_BottomMeshScript.agent = this;
        m_SideMeshScript.agent = this;
        m_BackMeshScript.agent = this;

        // Update model references if we're overriding by adding a pre-trained brain
        var modelOverrider = GetComponent<ModelOverrider>();
        if (modelOverrider.HasOverrides)
        {
            discreteBrain = modelOverrider.GetModelForBehaviorName(m_DiscreteBehaviorName);
            m_DiscreteBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_DiscreteBehaviorName);

            continuousBrain = modelOverrider.GetModelForBehaviorName(m_ContinuousBehaviorName);
            m_ContinuousBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_ContinuousBehaviorName);

            mixBrain = modelOverrider.GetModelForBehaviorName(m_MixBehaviorName);
            m_MixBehaviorName = ModelOverrider.GetOverrideBehaviorName(m_MixBehaviorName);
        }

        // Make agent unaffected by collision
        CapsuleCollider m_c = GetComponent<CapsuleCollider>();
        m_c.isTrigger = true;
        
        // Get bounds of bin
        Renderer [] renderers = binArea.GetComponentsInChildren<Renderer>();
        areaBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; ++i)
        {
            areaBounds.Encapsulate(renderers[i].bounds);
        }
        //Debug.Log($"BIN BOUNDS: {areaBounds}");
        // Get total bin volume 
        total_bin_volume = areaBounds.extents.x*2 * areaBounds.extents.y*2 * areaBounds.extents.z*2;
        total_bin_area   = 4 * areaBounds.extents.x * areaBounds.extents.y + 4 * areaBounds.extents.x * areaBounds.extents.z + 4 * areaBounds.extents.y * areaBounds.extents.z;
        //Debug.Log($" TOTAL BIN VOLUME: {total_bin_volume}");

        // Get scale of bin
        binscale_x = areaBounds.extents.x*2;
        binscale_y = areaBounds.extents.y*2;
        binscale_z = areaBounds.extents.z*2;
        //origin = new Vector3(8.25f, 0.50f, 10.50f);
        origin = Origin.transform.position;
        //Debug.Log($"XYZ bin scale | x:{binscale_x} y:{binscale_y} z:{binscale_z}");

        // initialize local reference of box pool
        boxPool = boxSpawner.boxPool;

        maxBoxNum = boxSpawner.maxBoxQuantity;

        if (useAttention){
            m_BufferSensor = GetComponent<BufferSensorComponent>();
        }

        isEpisodeStart = true;

        Debug.Log("INITIALIZE ENDS");
    }


    public override void OnEpisodeBegin()
    {   
        Debug.Log("-----------------------NEW EPISODE STARTS------------------------------");
      
    }


    /// <summary>
    /// Agent adds environment observations 
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) 
    {
        Debug.Log("OBSERVATION");
        // Add updated bin volume
        sensor.AddObservation(current_bin_volume);
        sensor.AddObservation(total_bin_volume);
        sensor.AddObservation(total_bin_area);
        sensor.AddObservation(new Vector3(binscale_x,binscale_y,binscale_z));
        sensor.AddObservation(new Vector3(binscale_x,binscale_y,binscale_z));

        int j = 0;
        maskedBoxIndices = new List<int>();
        // Add all boxes sizes (selected boxes have sizes of 0s)
        foreach (Box box in boxPool) 
        {   
            Vector3 scaled_continuous_boxsize = new Vector3((box.boxSize.x/binscale_x), (box.boxSize.y/binscale_y), (box.boxSize.z/binscale_z));

            if (useAttention){
                if (useOneHotEncoding){
                    // Used for variable size observations
                    float[] listVarObservation = new float[maxBoxNum+8];
                    int boxNum = int.Parse(box.rb.name);
                    // The first boxPool.Count are one hot encoding of the box
                    listVarObservation[boxNum] = 1.0f;
                    // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                    listVarObservation[maxBoxNum]  = scaled_continuous_boxsize.x;
                    listVarObservation[maxBoxNum +1] = scaled_continuous_boxsize.y;
                    listVarObservation[maxBoxNum +2] = scaled_continuous_boxsize.z;
                    // Add updated [volume]/[w*h*l] added to state vector
                    listVarObservation[maxBoxNum +3] = (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z);
                    //Debug.Log($"XVD box:{box.rb.name}  |  vertex:{box.boxVertex}  |  x: {box.boxVertex.x * 23.5}  |  y: {box.boxVertex.y * 23.9}  |  z: {box.boxVertex.z * 59}");
                    //Debug.Log($"XVB box:{box.rb.name}  |  vertex:{box.boxVertex}  |  dx: {scaled_continuous_boxsize.x*23.5}  |  dy: {scaled_continuous_boxsize.y*23.9}  |  dz: {scaled_continuous_boxsize.z*59}");
                    //Debug.Log($"XVR box:{box.rb.name}  |  vertex:{box.boxVertex}  |  1: {box.boxRot[0]}  |  2: {box.boxRot[1]}  |  3: {box.boxRot[2]} | 4: {box.boxRot[3]}");
                    // Add updated box placement vertex
                    listVarObservation[maxBoxNum +4] = box.boxVertex.x;
                    listVarObservation[maxBoxNum +5] = box.boxVertex.y;
                    listVarObservation[maxBoxNum +6] = box.boxVertex.z;
                    // Add updated box rotation
                    // listVarObservation[boxPool.Count+7] = box.boxRot[0];
                    // listVarObservation[boxPool.Count+8] = box.boxRot[1];
                    // listVarObservation[boxPool.Count+9] = box.boxRot[2];
                    // listVarObservation[boxPool.Count+10] = box.boxRot[3];
                    // Add if box is placed already: 1 if placed already and 0 otherwise
                    listVarObservation[maxBoxNum +7] = box.isOrganized ? 1.0f : 0.0f;;
                    m_BufferSensor.AppendObservation(listVarObservation);
                }
                else{
                    // Used for variable size observations
                    float[] listVarObservation = new float[13];
                    // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                    listVarObservation[0]  = scaled_continuous_boxsize.x;
                    listVarObservation[1] = scaled_continuous_boxsize.y;
                    listVarObservation[2] = scaled_continuous_boxsize.z;
                    // Add updated [volume]/[w*h*l] added to state vector
                    listVarObservation[3] = (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z);
                    // Add updated box placement vertex
                    listVarObservation[4] = box.boxVertex.x;
                    listVarObservation[5] = box.boxVertex.y;
                    listVarObservation[6] = box.boxVertex.z;
                    // Add updated box rotation
                    listVarObservation[7] = box.boxRot[0];
                    listVarObservation[8] = box.boxRot[1];
                    listVarObservation[9] = box.boxRot[2];
                    listVarObservation[10] = box.boxRot[3];
                    // Add if box is placed already: 1 if placed already and 0 otherwise
                    listVarObservation[12] = box.isOrganized ? 1.0f : 0.0f;;
                    m_BufferSensor.AppendObservation(listVarObservation);
                }
            }
            else{
                // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                sensor.AddObservation(scaled_continuous_boxsize);
                // Add updated [volume]/[w*h*l] added to state vector
                sensor.AddObservation( (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z) );
                sensor.AddObservation (box.boxVertex);
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
        if (useOneHotEncoding){
            for (int m=boxPool.Count(); m< maxBoxNum; m++)
            {
                // Debug.Log($"MASK ZERO PADDING {m}");
                maskedBoxIndices.Add(m);
            }
        }

        // Add array of vertices (selected vertices are 0s)
        if (useVerticesArray)
        {
            maskedVertexIndices = new List<int>();
            
            int i = 0;
            foreach (Vector3 vertex in verticesArray) 
            {   
                //Debug.Log($"XYX scaled_continuous_vertex: {scaled_continuous_vertex}");
                if (useVerticesArray)
                {
                    Vector3 scaled_continuous_vertex = new Vector3(((vertex.x - origin.x)/binscale_x), ((vertex.y - origin.y)/binscale_y), ((vertex.z - origin.z)/binscale_z));
                    sensor.AddObservation(scaled_continuous_vertex); //add vertices to sensor observations
                }
                // verticesArray is still getting fed vertex: (0, 0, 0) which is scaled_continuous_vertex: (-0.35, -0.02, -0.18)
                if (vertex == Vector3.zero)
                {
                    //Debug.Log($"MASK VERTEX LOOP INDEX:{i}");
                    maskedVertexIndices.Add(i);
                }
                i++;
            }
        }
        // If everything works for the Continuous approach then remove this line
        // else
        // {
        //     maskedVertexIndices.Add(0);
        // }

        // // array of blackboxes 
        // foreach (Blackbox blackbox in blackboxPool)
        // {
        //     // float[][] blackbox_observation = new float[][]{};
        //     // blackbox_observation = new float[][] {
        //     //     new float[] {blackbox.size.x, blackbox.size.y, blackbox.size.z},
        //     //     new float[] {blackbox.vertex.x, blackbox.vertex.y, blackbox.vertex.z},
        //     // };
        //     sensor.AddObservation(blackbox.size);
        //     sensor.AddObservation(blackbox.vertex);
        // }
        // sensor.AddObservation(blackboxesArray); //add vertices to sensor observations
    }


    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // vertices action mask
        if (useDiscreteSolution)
        {
            if (isAfterOriginVertexSelected) {
                if (useVerticesArray){
                    foreach (int vertexIdx in maskedVertexIndices) 
                    {
                        //Debug.Log($"MASK VERTEX {vertexIdx}");
                        actionMask.SetActionEnabled(1, vertexIdx, false);
                    }
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
        Debug.Log("ACTION");
        var j = -1;
        var i = -1;

        var discreteActions = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;

        SelectBox(discreteActions[++j]); 
        if (useDiscreteSolution){
            // activate for discrete solution + adjust in Inspector by adding 1 more discrete branch
            SelectVertexDiscrete(discreteActions[++j],continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        }
        else if (useContinuousSolution){
            // activate for continuous solution
            SelectVertexContinuous(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        }
        SelectRotation(discreteActions[++j]);
    }


    /// <summary>
    /// This function is called at every time step
    ///</summary>
    void FixedUpdate() 
    {
        // if all boxes packed, reset episode
        if (boxPool.Count!=0 && ((useOneHotEncoding && maskedBoxIndices.Count == maxBoxNum) || (maskedBoxIndices.Count == boxPool.Count)))
        {
            if (useSparseReward)
            {
                AddReward(percent_filled_bin_volume*10);
                Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
            }
            EndEpisode();
            if (useCurriculum){curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;}
            isEpisodeStart = true;
            Debug.Log($"EPISODE {CompletedEpisodes} START TRUE AFTER ALL BOXES PACKED");
        }
        // if reaches max step, reset episode 
        if (StepCount >= MaxStep) 
        {
            if (useSparseReward)
            {
                AddReward(percent_filled_bin_volume*10);
                Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
            }
            EndEpisode();
            if (useCurriculum){curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;}
            isEpisodeStart = true;
            Debug.Log($"EPISODE {CompletedEpisodes}  START TRUE AFTER MAXIMUM STEP");
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
                    isInitialization = false;
                }
            }
            else
            {
                // boxSpawner.SetUpBoxes(box_file, false, 0, 0, 0, seed);
                boxSpawner.SetUpBoxes("uniform_random", false, 2, 2, 2, seed);
                Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }

            if (useDiscreteSolution)
            { 
                selectedVertex = origin;
                isAfterOriginVertexSelected = false;
            }


            //Debug.Log("REQUEST DECISION AT START OF EPISODE"); 
            GetComponent<Agent>().RequestDecision(); 
            Academy.Instance.EnvironmentStep();

        }
        // if meshes are combined, reset states and go for next round of box selection 
        if ((isBackMeshCombined | isBottomMeshCombined | isSideMeshCombined) && isStateReset==false) 
        {
            // If a mesh didn't combine, force combine
            if (isBackMeshCombined==false)
            {
                m_BackMeshScript.ForceMeshCombine();
            }
            if (isSideMeshCombined == false)
            {     
                m_SideMeshScript.ForceMeshCombine();
            }
            if (isBottomMeshCombined == false)
            {     
                m_SideMeshScript.ForceMeshCombine();
            }

            StateReset();

            if (useDiscreteSolution)
            {
                isAfterOriginVertexSelected = true;
                // Vertices array of tripoints doesn't depend on the trimesh; Only update vertices list and vertices array when box is placed
                UpdateVerticesArray();
            }

            // Side, back, and bottom vertices lists depends on the trimesh; Should be commented out if not using blackbox for better performance
            //UpdateVerticesList();
            // Both vertices array and vertices list are used to find black boxes
            //UpdateBlackBox();

            box_volume = boxWorldScale.x * boxWorldScale.y * boxWorldScale.z;
            float box_surface_area = 2*boxWorldScale.x*boxWorldScale.y + 2*boxWorldScale.y * boxWorldScale.z + 2*boxWorldScale.x *  boxWorldScale.z;

            if (useSurfaceAreaReward_Discrete){
                // Add surface area reward
                float percent_contact_surface_area = sensorCollision.totalContactSA/box_surface_area;
                AddReward(percent_contact_surface_area * 50f);
                Debug.Log($"RWDsa {GetCumulativeReward()} total reward | {percent_contact_surface_area * 50f} reward from surface area");
            }
            if (useSurfaceAreaReward_Continuous)
            {
                // Add surface area reward
                float percent_contact_surface_area_scaled = sensorCollision.totalContactSA * box_volume / ( total_bin_area * total_bin_volume );
                AddReward(percent_contact_surface_area_scaled * 1000f);
                Debug.Log($"RWDsa {GetCumulativeReward()} total reward | {percent_contact_surface_area_scaled * 1000f} reward from surface area");
            }
            
            current_bin_volume = current_bin_volume - box_volume;
            percent_filled_bin_volume = (1 - (current_bin_volume/total_bin_volume)) * 100;
                // Add volume reward
            if (useDenseReward)
            {
                AddReward((box_volume/total_bin_volume) * 1000f);
                Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{(box_volume/total_bin_volume) * 1000f} reward | current_bin_volume: {current_bin_volume} | percent bin filled: {percent_filled_bin_volume}%");
            }

            // Increment stats recorder to match reward
            m_statsRecorder.Add("% Bin Volume Filled", percent_filled_bin_volume, StatAggregationMethod.Average);

            //Debug.Log("REQUEST DECISION AT NEXT ROUND OF OF PICKING");
            GetComponent<Agent>().RequestDecision();
            Academy.Instance.EnvironmentStep();
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
                if (sensorCollision.passedGravityCheck && sensorOuterCollision.passedBoundCheck && sensorOverlapCollision.passedOverlapCheck)
                {
                    DropoffBox();
                    boxes_packed++;
                }
                else
                {
                    //BoxReset("failedPhysicsCheck");
                    if (usePenaltyReward)
                    {
                        AddReward(-100f);
                    }
                    if (useSparseReward)
                    {
                        AddReward(percent_filled_bin_volume*10);   
                        Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{percent_filled_bin_volume * 10f} reward | percent bin filled: {percent_filled_bin_volume}%");
                    }
                    EndEpisode();
                    if (useCurriculum){curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;}
                    isEpisodeStart = true;
                    Debug.Log($"EPISODE {CompletedEpisodes} START TRUE AFTER FAILING PHYSICS TEST");
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


    /// <summary>
    /// Updates the vertices every time a new mesh is created
    ///</summary>
    // void UpdateVerticesList() 
    // {
    //     MeshFilter mf_back = binBack.GetComponent<MeshFilter>();
    //     AddVertices(mf_back.mesh.vertices, backMeshVertices);
    //     //Debug.Log($"OOO BACK MESH VERTICES COUNT IS {backMeshVertices.Count()}");
    //     MeshFilter mf_bottom = binBottom.GetComponent<MeshFilter>();
    //     AddVertices(mf_bottom.mesh.vertices, bottomMeshVertices);
    //     //Debug.Log($"OOO BOTTOM MESH VERTICES COUNT IS {bottomMeshVertices.Count()}");
    //     MeshFilter mf_side = binSide.GetComponent<MeshFilter>();
    //     AddVertices(mf_side.mesh.vertices, sideMeshVertices);  
    //     //Debug.Log($"OOO SIDE MESH VERTICES COUNT IS {sideMeshVertices.Count()}");
    // }

    /// <summary>
    /// For every mesh, add each unique vertex to a mesh list and a counter dictionary
    ///</summary>
    // Vertices used for constructing blackbox
    // AddVertices( input: ALL_LOCAL_VerticesFromMesh, output: UNIQUE_GLOBAL_VerticesFromMesh )
    // void AddVertices(Vector3 [] vertices, List<Vector3> verticesList) 
    // {
    //     Matrix4x4 localToWorld = binArea.transform.localToWorldMatrix;
    //     var tempHashSet = new HashSet<Vector3>();
    //     // rounding part
    //     foreach (Vector3 vertex in vertices) 
    //     {
    //         // first address vertices that are meant to be the same by rounding
    //         var roundedVertex = new Vector3((float)(Math.Round(vertex.x, 2)), (float)(Math.Round(vertex.y, 2)), (float)(Math.Round(vertex.z, 2)));
    //         // remove duplicates by using a hash set
    //         tempHashSet.Add(roundedVertex);
    //     }
    //     // localtoworld part
    //     foreach (Vector3 vertex in tempHashSet) 
    //     {
    //         // convert local scale to world position
    //         Vector3 worldVertex = localToWorld.MultiplyPoint3x4(vertex);
    //         verticesList.Add(worldVertex);
    //     }
    // }


    void UpdateVerticesArray() 
    {
        List<Vector3> tripoints_list = new List<Vector3>();
        var tripoint_redx = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y, selectedVertex.z); // x red side tripoint
        var tripoint_greeny = new Vector3(selectedVertex.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z); // y green bottom tripoint 
        var tripoint_bluez = new Vector3(selectedVertex.x, selectedVertex.y, selectedVertex.z+boxWorldScale.z); // z blue back tripoint 

        tripoints_list.Add(tripoint_redx);   
        tripoints_list.Add(tripoint_greeny);
        tripoints_list.Add(tripoint_bluez);

        // comment out the 4 lines below if want only 3 vertices
        // var tripoint_xy = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z);
        // var tripoint_xyz = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z+boxWorldScale.z);
        // var tripoint_xz = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y, selectedVertex.z+boxWorldScale.z);
        // var tripoint_yz = new Vector3(selectedVertex.x, selectedVertex.y+boxWorldScale.y, selectedVertex.z+boxWorldScale.z);

        // comment out the 4 lines below if want only 3 vertices
        // tripoints_list.Add(tripoint_xy);
        // tripoints_list.Add(tripoint_xyz);
        // tripoints_list.Add(tripoint_xz);
        // tripoints_list.Add(tripoint_yz);
    

        for (int idx = 0; idx<tripoints_list.Count(); idx++) 
        {
            //Debug.Log($"TPB tripoints_list[idx]: {tripoints_list[idx]} | areaBounds.min: {areaBounds.min} | areaBounds.max: {areaBounds.max} ");
            if (tripoints_list[idx].x >= areaBounds.min.x && tripoints_list[idx].x < areaBounds.max.x) {
            if (tripoints_list[idx].y >= areaBounds.min.y && tripoints_list[idx].y < areaBounds.max.y) {
            if (tripoints_list[idx].z >= areaBounds.min.z && tripoints_list[idx].z < areaBounds.max.z) {
                // only if historicVerticesArray doesnt already contain the tripoint, add it to the verticesArray
                Vector3 scaled_continuous_vertex = new Vector3((tripoints_list[idx].x - origin.x)/binscale_x,  (tripoints_list[idx].y - origin.y)/binscale_y,  (tripoints_list[idx].z - origin.z)/binscale_z);
                //Vector3 rounded_scaled_vertex = new Vector3((float)Math.Round(scaled_continuous_vertex.x, 2), (float)Math.Round(scaled_continuous_vertex.y, 2), (float)Math.Round(scaled_continuous_vertex.y, 2));
                //Debug.Log($"VACx historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false: {historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false} | scaled_continuous_vertex: {scaled_continuous_vertex} ");
                if ( historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false )
                {
                    // Debug.Log($"TPX idx:{idx} | tripoint add to tripoints_list[idx]: {tripoints_list[idx]} | selectedVertex: {selectedVertex}") ;
                    // Add scaled tripoint_vertex to verticesArray
                    verticesArray[VertexCount] = scaled_continuous_vertex;
                    historicalVerticesLog.Add(scaled_continuous_vertex);
                    VertexCount ++;
                    //Debug.Log($"VERTEX COUNT IS {VertexCount}");

                }
            }
            }
            }
        }
    }


    // public void UpdateBlackBox() 
    // {
    //     Debug.Log($"UBX Update BlackboX running");

        // foreach (Vector3 vertex in verticesArray) 
        // {
        //     //bottomVertices.Find(v=>  v[1]==vertex[1] && v[2]==vertex[2]).MinBy(v => Math.Abs(v[0]-vertex[0]));
        //     Vector3 closest_x_vertex = backMeshVertices.Aggregate(new Vector3(float.MaxValue, 0, 0), (min, next) => 
        //     vertex[0]<next[0] && Math.Abs(next[0]-vertex[0]) < Math.Abs(min[0] - vertex[0]) && next[1]==vertex[1] && next[2] == vertex[2] ? next : min);
        //     //Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES X VERTEX IS {closest_x_vertex}");

        //     Vector3 closest_y_vertex = sideMeshVertices.Aggregate(new Vector3(0, float.MaxValue, 0), (min, next) => 
        //     vertex[1]<next[1] && Math.Abs(next[1]-vertex[1]) < Math.Abs(min[1] - vertex[1]) && next[0]==vertex[0] && next[2] == vertex[2] ? next : min);
        //     //Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Y VERTEX IS {closest_y_vertex}");

        //     Vector3 closest_z_vertex = sideMeshVertices.Aggregate(new Vector3(0, 0, float.MaxValue), (min, next) => 
        //     vertex[2]<next[2] && Math.Abs(next[2]-vertex[2]) < Math.Abs(min[2] - vertex[2]) && next[1]==vertex[1] && next[0] == vertex[0] ? next : min);
        //     //Debug.Log($"BCX BLACK BOX VERTEX IS {vertex} AND CLOSES Z VERTEX IS {closest_z_vertex}");

        //     float blackbox_x_size = Math.Abs(closest_x_vertex[0] - vertex[0]);
        //     float blackbox_y_size = Math.Abs(closest_y_vertex[1] - vertex[1]);
        //     float blackbox_z_size = Math.Abs(closest_z_vertex[2] - vertex[2]);
        //     Vector3 blackbox_position = new Vector3(blackbox_x_size*0.5f+vertex[0], blackbox_y_size*0.5f+vertex[1], blackbox_z_size*0.5f+vertex[2]);

        //     if (blackbox_x_size<100f && blackbox_x_size>2f && blackbox_y_size<100f && blackbox_y_size > 2f && blackbox_z_size<100f && blackbox_z_size>2f) 
        //     {
        //         Debug.Log($"BPS BLACK BOX POSITION {blackbox_position} SIZES {blackbox_x_size}, {blackbox_y_size}, {blackbox_z_size}");
        //         GameObject blackbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //         blackbox.name = "blackbox";
        //         blackbox.transform.position = blackbox_position;
        //         blackbox.transform.localScale = new Vector3(blackbox_x_size, blackbox_y_size, blackbox_z_size);
        //         Renderer cubeRenderer = blackbox.GetComponent<Renderer>();
        //         cubeRenderer.material = clearPlastic;

        //         Blackbox newBlackbox = new Blackbox
        //         {
        //             position = blackbox_position,
        //             size = new Vector3(blackbox_x_size, blackbox_y_size, blackbox_z_size),
        //             vertex = vertex,
        //             gameobjectBlackbox = blackbox,
        //             volume = blackbox_x_size * blackbox_y_size * blackbox_z_size,
        //         };

        //         blackboxPool.Add(newBlackbox);
        //     }
        // }
        // isBlackboxUpdated = true;
    // }

    // public void SelectBlackboxVertex() 
    // {
        
    //     Blackbox smallest_blackbox = null;
    //     float minVolume = float.MaxValue;
    //     foreach (Blackbox blackbox in blackboxPool)
    //     {
    //         if (blackbox.volume < minVolume)
    //         {
    //             minVolume = blackbox.volume;
    //             smallest_blackbox = blackbox;
    //         }
    //     }
    //     Debug.Log($"SBV SMALLEST BLACKBOX IS: {smallest_blackbox.gameobjectBlackbox} with volume {smallest_blackbox.volume} and vertex {smallest_blackbox.vertex}");
    //     smallest_blackbox.gameobjectBlackbox.GetComponent<Renderer>().material.color = Color.black;
    //     selectedVertex = smallest_blackbox.vertex;
    //     isVertexSelected = true;

    // }

    public void SelectVertexDiscrete(int action_SelectedVertexIdx, float action_SelectedVertex_x, float action_SelectedVertex_y, float action_SelectedVertex_z) 
    {
        action_SelectedVertex_x = (action_SelectedVertex_x + 1f) * 0.5f;
        action_SelectedVertex_y = (action_SelectedVertex_y + 1f) * 0.5f;
        action_SelectedVertex_z = (action_SelectedVertex_z + 1f) * 0.5f;
        Debug.Log($"SVB brain selected vertex #: {action_SelectedVertexIdx} ");

        // assign selected vertex where next box will be placed, selected from brain's actionbuffer (inputted as action_SelectedVertex)
        selectedVertexIdx = action_SelectedVertexIdx;
        var scaled_selectedVertex = verticesArray[action_SelectedVertexIdx];
        boxPool[selectedBoxIdx].boxVertex = scaled_selectedVertex;
        if (curriculum_ConfigurationLocal == 1)
        {
            if (useDenseReward_Distance){
                // reward_dense_distance = inverse of exponential distance between discreteVertex and continuousVertex 
                float reward_dense_distance = (float) 
                (1/(Math.Pow(action_SelectedVertex_x - scaled_selectedVertex.x, 2) + Math.Pow(action_SelectedVertex_y - scaled_selectedVertex.y, 2) + Math.Pow(action_SelectedVertex_z - scaled_selectedVertex.z, 2)));
                AddReward(reward_dense_distance);
                Debug.Log($"RWDvtx {GetCumulativeReward()} total reward | {reward_dense_distance} reward from vertex distance");
            }
        }
        selectedVertex =  new Vector3(((scaled_selectedVertex.x* binscale_x) + origin.x), ((scaled_selectedVertex.y* binscale_y) + origin.y), ((scaled_selectedVertex.z* binscale_z) + origin.z));
        Debug.Log($"SVX Discrete Selected VerteX: {selectedVertex}");
        // isVertexSelected = true;

    }

    public void SelectVertexContinuous(float action_SelectedVertex_x, float action_SelectedVertex_y, float action_SelectedVertex_z) 
    {
        action_SelectedVertex_x = (action_SelectedVertex_x + 1f) * 0.5f;
        action_SelectedVertex_y = (action_SelectedVertex_y + 1f) * 0.5f;
        action_SelectedVertex_z = (action_SelectedVertex_z + 1f) * 0.5f;
        Debug.Log($"action_SelectedVertex_x ================== {action_SelectedVertex_x}");


        if (isFirstLayerContinuous)
        {
            selectedVertex = new Vector3(((action_SelectedVertex_x* binscale_x) + origin.x), 0.5f, ((action_SelectedVertex_z* binscale_z) + origin.z));
            boxPool[selectedBoxIdx].boxVertex = new Vector3(action_SelectedVertex_x, action_SelectedVertex_y, action_SelectedVertex_z);
            Debug.Log($"SVX Continuous Selected VerteX: {selectedVertex}");
        }
        else{
            selectedVertex = new Vector3(((action_SelectedVertex_x* binscale_x) + origin.x), ((action_SelectedVertex_y* binscale_y) + origin.y), ((action_SelectedVertex_z* binscale_z) + origin.z));
            boxPool[selectedBoxIdx].boxVertex = new Vector3(action_SelectedVertex_x, action_SelectedVertex_y, action_SelectedVertex_z);
            Debug.Log($"SVX Continuous Selected VerteX: {selectedVertex}");
        }
    }


    public void UpdateBoxPosition() 
    {
        // Packerhand.cs  : deals parent box : position (math)
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)

        if (targetBin==null) 
        {
            // this targetBin will need be destroyed too 
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
        // leave this in case we want to add more physics to the boxes
        // BoxCollider bc = testBox.GetComponent<BoxCollider>();
        // rb.mass = 300f;
        // rb.velocity = Vector3.zero;
        // rb.angularVelocity = Vector3.zero;
        // rb.drag = 1f;
        // rb.angularDrag = 2f;
        // bc.material.bounciness = 0f;
        // bc.material.dynamicFriction = 1f;
        // bc.material.staticFriction = 1f;
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
        testBoxChild.transform.localScale = new Vector3((boxWorldScale.x - 0.5f), (boxWorldScale.y - 0.5f), (boxWorldScale.z - 0.5f));
        testBoxChild.transform.position = testPosition;
        rbChild.constraints = RigidbodyConstraints.FreezeAll;
        rbChild.interpolation = RigidbodyInterpolation.Interpolate;
        sensorOverlapCollision = testBoxChild.AddComponent<SensorOverlapCollision>();
        sensorOuterCollision = testBoxChild.AddComponent<SensorOuterCollision>();
        sensorOverlapCollision.agent = this; // agent reference used by component to set rewards on collision
        sensorOuterCollision.agent = this; // agent reference used by component to set rewards on collision
        testBoxChild.name = $"testboxChild{targetBox.name}";
        testBoxChild.tag = "testboxChild";
    }


    /// <summary>
    /// Agent selects a target box
    ///</summary>
    public void SelectBox(int action_SelectedBox) 
    {
        selectedBoxIdx = action_SelectedBox;
        Debug.Log($"SBB Selected Box selectedBoxIdx: {selectedBoxIdx}");
        targetBox = boxPool[selectedBoxIdx].rb.transform;
        isBoxSelected = true;
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
            selectedRotation = new Vector3(0, 0, 0);
            foreach (Transform child in childrenList)
            {
                child.tag = "pickupbox";
            }      
        }  
        else if (action==1) 
        {
            Debug.Log($"SelectRotation() called with rotation (90, 0, 0)");
            selectedRotation = new Vector3(90, 0, 0);
            boxWorldScale = new Vector3(boxWorldScale[0], boxWorldScale[2], boxWorldScale[1]); // actual rotation of object transform
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxSize = boxWorldScale;
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
            selectedRotation = new Vector3(0, 90, 0);
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[1], boxWorldScale[0]); // actual rotation of object transform
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxSize = boxWorldScale;
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
            selectedRotation = new Vector3(0, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[0], boxWorldScale[2]); // actual rotation of object transform
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxSize = boxWorldScale;
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
            selectedRotation = new Vector3(0, 90, 90 ); 
            boxWorldScale = new Vector3(boxWorldScale[2], boxWorldScale[0], boxWorldScale[1]); // actual rotation of object transform
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxSize = boxWorldScale;
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
            selectedRotation = new Vector3(90, 0, 90);
            boxWorldScale = new Vector3(boxWorldScale[1], boxWorldScale[2], boxWorldScale[0]); // actual rotation of object transform
            boxPool[selectedBoxIdx].boxRot = Quaternion.Euler(selectedRotation);
            boxPool[selectedBoxIdx].boxSize = boxWorldScale;
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
        isRotationSelected = true;
    }


    /// <summmary>
    /// Agent picks up the box
    /// </summary>
    public void PickupBox() 
    {
        // Attach the box as a child of the agent parent, effectively attaching the box's movement to the agent's movement  
        //targetBox.parent = this.transform;
        targetBox.parent = m_Agent.transform;

        isPickedup = true;

        Destroy(targetBox.GetComponent<BoxCollider>());  

        // Would be best if moved isCollidedColor=false state reset to StateReset(), but current issue
        m_BackMeshScript.isCollidedBlue = false;
        m_BottomMeshScript.isCollidedGreen = false;
        m_SideMeshScript.isCollidedRed = false;
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
            // m_c.gameObject.tag = "droppedoff";
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


    public void ReverseSideNames(int id) 
    {
        var childrenList = boxPool[id].rb.gameObject.GetComponentsInChildren<Transform>();
        if (selectedRotation==new Vector3(90, 0, 0))
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
        else if (selectedRotation == new Vector3(0, 90, 0)) 
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
        else if (selectedRotation == new Vector3(0, 0, 90))
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
        else if (selectedRotation== new Vector3(0, 90, 90)) 
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
        else if (selectedRotation == new Vector3(90, 0, 90))
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


    public void BoxReset(string cause)
    {
        if (cause == "failedPhysicsCheck") 
        {
            Debug.Log($"SCS BOX {selectedBoxIdx} RESET LOOP, BOX POOL COUNT IS {boxPool.Count}");
            // Detach box from agent
            targetBox.parent = null;
            // Add back rigidbody and collider
            Rigidbody rb = boxPool[selectedBoxIdx].rb;
            BoxCollider bc = boxPool[selectedBoxIdx].rb.gameObject.AddComponent<BoxCollider>();
            // Not be affected by forces or collisions, position and rotation will be controlled directly through script
            rb.isKinematic = true;
            // Reset to starting position
            rb.transform.localScale = boxPool[selectedBoxIdx].startingSize;
            rb.transform.rotation = boxPool[selectedBoxIdx].startingRot;
            rb.transform.position = boxPool[selectedBoxIdx].startingPos;
            ReverseSideNames(selectedBoxIdx);
            // Remove from organized list to be picked again
            maskedBoxIndices.Remove(selectedBoxIdx);
            // Reset states
            StateReset();
            // REQUEST DECISION FOR THE NEXT ROUND OF PICKING
            GetComponent<Agent>().RequestDecision();
            Academy.Instance.EnvironmentStep();
            // Setting to true allows another vertex to be selected
            // isBlackboxUpdated = true;
            // Setting to true keeps the current vertex and allows another box to be selected
            // isVertexSelected = true;
        }
    }


    public void AgentReset() 
    {
        m_Agent.position = initialAgentPosition;
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }


    public void StateReset() 
    {
        if (isBackMeshCombined && isSideMeshCombined && isBottomMeshCombined) 
        {
            if (useDiscreteSolution)
            {
                // Remove consumed selectedVertex from verticesArray (since another box cannot be placed there)
                // only removed when a box is successfully placed, if box fails physics test, selected vertex will not be removed
                // conditional check can be removed if failing physics test = end of episode
                if (isAfterOriginVertexSelected)
                {
                    //Debug.Log($"SRS SELECTED VERTEX IDX {selectedVertexIdx} RESET");
                    verticesArray[selectedVertexIdx] = Vector3.zero;               
                }
            }
            boxPool[selectedBoxIdx].isOrganized = true;
        }
        //isBlackboxUpdated = false;
        // isVertexSelected = false;
        isBoxSelected = false;
        isRotationSelected = false;
        isPickedup = false;
        isDroppedoff = false;
        if (targetBin!=null)
        {
            DestroyImmediate(targetBin.gameObject);
        }
        targetBox = null;
        outerbinfront.tag = "binopening";
        isStateReset = true;
    }


    public void SetResetParameters()
    {
        // Reset meshes
        m_BottomMeshScript.MeshReset();
        m_SideMeshScript.MeshReset();
        m_BackMeshScript.MeshReset();

        isBackMeshCombined = false;
        isSideMeshCombined = false;
        isBottomMeshCombined = false;

        // Reset reward
        SetReward(0f);

        // Reset number of packed boxes
        boxes_packed = 0;

        // Reset current bin volume
        current_bin_volume = total_bin_volume;

        // Destroy old boxes
        foreach (Box box in boxPool)
        {
            DestroyImmediate(box.gameobjectBox);
        }        
        // Reset box pool
        boxPool.Clear();
        if (useDiscreteSolution)
        {
            // Reset vertices array
            Array.Clear(verticesArray, 0, verticesArray.Length);
            // Reset vertices list
            // backMeshVertices.Clear();
            // sideMeshVertices.Clear();
            // bottomMeshVertices.Clear();
            historicalVerticesLog.Clear();
        }
        
        // Reset vertex count
        VertexCount = 0;

        // Reset states;
        StateReset();

        // Reset agent
        AgentReset();
    }


    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    void ConfigureAgent(int n) 
    {
        if (n==0) 
        // DISCRETE
        {
            useDiscreteSolution     = true;
            isFirstLayerContinuous = false;
            useContinuousSolution        = false;
            useVerticesArray       = true;
            Debug.Log($"BBN BRAIN BEHAVIOR NAME: {m_DiscreteBehaviorName}");            
            if (isInitialization)
            {
                SetModel(m_DiscreteBehaviorName, discreteBrain);
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 0.0f) == 0.0f)
            {
                boxSpawner.SetUpBoxes("mix_random", true, 4, 4, 6, seed);
                Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 1.0f) == 1.0f)
            {
                boxSpawner.SetUpBoxes("mix_random", true, 4, 4, 6, seed+1);
                Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("discrete", 2.0f) == 2.0f)
            {
                boxSpawner.SetUpBoxes("mix_random", true, 4, 4, 6, seed+2);
                Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
        }
        else if (n==1) 
        // Mixed DISCRETE-CONTINUOUS (not recommended to switch from discrete to continuous brain)
        {
            Debug.Log($"BBN BRAIN BEHAVIOR NAME: {m_MixBehaviorName}");
            if (isInitialization)
            {
                SetModel(m_MixBehaviorName, mixBrain);
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("mix", 0.0f) == 0.0f)
            {
                useDiscreteSolution     = true;
                isFirstLayerContinuous = false;
                useContinuousSolution        = false;
                useVerticesArray       = true;
            }
            else if (Academy.Instance.EnvironmentParameters.GetWithDefault("mix", 1.0f) == 1.0f)
            {
                useDiscreteSolution     = false;
                isFirstLayerContinuous = true;
                useContinuousSolution        = false;
                useVerticesArray       = false;
            }
            else if (Academy.Instance.EnvironmentParameters.GetWithDefault("mix", 2.0f) == 2.0f)
            {
                useDiscreteSolution     = false;
                isFirstLayerContinuous = false;
                useContinuousSolution        = true;
                useVerticesArray       = false;
            }
        }
        else if (n==2)
        // CONTINUOUS
        {
            useVerticesArray       = false;
            useDiscreteSolution     = false;
            Debug.Log($"BBN BRAIN BEHAVIOR NAME: {m_ContinuousBehaviorName}");
            if (isInitialization)
            {
                SetModel(m_ContinuousBehaviorName, continuousBrain);  
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault("continuous", 1.0f) == 1.0f)
            {
                isFirstLayerContinuous = false;
                useContinuousSolution        = false;

            }
            else if (Academy.Instance.EnvironmentParameters.GetWithDefault("continuous", 2.0f) == 2.0f)
            {
                isFirstLayerContinuous = false;
                useContinuousSolution        = true;
            }  
        }
    }

    /// <summary>
    /// Agent moves according to selected action.
    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     var discreteActionsOut = actionsOut.DiscreteActions;
    //     //forward
    //     if (Input.GetKey(KeyCode.W))
    //     {
    //         discreteActionsOut[1] = 1;
    //     }
    //     if (Input.GetKey(KeyCode.S))
    //     {
    //         discreteActionsOut[1] = 2;
    //     }
    //     //rotate
    //     if (Input.GetKey(KeyCode.D))
    //     {
    //         discreteActionsOut[2] = 1;
    //     }
    //     if (Input.GetKey(KeyCode.A))
    //     {
    //         discreteActionsOut[2] = 2;
    //     }
    //     //right
    //     if (Input.GetKey(KeyCode.E))
    //     {
    //         discreteActionsOut[3] = 1;
    //     }
    //     if (Input.GetKey(KeyCode.Q))
    //     {
    //         discreteActionsOut[3] = 2;
    //     }
    // }
}
