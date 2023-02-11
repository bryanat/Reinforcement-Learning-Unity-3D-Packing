using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Physics check for protrusion (box unrealistic placement sticking out of bin)
public class SensorOuterCollision : MonoBehaviour
{
    [HideInInspector] public PackerHand agent;
    public bool passedBoundCheck = true;


    // if box collides with the outerbin then it is protruding through the inside of the bin and will be reset, box should not touch outerbin
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "binopening") 
        {
            collision.gameObject.tag = "outerbin";
        }
        // if a testbox physically penetrates the inner bin walls and touches the outer bin walls then reset the box (impossible placement)
        if (collision.gameObject.tag == "outerbin") 
        {
            // reset box, through passedBoundCheck flag that agent uses to reset box and pickup a new box 
            passedBoundCheck = false;
            //agent.AddReward(-1f);
            //Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedBoundCheck: {passedBoundCheck}");
            Debug.Log($"SCS {name} FAILED PROTRUSION TEST");
        }  
    }  
}
