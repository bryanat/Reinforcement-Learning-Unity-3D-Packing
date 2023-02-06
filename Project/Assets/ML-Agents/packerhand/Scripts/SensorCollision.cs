using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Check for center of gravity

public class SensorCollision : MonoBehaviour
{
    public PackerHand agent;

    public float fallingThreshold = 0.2f;

    public float distance = 0f;

    public bool passedGravityCheck;

    private RaycastHit hit;


    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 3);

    }



    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"OCC COLLISION OBJECT NAME IS {collision.gameObject.name}");
        GetHitDistance();
        // if fails gravity check
        // this loop should only be executed once
        Debug.Log($"SCS {gameObject.name} distance: {distance}");  
        if (distance> fallingThreshold) 
        {
            int failedBoxId = int.Parse(gameObject.name.Substring(7));
            passedGravityCheck = false;
            // agent.AddReward(-1f);
            Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedGravityCheck: {passedGravityCheck}");
            Debug.Log($"SCS {gameObject.name} FAILED GRAVITY CHECK --- RESET TO SPAWN POSITION");  
            // destroy test box  
            Destroy(gameObject);
        }  
        else 
        {
            passedGravityCheck = true;
            Debug.Log($"SCS {gameObject.name} PASSED GRAVITY CHECK");  
            Destroy(gameObject);
        }
    }



    void GetHitDistance()
     {
         // Bit shift the index of the layer 6 to get a bit mask
        int layerMask = 1 << 6;
        // This would cast rays only against colliders in layer 6.
        Vector3 boxBottomCenter = new Vector3(transform.position.x, transform.position.y-transform.localScale.y*0.5f, transform.position.z);
         //Ray downRay = new Ray(boxBottomCenter, Vector3.down); // this is the downward ray from box bottom to ground
         if (Physics.Raycast(boxBottomCenter, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
         {
            distance = hit.distance;
            Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
         }
     }
    
     
}