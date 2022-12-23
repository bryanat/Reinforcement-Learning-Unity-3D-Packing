using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;
using Box = Boxes.Box;
using Boxes;
using static HandDetect;
public class PackerHand : Agent

{
    public GameObject ground;

    public GameObject binArea;

    //cache on initilization
    Rigidbody m_Agent; 

    [HideInInspector]
    public Transform carriedObject;

    private Vector3 distance = Vector3.right;

    [HideInInspector]
    public Transform target; //Target the agent will walk towards during training.

    public Transform hand;

    EnvironmentParameters m_ResetParams;
    BoxSpawner m_Box;

    public override void Initialize()
    {

        m_Box = GetComponentInChildren<BoxSpawner>();

        Debug.Log("++++++++++++++++++++BOX in INITIALIZE++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);

        
        // Cache the agent rigidbody
        m_Agent = GetComponent<Rigidbody>();

        //Create boxes
        m_Box.SetUpBoxes();
        
        //Setup Agent Detect
        HandDetect handDetect = this.GetComponent<HandDetect>();
        handDetect.agent = this; 

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }


    public override void OnEpisodeBegin()
    {

        //m_Box = GetComponentInChildren<BoxSpawner>();
        Debug.Log("++++++++++++++++++++BOX in ONEPISODEBEGIN++++++++++++++++++++++++++++++");
        Debug.Log(m_Box);

        //Reset boxes
        foreach (var box in m_Box.boxPool) {
            box.ResetBoxes(box);
        }

        //Update target and orientation
        Debug.Log("++++++++++++++++++++BOX POOL COUNT++++++++++++++++++++++++++++++++++++++++++");
        Debug.Log(m_Box.boxPool.Count);


        //Reset agent
        SetResetParameters();

    }


    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
    
        // Bin position
        sensor.AddObservation(binArea.transform.position); //(x, y, z)

        //Box size and position
        foreach (var box in m_Box.boxPool) {
            sensor.AddObservation(box.boxSize); //add box size to sensor observations
            sensor.AddObservation(box.rb.transform.position); //add box position to sensor observations
        }

        //Agent postiion
        sensor.AddObservation(this.transform.position);

        //Agent velocity
        sensor.AddObservation(m_Agent.velocity.x);
        sensor.AddObservation(m_Agent.velocity.z);

    }

    	////This is where the agent learns to move its joints and where it learns what is its next target to pick
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Debug.Log($"Timestep reward: {GetCumulativeReward()}");

        var j = -1;
        //var i = -1;


        var discreteActions = actionBuffers.DiscreteActions;
        //var continuousActions = actionBuffers.ContinuousActions;
    
        SelectTarget(discreteActions[++j]); 
        MoveAgent(discreteActions[++j]);

        ////////////////SelectPosition




        //this.transform.position.Set(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]);
        ///m_Agent.AddForce(new Vector3(this.transform.position.x+continuousActions[++i], 0, this.transform.position.z+continuousActions[++i]));
        if (target!=null) {
             // Move the agent using the action.

            // Add cumulative reward    
            float distance = Vector3.Distance(target.transform.position, this.transform.position);
            // if (distance<0.1f) {
            //     AddReward(0.5f);
            // }
            var x = 1/(distance*distance);
            Debug.Log($"REWARD FOR MOVING TOWARDS A TARGET IS:{x}");
            if (x>1) {
                x=1;
            }
            AddReward(x);
        }
    }


        /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(int action)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_Agent.AddForce(dirToGo, 
            ForceMode.VelocityChange);
    }
    
    public void SelectTarget(int x) {
        target = m_Box.boxPool[x].rb.transform;
   }
    public void PickUpBox() {
        //packer picks up target box not in bin, a small reward is added
        if (carriedObject==null) {
            carriedObject = target.transform;
            PickupScript pickupScript = carriedObject.GetComponent<PickupScript>();
            if (pickupScript!=null && !pickupScript.isOrganized) {
                pickupScript.isHeld = true;
                //NEEDS TO MAKE THE BOX DOESN'T TOUCH THE GROUND WHEN IT'S CARRIED SINCE COLLISION WITH GROUND IN BIN IS REWARDED 
                carriedObject.position = this.transform.position + this.transform.forward * 0.5f;
            }
            //change target to bin
            target = binArea.transform;
        }      
    }
        
  
    //WORK TO DO: CHECK THE PHYSICS AND CONTRAINTS WHEN STACKING BOXES, SET ROTATION OF BOX, ETC.
    public void DropoffBox(int x) {
        //TBD:  if agent wants to drop the box
        //drop off the box, when the box touches the bin area, reward is added
        if (carriedObject!=null) {
            PickupScript pickupScript = carriedObject.GetComponent<PickupScript>();
            pickupScript.isHeld = false;
            pickupScript.isOrganized = true;
            carriedObject.position = this.transform.position + this.transform.forward * 0.5f;
            carriedObject = null;   
        }
        AgentReset();
    }
    

    /// <summary>
    ////Agent touched the target
    ///may need to change to when the distance is close enough so agent does not bump into it and fall down
    ///</summary>
     public void TouchedTarget()
     {

        Debug.Log("REWARD IN TOUCHED TARGET IS CALLED");
         SetReward(1f);
         print("Got to box!!!!!");
         PickUpBox();
     }


    /// <summary>
    ////Box got dropped off
    ///</summary>
    public void DroppedBox()
    { 
        SetReward(5f);
        print("Box dropped in bin!!! 5 pt added");

        // By marking an agent as done AgentReset() will be called automatically.
        // EndEpisode();
    }

    /// <summmary>
    ////Agent got to the bin
    /// </summary>
    public void GotToBin() 
    {
        SetReward(1f);
        //////// if the agent moves to the bin without a box, it should have a negative reward /////
        print("Agent got to bin!!!! 1 pt added");
    }
    public void AgentReset() 
    {
        m_Agent.position = new Vector3(5, 0, 5);
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }


    public void SetResetParameters()
    {
        AgentReset();
    }
}




////2. QUESTION: IF THE TARGET IS SET TO ONE OF THE BOXES AND THE CARRIED OBJECT IS SET TO TARGET, WILL THE OBSERVATION BE COLLECTED ON THIS BOX STILL?


/////Rewarded: 
///on episode begin: negative reward proportional to the volumne inside the bin area 
///small rewards: walking towards the target box, picking up the target box, getting to the bin, putting bin inside bin area
///addreward vs setrewaard: add reward for getting to the next stage of actions, set reward at the beginning of each stage of actions, setreward > accumulated rewarded from previous stage