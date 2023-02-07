using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Check for center of gravity

public class SensorCollision : MonoBehaviour
{
    public PackerHand agent;

    public float fallingThreshold = 0.2f;

    public float distance = 0f;

    public HashSet<string> sides_list = new HashSet<string>();

    public float totalContactSA;

    public bool passedGravityCheck;

    private RaycastHit hit;


    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 3);

    }



    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"OCC COLLISION OBJECT NAME IS {collision.gameObject.name}");
        GetHitDistance();
        // get surface area of contact
        if (!sides_list.Contains(collision.gameObject.name))
        {
            GetSurfaceArea(collision.gameObject.name);
        }
        // if fails gravity check
        // this loop should only be executed once
        Debug.Log($"SCS {gameObject.name} distance: {distance}");  
        if (distance> fallingThreshold) 
        {
            int failedBoxId = int.Parse(gameObject.name.Substring(7));
            passedGravityCheck = false;
            agent.AddReward(-1f);
            //Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedGravityCheck: {passedGravityCheck}");
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


    void GetSurfaceArea(string side_name)
    {   
        // NOTE: for this to work, has to set unit box sides from 1 to 0.95//
        if (side_name == "BinIso20Side") {
            // collision with both biniso20side and left/right happened, count only once
            if (sides_list.Contains("left") | sides_list.Contains("right"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.z * agent.boxWorldScale.y;   
        }
        else if (side_name == "left" | side_name=="right")
        {
            // collision with both biniso20side and left/right happened, count only once
            if (sides_list.Contains("BinIso20Side"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.z * agent.boxWorldScale.y;   
        }
        else if (side_name == "BinIso20Bottom")
        {
            // collision with both biniso20bottom and bottom/top happened, count only once
             if (sides_list.Contains("bottom") | sides_list.Contains("top"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.z * agent.boxWorldScale.x;   
        }
        else if (side_name == "bottom" | side_name == "top")
        {
            // collision with both biniso20bottom and bottom/top happened, count only once
            if (sides_list.Contains("BinIso20Bottom"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.z * agent.boxWorldScale.x;  
        }
        else if (side_name == "BinIso20Back")
        {
            // collision with both biniso20back and back/front happened, count only once
             if (sides_list.Contains("back") | sides_list.Contains("front"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.y * agent.boxWorldScale.x;   
        }
        else if (side_name == "back" | side_name == "front")
        {
             // collision with both biniso20back and back/front happened, count only once
            if (sides_list.Contains("BinIso20Back"))
            {
                return;
            }
            totalContactSA += agent.boxWorldScale.y * agent.boxWorldScale.x;  
        }
        sides_list.Add(side_name);
        Debug.Log($"SSA {side_name} current surface area is: {totalContactSA}");
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
            Debug.DrawRay(boxBottomCenter, transform.TransformDirection(Vector3.down), Color.yellow);
            Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
         }
     }
    
     
}