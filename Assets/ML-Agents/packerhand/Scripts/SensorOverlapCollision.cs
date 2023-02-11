using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Physics check for overlap (box unrealistic placement overlapping with another box)
public class SensorOverlapCollision : MonoBehaviour
{
    [HideInInspector] public PackerHand agent;
    public bool passedOverlapCheck = true;

    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 3);
    }


    // if box 
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"CBO {collision.gameObject.name}               |          overlap with side: {name} ");

        // if convex mesh does not have thickness, it will have holes and entrances
        // for overlap check to work, mesh has to have certain thickness
        if (collision.gameObject.tag == "bin" | collision.gameObject.tag == "pickupbox") 
        {
            // reset box, through failing passedOverlapCheck flag that agent uses to reset box and pickup a new box when false
            passedOverlapCheck = false;
            //agent.AddReward(-1f);
            //Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedOverlapCheck: {passedOverlapCheck}");
            Debug.Log($"SCS {name} FAILED OVERLAP CHECK");
            Destroy(gameObject);
        }         
    }  
}
