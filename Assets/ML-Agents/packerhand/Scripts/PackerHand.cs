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
    public override void CollectObservations(VectorSensor sensor)
    {

        //position of target relative to target
        //if (target!=null) {
            sensor.AddObservation(target.transform.position);
        //}
        //observation of boxes when agent does not have a box
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
    
    public Transform SelectTarget(int x) {
        return m_Box.boxPool[x].rb.transform;
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
    


	////This is where the agent learns to move its joints and where it learns what is its next target to pick
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var j = -1;

        //var continuousActions = actionBuffers.ContinuousActions;
        var discreteActions = actionBuffers.DiscreteActions;
        

        SelectTarget(discreteActions[++j]); 

        //AddReward(distance);
        float distance = Vector3.Distance(target.transform.position, this.transform.position);

        SetReward(1/(distance*distance)*0.01f);
       

    }

    /// <summary>
    ////Agent touched the target
    ///may need to change to when the distance is close enough so agent does not bump into it and fall down
    ///</summary>
     public void TouchedTarget()
     {
         //AddReward(1f);
         print("Got to box!!!!!");
         PickUpBox();
     }


    /// <summary>
    ////Box got dropped off
    ///</summary>
    public void DroppedBox()
    { 
        AddReward(5f);
        print("Box dropped in bin!!! 5 pt added");

        // By marking an agent as done AgentReset() will be called automatically.
        // EndEpisode();
    }

    /// <summmary>
    ////Agent got to the bin
    /// </summary>
    public void GotToBin() 
    {
        AddReward(0.5f);
        print("Agent got to bin!!!! 0.5 pt added");
    }
    public void AgentReset() 
    {
        m_Agent.position = new Vector3(0, 5, 0);
        m_Agent.velocity = Vector3.zero;
        m_Agent.angularVelocity = Vector3.zero;
    }


    public void SetResetParameters()
    {
        AgentReset();
    }
}




////2. QUESTION: IF THE TARGET IS SET TO ONE OF THE BOXES AND THE CARRIED OBJECT IS SET TO TARGET, WILL THE OBSERVATION BE COLLECTED ON THIS BOX STILL?
        