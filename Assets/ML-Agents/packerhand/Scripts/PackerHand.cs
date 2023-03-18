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
    // Miscellaneous environment parameters   
    public int packSpeed=20;
    public int seed=123;

    // Method switchers
    public bool useCurriculum;
    public bool useAttention; // use attention by default (default = true)
    public bool useDiscreteSolution;
    public bool _useOneHotEncodingForAttention; 
                            // _useOneHotEncodingForAttention means that we have a number of standard boxes (eg 50), each with unique attributes. For each training,
                            // we would use only a subset of these boxes (eg 10). The rest of the boxes will be created but their sizes will be set to 
                            // zero. This is done to ensure that the agent learns on boxes only in the training.
                            // In other words, this implementation aims to create a "BOX VOCABULARY". This method is NOT COMPLETE yet as we have not yet
                            // generated a standard box vocabulary.
                            // Three points to caounter the use of this method:
                            // 1. We don;t have a box vocabulary yet
                            // 2. Even if we did, it is not really needed. The unique identifier of each box is its size which is already in the 
                            //    observation stack. 
                            // 3. OneHotEncoding drastically increases the size of the observation stack, making training far more difficult.
    [SerializeField] private bool _isFirstLayerContinuous; // Used to pack the first layer of boxes onto the bottom of the bin

    //  Reward switchers
    public bool useSparseReward;        // Reward at the end of the episode: total volume ratio (total volume of the boxes / volume of the bin)
    public bool usePenaltyReward;       // Penalty for every box misplaced and/or violating environemnt constraints
    public bool useDenseReward;         // Reward for every box placed: volume ratio (volume of the box / volume of the bin)
    public bool useSurfaceAreaReward;   // Reward for every box placed: surface area ratio (area of the box / area of the bin)
    public bool useCombinedReward;    // Reward for every box placed: combines the area & volume rewards (surface area ratio & volume ratio)
    public bool useDistanceReward;    // Reward for every box placed: distance ratio (distance between the box and the bin / distance between the agent and the bin)

    // Filters for lLocal features
    private bool useVerticesArray{ get{ return useDiscreteSolution;}}   // Padded container of all vertices generated from TriMesh
    private bool useContinuousSolution{ get{ return !useDiscreteSolution;}}
    private bool isFirstLayerContinuous{    // Used to pack the first layer of boxes onto the bottom of the bin; only possible when useContinuousSolution is True
        get{if (useContinuousSolution) return _isFirstLayerContinuous; else return false;}
        set{}}
    // Using OneHotEncoding in Attention entities; padding not required unless Curriculum learning is applied AND the same boxes are re-used
    private bool useOneHotEncoding{ get{ return (useAttention && _useOneHotEncodingForAttention);}}  
    private bool usePadding{                    // Padding is used when activating Curriculum Learning (with varying number of boxes between lessons).
        get{                                    // Padding does not require one-hot encoding. That would only be useful when the same boxes are re-used in the next
            if (useCurriculum) return true;     //   next Curriculum lesson. Otherwise, using OneHotEncoding just mixes up the identities of the boxes from the  
            else return false;                  //   previous lesson with the boxes from the new lesson.
        }                                       // When padding is used, take care to set the padding value to the maximum number of boxes out of all curriculum lessons.
    }                                           // Also take care to pass that value to the available observations and actions, incl. the attention-related observations.
    private int curriculum_ConfigurationGlobal{
        get {
            if (useCurriculum)
                if (useDiscreteSolution) return 0;
                else if (useContinuousSolution) return 2;
                else return -1;
            else return -1;
        }
        set {}
    }
    private int curriculum_ConfigurationLocal=-1; // used as temporary variable to store the curriculum configuration value


    private BufferSensorComponent m_BufferSensor;
    StatsRecorder m_statsRecorder; // adds stats to tensorboard

    public NNModel brain;
    private string m_BehaviorName{
        get{
            if (useDiscreteSolution) return "Discrete";
            else if (useContinuousSolution) return "Continuous";
            else return null;}
        set{}
    } 

    EnvironmentParameters m_ResetParams; // Environment parameters
    [HideInInspector] Rigidbody m_Agent; //cache agent rigidbody on initilization
    [HideInInspector] CombineMesh m_BackMeshScript;
    [HideInInspector] CombineMesh m_SideMeshScript;
    [HideInInspector] CombineMesh m_BottomMeshScript;

    private Vector3 initialAgentPosition;
    private Transform targetBox; // Box selected (by agent) to be placed in the bin
    private Transform targetBin; // Phantom target bin object where the box will be placed

    [HideInInspector] public int selectedBoxIdx; // Box selected 
    private Vector3 selectedRotation; // selectedRotation selected
    [HideInInspector] public Vector3 selectedVertex; // Vertex selected
    [HideInInspector] public Vector3 [] verticesArray; // (2*num_boxes + 1) vertices, each vertex a Vextor3 vector: total size = (2*num_boxes + 1)*3
    private int selectedVertexIdx = -1; 
    [HideInInspector] public List<Box> boxPool; // space: num boxes
    [HideInInspector] private List<int> maskedVertexIndices;
    [HideInInspector] public List<int> maskedBoxIndices; // list of organzed box indices
    [HideInInspector] public List<Vector3> historicalVerticesLog;
    [HideInInspector] public int VertexCount = 0;
    [HideInInspector] public Vector3 boxWorldScale;
    private int maxBoxNum;
   // [HideInInspector] public List<Blackbox> blackboxPool  = new List<Blackbox>();

    //public Dictionary<Vector3, int > allVerticesDictionary = new Dictionary<Vector3, int>();
    // [HideInInspector] public List<Vector3> backMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    // [HideInInspector] public List<Vector3> sideMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    // [HideInInspector] public List<Vector3> bottomMeshVertices = new List<Vector3>(); // space: 7n + 4 Vector3 vertices where n = num boxes
    private float total_x_distance; //total x distance between agent and target
    private float total_y_distance; //total y distance between agent and target
    private float total_z_distance; //total z distance between agent and target
    
    public BoxSpawner boxSpawner; // Box Spawner
    [HideInInspector] public SensorCollision sensorCollision;
    [HideInInspector] public SensorOuterCollision sensorOuterCollision;
    [HideInInspector] public SensorOverlapCollision sensorOverlapCollision;

    private bool initializeBrain = true;
    private bool isEpisodeStart;
    private bool isAfterOriginVertexSelected;
    //[HideInInspector] public bool isBlackboxUpdated;
    // public bool isVertexSelected;
    private bool isBoxSelected;
    private bool isRotationSelected;
    private bool isPickedup;
    private bool isDroppedoff;
    private bool isStateReset;
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
    private float current_empty_bin_volume;
    private float percent_filled_bin_volume;
    private float box_volume;
    [HideInInspector] public float binscale_x;
    [HideInInspector] public float binscale_y;
    [HideInInspector] public float binscale_z;
    [HideInInspector] public Vector3 origin;
    private int num_boxes_x;
    private int num_boxes_y;
    private int num_boxes_z;
    private int observable_size=8; // The observable size of an entity used in the attention mechanism
    private int max_observable_size; // The maximum possible observable size of an entity used in the attention mechanism
                                     // Default        : observable_size
                                     // OneHotEncoding : observable_size + number of boxes in current lesson
                                     // Padding        : observable_size + maximum number of boxes in all lessons
    // private BehaviorParameters m_BehaviorParameters;
    private double totalPossibleContanArea;

    public override void Initialize()
    {   
        // m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        // m_BehaviorParameters.BehaviorName = m_BehaviorName;
        // Unity.MLAgents.Sensors.VectorSensor sensor = new Unity.MLAgents.Sensors.VectorSensor(observable_size);
        // var ObservationSpec = sensor.GetObservationSpec();
        // m_BehaviorParameters.BrainParameters.VectorObservationSize = ObservationSpec.Shape[0];

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
            brain = modelOverrider.GetModelForBehaviorName(m_BehaviorName);
            m_BehaviorName = ModelOverrider.GetOverrideBehaviorName(m_BehaviorName);
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

        // Maximum possible number of boxes defined in a curriculum
        Debug.Log($"MAX BOX NUM: {maxBoxNum} ");
        if (useCurriculum){
            // Use Curriculum; padding is applied
            maxBoxNum = boxSpawner.maxBoxQuantity;
        }
        else{
            // No curriculum; one set of boxes; no need for padding
            int box_counter = 0;
            foreach(BoxSize b in boxSpawner.sizes) box_counter += 1;
            maxBoxNum = box_counter;
            Debug.Log($"MAX BOX NUM reloaded: {maxBoxNum}");
            // No curriculum; boxes generated randomly according to user-specified divisions along each dimension
            if (boxSpawner.useRandomGenerator){
                num_boxes_x = boxSpawner.num_boxes.x;
                num_boxes_y = boxSpawner.num_boxes.y;
                num_boxes_z = boxSpawner.num_boxes.z;
            }  
        }

        isEpisodeStart = true;

        // Initialize BufferSensor
        if (useAttention){
            m_BufferSensor = GetComponent<BufferSensorComponent>();

            // Set (in the Inspector) the maximum number of observables entities that the BufferSensor can observe
            m_BufferSensor.MaxNumObservables = maxBoxNum;

            // Set (in the Inspector) the size of each observable entity
            if (useOneHotEncoding) max_observable_size = maxBoxNum + observable_size;
            else                   max_observable_size = observable_size;
            m_BufferSensor.ObservableSize = max_observable_size;
        }
        else DestroyImmediate(m_Agent.GetComponent<BufferSensorComponent>());

        if (useVerticesArray) verticesArray = new Vector3[3*maxBoxNum]; // the 3x multiplication is to make sure enough space is allocated for the vertices of the bin
        // if (useVerticesArray) verticesArray = new Vector3[2*(2*maxBoxNum+1)]; // the 2x multiplication is to make sure enough space is allocated for the vertices of the bin
        // if (useVerticesArray) verticesArray = new Vector3[2*maxBoxNum+1]; // this is the exact space that the vertices array should need
        else                  verticesArray = new Vector3[0];

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
        sensor.AddObservation(current_empty_bin_volume);
        sensor.AddObservation(total_bin_volume);
        sensor.AddObservation(new Vector3(binscale_x,binscale_y,binscale_z));

        // Size of vector observation space: s1 = 1 + 1 + 1 * 3 = 5

        int j = -1;
        maskedBoxIndices = new List<int>();
        // Add all boxes sizes (selected boxes have sizes of 0s)
        foreach (Box box in boxPool) 
        {   
            Vector3 scaled_continuous_boxsize = new Vector3((box.boxSize.x/binscale_x), (box.boxSize.y/binscale_y), (box.boxSize.z/binscale_z));

            // Box index; update it here due to the "continue" statement below
            j++;

            // Add boxes to action mask. These boxes will be exempted from the next action/decision of the agent
            if (box.isOrganized){           
                // Already placed boxes are exempted from action but included in observation so that the agent "knows" about the 
                // positions of the boxes inside the bin
                maskedBoxIndices.Add(j);
            }
            else if (box.boxSize == Vector3.zero){
                // Mask zero boxes that may end up here; normally shouldn't happen
                    maskedBoxIndices.Add(j);
                // Skip to next box
                continue;
            }

            if (useAttention){
                int idx_cntr = 0;
                float[] listVarObservation = new float[max_observable_size];
                

                if (useOneHotEncoding){
                    // Used for variable size observations
                    int boxNum = int.Parse(box.rb.name);
                    // The first boxPool.Count are one hot encoding of the box
                    listVarObservation[boxNum] = 1.0f;
                    // Counter for remaining observations
                    idx_cntr = maxBoxNum;
                }
                // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                listVarObservation[idx_cntr]    = scaled_continuous_boxsize.x;
                listVarObservation[idx_cntr +1] = scaled_continuous_boxsize.y;
                listVarObservation[idx_cntr +2] = scaled_continuous_boxsize.z;
                // Add updated [volume]/[w*h*l] added to state vector
                listVarObservation[idx_cntr +3] = (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z);
                // Add updated box placement vertex
                listVarObservation[idx_cntr +4] = box.boxVertex.x;
                listVarObservation[idx_cntr +5] = box.boxVertex.y;
                listVarObservation[idx_cntr +6] = box.boxVertex.z;
                // Add if box is placed already: 1 if placed already and 0 otherwise
                listVarObservation[idx_cntr +7] = box.isOrganized ? 1.0f : 0.0f;;
                // Add updated box rotation
                // listVarObservation[idx_cntr+7] = box.boxRot[0];
                // listVarObservation[idx_cntr+8] = box.boxRot[1];
                // listVarObservation[idx_cntr+9] = box.boxRot[2];
                // listVarObservation[idx_cntr+10] = box.boxRot[3];

                m_BufferSensor.AppendObservation(listVarObservation);

                //Debug.Log($"XVD box:{box.rb.name}  |  vertex:{box.boxVertex}  |  x: {box.boxVertex.x * 23.5}  |  y: {box.boxVertex.y * 23.9}  |  z: {box.boxVertex.z * 59}");
                //Debug.Log($"XVB box:{box.rb.name}  |  vertex:{box.boxVertex}  |  dx: {scaled_continuous_boxsize.x*23.5}  |  dy: {scaled_continuous_boxsize.y*23.9}  |  dz: {scaled_continuous_boxsize.z*59}");
                //Debug.Log($"XVR box:{box.rb.name}  |  vertex:{box.boxVertex}  |  1: {box.boxRot[0]}  |  2: {box.boxRot[1]}  |  3: {box.boxRot[2]} | 4: {box.boxRot[3]}");

                // Size of vector observation space: s2 = 0
            }
            else{
                // Add updated box [x,y,z]/[w,h,l] dimensions added to state vector
                sensor.AddObservation(scaled_continuous_boxsize);
                // Add updated [volume]/[w*h*l] added to state vector
                sensor.AddObservation( (box.boxSize.x/binscale_x)*(box.boxSize.y/binscale_y)*(box.boxSize.z/binscale_z) );
                sensor.AddObservation (box.boxVertex);

                // Size of vector observation space: s2 = 1 * 3 + 1 + 1 * 3 = 7

            }
        }

        // Add array of vertices (selected vertices are 0s)
        if (useVerticesArray)
        {
            maskedVertexIndices = new List<int>();
            
            int i = 0;
            foreach (Vector3 vertex in verticesArray) 
            {   
                // vertices array is still getting fed vertex: (0, 0, 0) which is scaled_continuous_vertex: (-0.35, -0.02, -0.18)
                if (vertex == Vector3.zero)
                {
                    //Debug.Log($"MASK VERTEX LOOP INDEX:{i}");
                    maskedVertexIndices.Add(i);
                }
                else
                {
                    //Debug.Log($"XYX scaled_continuous_vertex: {scaled_continuous_vertex}");
                    Vector3 scaled_continuous_vertex = new Vector3(((vertex.x - origin.x)/binscale_x), ((vertex.y - origin.y)/binscale_y), ((vertex.z - origin.z)/binscale_z));
                    sensor.AddObservation(scaled_continuous_vertex); //add vertices to sensor observations
                }

                i++;

                // Size of vector observation space: s3 = 3
            }

            // Size of vector observation space: s3 * verticesArray.Length
        }

        // Inspector --> Hand --> Behavior parameters --> Vector Observation --> Space size:   s1 + s2 * n + s3 * verticesArray.Length
        //
        // , where n = maxBoxNumber  
        //   (i.e. number of boxes in the scene; note that when padding is used the number of boxes in the scene is larger than the number of boxes in the scene without padding)
        //
        // Thus, IN GENERAL:
        //     with    attention
        //          Inspector --> Hand --> Behavior parameters --> Vector Observation --> Space size:   5 + 0 * n + 3 * 3*n = 8 + 9 * n
        //     without attention
        //          Inspector --> Hand --> Behavior parameters --> Vector Observation --> Space size:   5 + 7 * n + 3 * 3*n = 8 + 16 * n
    }


    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        Debug.Log("MASK");

        // vertices action mask
        Debug.Log($"MASK VERTEX ARRAY size: {maskedVertexIndices.Count}");
        Debug.Log($"VERTICES    ARRAY size: {verticesArray.Length}");
        if (useDiscreteSolution)
        {
            if (isAfterOriginVertexSelected) {
                if (useVerticesArray){
                    // Debug.Log($"MASK VERTEX ARRAY size: {maskedVertexIndices.Count}");
                    foreach (int vertexIdx in maskedVertexIndices) 
                    {
                        //Debug.Log($"MASK VERTEX {vertexIdx}");
                        actionMask.SetActionEnabled(1, vertexIdx, false);
                    }
                }
            }
        }
        // box action mask
        Debug.Log($"MASK BOX ARRAY size: {maskedBoxIndices.Count}");
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

        var discreteActions   = actionBuffers.DiscreteActions;
        var continuousActions = actionBuffers.ContinuousActions;

        SelectBox(discreteActions[++j]); 

        if (useDiscreteSolution){
            // activate for discrete solution + adjust in Inspector by adding 1 more discrete branch
            SelectVertexDiscrete(discreteActions[++j]);
        }
        else if (useContinuousSolution){
            // activate for continuous solution
            var i = -1;
            SelectVertexContinuous(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        }

        SelectRotation(discreteActions[++j]);
    }


    /// <summary>
    /// This function is called at every time step
    /// <summary>
    void FixedUpdate() 
    {
        // All boxes packed? Reset episode
        if (boxPool.Count!=0 && boxes_packed == boxPool.Count)
        {
            Debug.Log("All boxes packed!");
            
            initiateNewEpisode();
        }

        // If reaches max step, reset episode 
        if (StepCount >= MaxStep) 
        {
            Debug.Log("Max step reached!");

            initiateNewEpisode();
        }

        // Start of episode
        if (isEpisodeStart)
        {
            Debug.Log("Reset new episode!");
            
            isEpisodeStart = false;

            // Reset agent and rewards
            SetResetParameters();
            // Reset states;
            StateReset();
            // Reset agent
            AgentReset();

            // Initialize curriculum & brain and generate boxes
            if (useCurriculum)
            {
                if (curriculum_ConfigurationGlobal != -1)
                {
                    ConfigureAgent(curriculum_ConfigurationGlobal);
                    curriculum_ConfigurationGlobal = -1;
                    initializeBrain = false;
                }
            }
            else
            {
                // boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, 0, 0, 0, seed, usePadding);
                // boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, 2, 2, 2, seed, usePadding);
                boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, num_boxes_x, num_boxes_y, num_boxes_z, seed, usePadding);
                Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }

            // For padding, make sure that the newly generated amount of boxes is not greater than the maxBoxNum memory allocated
            if (usePadding && maxBoxNum < boxPool.Count)
            {
                Debug.Log($" Increase maxBoxNum from {maxBoxNum} to {boxPool.Count} to fit all padded boxes in the bin");
                return;
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
            Debug.Log("Meshes combined!");

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
                Debug.Log("Update vertices array");

                isAfterOriginVertexSelected = true;
                // Vertices array of tripoints don't depend on the trimesh; Only update vertices list and vertices array when box is placed
                UpdateVerticesArray();
            }

            Debug.Log("Add new vertices to vertices array");


            float box_surface_area    = 2*boxWorldScale.x*boxWorldScale.y + 2*boxWorldScale.y * boxWorldScale.z + 2*boxWorldScale.x *  boxWorldScale.z;
            box_volume                = boxWorldScale.x * boxWorldScale.y * boxWorldScale.z;
            current_empty_bin_volume  = current_empty_bin_volume - box_volume;
            percent_filled_bin_volume = (1 - (current_empty_bin_volume/total_bin_volume)) * 100;

            // Add surface area reward
            if (useSurfaceAreaReward){
                float percent_contact_surface_area = sensorCollision.totalContactSA / box_surface_area;
                AddReward(percent_contact_surface_area * 50f);
                // Debug.Log($"RWDsa {GetCumulativeReward()} total reward | {percent_contact_surface_area * 50f} reward from surface area");
            }

            // Add combined area-volume reward
            if (useCombinedReward)
            {
                // float percent_filled_bin_combined = sensorCollision.totalContactSA * box_volume / ( total_bin_area * total_bin_volume );
                float percent_filled_bin_combined = (2*sensorCollision.totalContactSA/box_surface_area) * ( box_volume/total_bin_volume );
                AddReward(percent_filled_bin_combined * 100f);
                // Debug.Log($"RWDsa {GetCumulativeReward()} total reward | {percent_filled_bin_combined * 1000f} reward from surface area");

                // Increment stats recorder to match reward
                m_statsRecorder.Add("% Bin Volume & Area Filled", percent_filled_bin_combined, StatAggregationMethod.Average);
            }
            
            // Add volume reward
            if (useDenseReward)
            {
                AddReward((box_volume/total_bin_volume) * 1000f);
                // Debug.Log($"RWDx {GetCumulativeReward()} total reward | +{(box_volume/total_bin_volume) * 1000f} reward | current_empty_bin_volume: {current_empty_bin_volume} | percent bin filled: {percent_filled_bin_volume}%");

                // Increment stats recorder to match reward
                m_statsRecorder.Add("% Bin Volume Filled", percent_filled_bin_volume, StatAggregationMethod.Average);
            }


            Debug.Log("Request decision for next box");
            GetComponent<Agent>().RequestDecision();
            Academy.Instance.EnvironmentStep();
        }

        // If agent selects a box, it should move towards the box
        else if (isBoxSelected && isPickedup == false) 
        {
            // Agent moves to position of the box
            UpdateAgentPosition(targetBox);
            if ( Math.Abs(total_x_distance) < 0.1f && Math.Abs(total_z_distance) < 0.1f ) 
            {
                Debug.Log("Box picked up!");
                PickupBox();
            }
        }

        //if agent is carrying a box it should move towards the selected position
        else if (isPickedup && isRotationSelected && isDroppedoff == false) 
        {

            UpdateBoxPosition();
            // Agent moves (with the box) to selected position inside the bin
            UpdateAgentPosition(targetBin);

            UpdateTargetBox();
            //if agent is close enough to the position, it should drop off the box
            if ( Math.Abs(total_x_distance) < 2f && Math.Abs(total_z_distance) < 2f ) 
            {
                if (sensorCollision.passedGravityCheck && sensorOuterCollision.passedBoundCheck && sensorOverlapCollision.passedOverlapCheck)
                {
                    DropoffBox();
                    boxes_packed++;
                 
                    Debug.Log("Box dropped off!");
                }
                else
                {
                    Debug.Log("Box dropped off in wrong position! Failed physics checks!");

                    if (usePenaltyReward)
                    {
                        SetReward(current_empty_bin_volume/total_bin_volume * -100f);
                    }

                    initiateNewEpisode();
                }
            }
        }

        else { return;}
    }

    public void initiateNewEpisode(){
        // Reset curriculum brain & box generation, end episode, reset flag for new episode
        
        Debug.Log($"Initiating new episode");

        if (useSparseReward){
            if      (current_empty_bin_volume/total_bin_volume < 0.15f) AddReward(2500f); 
            else if (current_empty_bin_volume/total_bin_volume < 0.10f) AddReward(2500f);
            else if (current_empty_bin_volume/total_bin_volume < 0.05f) AddReward(2500f);
        }

        if (useCurriculum){curriculum_ConfigurationGlobal = curriculum_ConfigurationLocal;}
        isEpisodeStart = true;
        // Debug.Log($"EPISODE {CompletedEpisodes} START TRUE AFTER FAILING PHYSICS TEST");

        EndEpisode();
    }
    

    /// <summary>
    /// Updates agent position relative to the target position
    ///</summary>
    void UpdateAgentPosition(Transform target) 
    {
        // Move agent towards the target position with specified PackSpeed
        total_x_distance = target.position.x- m_Agent.position.x;
        total_y_distance = target.position.y- m_Agent.position.y;
        total_z_distance = target.position.z- m_Agent.position.z;
        var current_agent_x = m_Agent.position.x;
        var current_agent_y = m_Agent.position.y;
        var current_agent_z = m_Agent.position.z;   
        this.transform.position = new Vector3(current_agent_x + total_x_distance/packSpeed, 
                                              target.position.y, 
                                              current_agent_z + total_z_distance/packSpeed);   

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
        var tripoint_redx   = new Vector3(selectedVertex.x + boxWorldScale.x, selectedVertex.y,                 selectedVertex.z);                   // x red side tripoint
        var tripoint_greeny = new Vector3(selectedVertex.x,                   selectedVertex.y+boxWorldScale.y, selectedVertex.z);                   // y green bottom tripoint 
        var tripoint_bluez  = new Vector3(selectedVertex.x,                   selectedVertex.y,                 selectedVertex.z+boxWorldScale.z);   // z blue back tripoint 

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
                // only if historicVerticesArray doesnt already contain the tripoint, add it to the vertices array
                Vector3 scaled_continuous_vertex = new Vector3((tripoints_list[idx].x - origin.x)/binscale_x,  (tripoints_list[idx].y - origin.y)/binscale_y,  (tripoints_list[idx].z - origin.z)/binscale_z);
                //Vector3 rounded_scaled_vertex = new Vector3((float)Math.Round(scaled_continuous_vertex.x, 2), (float)Math.Round(scaled_continuous_vertex.y, 2), (float)Math.Round(scaled_continuous_vertex.y, 2));
                //Debug.Log($"VACx historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false: {historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false} | scaled_continuous_vertex: {scaled_continuous_vertex} ");

                // if the scaled_continuous_vertex is not already in the historicalVerticesLog, add it to the vertices array
                if ( historicalVerticesLog.Exists(element => element == scaled_continuous_vertex) == false )
                {
                    // Debug.Log($"TPX idx:{idx} | tripoint add to tripoints_list[idx]: {tripoints_list[idx]} | selectedVertex: {selectedVertex}") ;

                    // Add scaled tripoint_vertex to vertices array
                    Debug.Log($" VERTICES ARRAY lenght: {verticesArray.Length} | VertexCount: {VertexCount}");
                    verticesArray[VertexCount] = scaled_continuous_vertex;
                    historicalVerticesLog.Add(scaled_continuous_vertex);
                    
                    VertexCount ++;
                    Debug.Log($"new VERTEX COUNT IS {VertexCount}");

                }
            }
            }
            }
        }
    }

    public void SelectVertexDiscrete(int action_SelectedVertexIdx) 
    {

        // assign selected vertex where next box will be placed, selected from brain's actionbuffer (inputted as action_SelectedVertex)
        selectedVertexIdx = action_SelectedVertexIdx;
        var scaled_selectedVertex = verticesArray[action_SelectedVertexIdx];
        boxPool[selectedBoxIdx].boxVertex = scaled_selectedVertex;

        selectedVertex =  new Vector3(((scaled_selectedVertex.x* binscale_x) + origin.x), ((scaled_selectedVertex.y* binscale_y) + origin.y), ((scaled_selectedVertex.z* binscale_z) + origin.z));
        // Debug.Log($"SVX Discrete Selected VerteX: {selectedVertex}");
        // isVertexSelected = true;

    }

    public void SelectVertexContinuous(float action_SelectedVertex_x, float action_SelectedVertex_y, float action_SelectedVertex_z) 
    {
        action_SelectedVertex_x = (action_SelectedVertex_x + 1f) * 0.5f;
        action_SelectedVertex_y = (action_SelectedVertex_y + 1f) * 0.5f;
        action_SelectedVertex_z = (action_SelectedVertex_z + 1f) * 0.5f;
        // Debug.Log($"action_SelectedVertex_x ================== {action_SelectedVertex_x}");


        if (isFirstLayerContinuous)
        {
            selectedVertex = new Vector3(((action_SelectedVertex_x* binscale_x) + origin.x), 0.5f, ((action_SelectedVertex_z* binscale_z) + origin.z));
            boxPool[selectedBoxIdx].boxVertex = new Vector3(action_SelectedVertex_x, action_SelectedVertex_y, action_SelectedVertex_z);
            // Debug.Log($"SVX Continuous Selected VerteX: {selectedVertex}");
        }
        else{
            selectedVertex = new Vector3(((action_SelectedVertex_x* binscale_x) + origin.x), ((action_SelectedVertex_y* binscale_y) + origin.y), ((action_SelectedVertex_z* binscale_z) + origin.z));
            boxPool[selectedBoxIdx].boxVertex = new Vector3(action_SelectedVertex_x, action_SelectedVertex_y, action_SelectedVertex_z);
            // Debug.Log($"SVX Continuous Selected VerteX: {selectedVertex}");
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
                
            // 3: Calc Position
            Vector3 position = new Vector3( (selectedVertex.x + (magnitudeX * directionX)), (selectedVertex.y + (magnitudeY * directionY)), (selectedVertex.z + (magnitudeZ * directionZ)) );
            // Debug.Log($"UVP Updated Box Position: {position}");

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
        // Debug.Log($"SBB Selected Box selectedBoxIdx: {selectedBoxIdx}");
        // Debug.Log($"BOX POOL COUNT = {boxPool.Count()}");
        // Debug.Log($"action_selectedBox = {action_SelectedBox}");
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
            // Debug.Log($"SelectRotation() called with rotation (90, 0, 0)");
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
            // Debug.Log($"SelectRotation() called with rotation (0, 90, 0)");
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
            // Debug.Log($"SelectRotation() called with rotation (0, 0, 90)");
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
            // Debug.Log($"SelectRotation() called with rotation (0, 90, 90)");
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
            // Debug.Log($"SelectRotation() called with rotation (90, 0, 90)");
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
                // Remove consumed selectedVertex from vertices array (since another box cannot be placed there)
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

        // isVertexSelected = false;
        isBoxSelected      = false;
        isRotationSelected = false;
        isPickedup         = false;
        isDroppedoff       = false;

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
    
        isBackMeshCombined   = false;
        isSideMeshCombined   = false;
        isBottomMeshCombined = false;

        // Reset reward
        SetReward(0f);

        // Reset number of packed boxes
        boxes_packed = 0;

        // Reset current bin volume
        current_empty_bin_volume = total_bin_volume;

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

            // Reset vertex count
            VertexCount = 0;
        }
    }


    /// Configures the agent. Given an integer config, difficulty level will be different and a different brain will be used.
    void ConfigureAgent(int n) 
    {
        // Debug.Log($"BBN BRAIN BEHAVIOR NAME: {m_BehaviorName}");

        if (useCurriculum && initializeBrain)
        {
            SetModel(m_BehaviorName, brain);  
        }

        if (n==0) 
        // DISCRETE
        {
            if (Academy.Instance.EnvironmentParameters.GetWithDefault(m_BehaviorName, 0.0f) == 0.0f)
            {
                boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, 4, 4, 6, seed, usePadding);
                // Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault(m_BehaviorName, 1.0f) == 1.0f)
            {
                boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, 8, 8, 12, seed+1, usePadding);
                // Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
            if (Academy.Instance.EnvironmentParameters.GetWithDefault(m_BehaviorName, 2.0f) == 2.0f)
            {
                boxSpawner.SetUpBoxes(boxSpawner.box_type, boxSpawner.pickRandom, 16, 16, 24, seed+2, usePadding);
                // Debug.Log($"BXS BOX POOL COUNT: {boxPool.Count}");
            }
        }
        else if (n==2)
        // CONTINUOUS
        {
            if (Academy.Instance.EnvironmentParameters.GetWithDefault(m_BehaviorName, 1.0f) == 1.0f)
            {
                // pending setup
            }
            else if (Academy.Instance.EnvironmentParameters.GetWithDefault(m_BehaviorName, 2.0f) == 2.0f)
            {
                // pending setup
            }  
        }
    }

}
