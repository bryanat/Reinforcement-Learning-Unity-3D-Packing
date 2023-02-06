using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class WalkingTeddy3 : Agent
{
    [Header("Walk Speed")]
    [Range(0.1f, 10)]
    [SerializeField]
    //The walking speed to try and achieve
    private float m_TargetWalkingSpeed = 10;

    public float MTargetWalkingSpeed // property
    {
        get { return m_TargetWalkingSpeed; }
        set { m_TargetWalkingSpeed = Mathf.Clamp(value, .1f, m_maxWalkingSpeed); }
    }

    const float m_maxWalkingSpeed = 10; //The max walking speed

    //Should the agent sample a new goal velocity each episode?
    //If true, walkSpeed will be randomly set between zero and m_maxWalkingSpeed in OnEpisodeBegin()
    //If false, the goal velocity will be walkingSpeed
    public bool randomizeWalkSpeedEachEpisode;

    //The direction an agent will walk during training.
    private Vector3 m_WorldDirToWalk = Vector3.right;

    [Header("Target To Walk Towards")] public Transform target; //Target the agent will walk towards during training.

    [Header("Body Parts")] public Transform Pelvis;
    public Transform Chest;
    public Transform Spine;
    public Transform Head;
    public Transform LThigh;
    public Transform LShin;
    public Transform LFoot;
    public Transform RThigh;
    public Transform RShin;
    public Transform RFoot;
    public Transform LUpperArm;
    public Transform LForearm;
    public Transform LHand;
    public Transform RUpperArm;
    public Transform RForearm;
    public Transform RHand;

    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    OrientationCubeController m_OrientationCube;

    //The indicator graphic gameobject that points towards the target
    DirectionIndicator m_DirectionIndicator;
    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_OrientationCube = GetComponentInChildren<OrientationCubeController>();
        m_DirectionIndicator = GetComponentInChildren<DirectionIndicator>();

        //Setup each body part
        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(Pelvis);
        m_JdController.SetupBodyPart(Chest);
        m_JdController.SetupBodyPart(Spine);
        m_JdController.SetupBodyPart(Head);
        m_JdController.SetupBodyPart(LThigh);
        m_JdController.SetupBodyPart(LShin);
        m_JdController.SetupBodyPart(LFoot);
        m_JdController.SetupBodyPart(RThigh);
        m_JdController.SetupBodyPart(RShin);
        m_JdController.SetupBodyPart(RFoot);
        m_JdController.SetupBodyPart(LUpperArm);
        m_JdController.SetupBodyPart(LForearm);
        m_JdController.SetupBodyPart(LHand);
        m_JdController.SetupBodyPart(RUpperArm);
        m_JdController.SetupBodyPart(RForearm);
        m_JdController.SetupBodyPart(RHand);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        Pelvis.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
        print(Pelvis.rotation);

        UpdateOrientationObjects();

        //Set our goal walking speed
        MTargetWalkingSpeed =
            randomizeWalkSpeedEachEpisode ? Random.Range(0.1f, m_maxWalkingSpeed) : MTargetWalkingSpeed;

        SetResetParameters();
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(bp.rb.position - Pelvis.position));

        if (bp.rb.transform != Pelvis && bp.rb.transform != LHand && bp.rb.transform != RHand)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = m_OrientationCube.transform.forward;

        //velocity we want to match
        var velGoal = cubeForward * MTargetWalkingSpeed;
        //ragdoll's avg vel
        var avgVel = GetAvgVelocity();

        //current ragdoll velocity. normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        //avg body vel relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(avgVel));
        //vel goal relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(Pelvis.forward, cubeForward));
        sensor.AddObservation(Quaternion.FromToRotation(Head.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(m_OrientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        var continuousActions = actionBuffers.ContinuousActions;
        bpDict[Chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[Spine].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[LThigh].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[RThigh].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[LShin].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[RShin].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[RFoot].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[RFoot].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[LUpperArm].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[RUpperArm].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[LForearm].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[RForearm].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[Head].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);

        //update joint strength settings
        bpDict[Chest].SetJointStrength(continuousActions[++i]);
        bpDict[Spine].SetJointStrength(continuousActions[++i]);
        bpDict[Head].SetJointStrength(continuousActions[++i]);
        bpDict[LThigh].SetJointStrength(continuousActions[++i]);
        bpDict[LShin].SetJointStrength(continuousActions[++i]);
        bpDict[LFoot].SetJointStrength(continuousActions[++i]);
        bpDict[RThigh].SetJointStrength(continuousActions[++i]);
        bpDict[RShin].SetJointStrength(continuousActions[++i]);
        bpDict[RFoot].SetJointStrength(continuousActions[++i]);
        bpDict[LUpperArm].SetJointStrength(continuousActions[++i]);
        bpDict[LForearm].SetJointStrength(continuousActions[++i]);
        bpDict[RUpperArm].SetJointStrength(continuousActions[++i]);
        bpDict[RForearm].SetJointStrength(continuousActions[++i]);
    }

    //Update OrientationCube and DirectionIndicator
    void UpdateOrientationObjects()
    {
        m_WorldDirToWalk = target.position - Pelvis.position;
        //print(transform.position);
        m_OrientationCube.UpdateOrientation(Pelvis, target);
        if (m_DirectionIndicator)
        {
            m_DirectionIndicator.MatchOrientation(m_OrientationCube.transform);
        }
    }

    void FixedUpdate()
    {
        UpdateOrientationObjects();

        var cubeForward = m_OrientationCube.transform.forward;

        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * MTargetWalkingSpeed, GetAvgVelocity());

        //Check for NaNs
        if (float.IsNaN(matchSpeedReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" Pelvis.velocity: {m_JdController.bodyPartsDict[Pelvis].rb.velocity}\n" +
                $" maximumWalkingSpeed: {m_maxWalkingSpeed}"
            );
        }

        // b. Rotation alignment with target direction.
        //This reward will approach 1 if it faces the target direction perfectly and approach zero as it deviates
        var lookAtTargetReward = (Vector3.Dot(cubeForward, Head.forward) + 1) * .5F;

        //Check for NaNs
        if (float.IsNaN(lookAtTargetReward))
        {
            throw new ArgumentException(
                "NaN in lookAtTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" Head.forward: {Head.forward}"
            );
        }

        AddReward(matchSpeedReward * lookAtTargetReward);
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in m_JdController.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    //normalized value of the difference in avg speed vs goal walking speed.
    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, MTargetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / MTargetWalkingSpeed, 2), 2);
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
    }

    public void SetTorsoMass()
    {
        m_JdController.bodyPartsDict[Chest].rb.mass = m_ResetParams.GetWithDefault("Chest_mass", 8);
        m_JdController.bodyPartsDict[Spine].rb.mass = m_ResetParams.GetWithDefault("Spine_mass", 8);
        m_JdController.bodyPartsDict[Pelvis].rb.mass = m_ResetParams.GetWithDefault("Pelvis_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
    
    
    
  
}
