using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Check for protrusion

public class SensorOuterCollision : MonoBehaviour
{
    public PackerHand agent;
    public bool passedBoundCheck = true;


    void Start()
    {

    }


    void Update()
    {

    }
    

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "binopening") 
        {
            collision.gameObject.tag = "outerbin";
        }
        // if a testbox physically penetrates the inner bin walls and touches the outer bin walls then reset the box (impossible placement)
        if (collision.gameObject.tag == "outerbin") 
        {
            // reset box
            passedBoundCheck = false;
            agent.AddReward(-1f);
            Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedBoundCheck: {passedBoundCheck}");
            Debug.Log($"CTU {collision.gameObject.name} box reset due to collision with outer bin side: {name} ");
        } 
        
    }
    
     
}
