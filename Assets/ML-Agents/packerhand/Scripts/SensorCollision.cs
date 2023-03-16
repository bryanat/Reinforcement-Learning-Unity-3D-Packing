using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Physics check for center of gravity (box unstable vertical position that may fall to ground)
public class SensorCollision : MonoBehaviour
{
    [HideInInspector] public PackerHand agent;

    public float fallingThreshold = 0.2f;

    public float distance = 0f;

    public HashSet<string> sides_list = new HashSet<string>();

    public float totalContactSA;

    public bool passedGravityCheck;

    [HideInInspector] private RaycastHit hit;


    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 2);
    }


    void OnCollisionEnter(Collision collision)
    {
        GetHitDistance();
        // get surface area of contact
        GetSurfaceArea(collision.gameObject.name);
        // if fails gravity check, this loop should only be executed once
        if (distance> fallingThreshold) 
        {
            int failedBoxId = int.Parse(name.Substring(7));
            // reset box, through failing passedGravityCheck flag that agent uses to reset box and pickup a new box when false
            passedGravityCheck = false;
            //Debug.Log($"SCS {name} FAILED GRAVITY CHECK");  
            // destroy test box  
            Destroy(gameObject);
        }  
        else 
        {
            passedGravityCheck = true;
            //Debug.Log($"SCS {name} PASSED GRAVITY CHECK");  
            Destroy(gameObject);
        }
    }


    //// THIS FUNCTION NEEDS TO BE TESTED MORE////
    void GetSurfaceArea(string side_name)
    { 
        // NOTE: for this to work, has to set unit box sides from 1 to 0.95//
        if (side_name == "BinIso20Side") 
        {
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
        //Debug.Log($"SSA {side_name} current surface area is: {totalContactSA}");
    }


    // Draws a Ray from bottom center of box, checking for collision, if no very short distance collision, then box is not supported by another box
    void GetHitDistance()
    {
        // Bit shift the index of the layer 6 to get a bit mask
        int layerMask = 1 << 6;
        // This would cast rays only against colliders in layer 6.
        Vector3 boxBottomCenter = new Vector3(transform.position.x, transform.position.y-transform.localScale.y*0.5f, transform.position.z);
        Vector3 leftRay = new Vector3(boxBottomCenter.x-transform.localScale.x*0.2f, boxBottomCenter.y, boxBottomCenter.z);
        Vector3 rightRay = new Vector3(boxBottomCenter.x+transform.localScale.x*0.2f, boxBottomCenter.y, boxBottomCenter.z+transform.localScale.z*0.2f);
        Vector3 frontRay = new Vector3(boxBottomCenter.x, boxBottomCenter.y, boxBottomCenter.z+transform.localScale.z*0.2f);
        Vector3 backRay = new Vector3(boxBottomCenter.x, boxBottomCenter.y, boxBottomCenter.z-transform.localScale.z*0.2f);
        if (Physics.Raycast(leftRay, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
        {
            distance = Mathf.Max(distance, hit.distance);
            //Debug.DrawRay(boxBottomCenter, transform.TransformDirection(Vector3.down), Color.yellow);
            //Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
        }
        if (Physics.Raycast(rightRay, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
        {
            distance = Mathf.Max(hit.distance, distance);
            //Debug.DrawRay(boxBottomCenter, transform.TransformDirection(Vector3.down), Color.yellow);
            //Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
        }
        if (Physics.Raycast(frontRay, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
        {
            distance = Mathf.Max(hit.distance, distance);
            //Debug.DrawRay(boxBottomCenter, transform.TransformDirection(Vector3.down), Color.yellow);
            //Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
        }
        if (Physics.Raycast(backRay, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
        {
            distance = Mathf.Max(hit.distance, distance);
            //Debug.DrawRay(boxBottomCenter, transform.TransformDirection(Vector3.down), Color.yellow);
            //Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
        }
    }       
}