using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
public class SensorCollision : MonoBehaviour
{
    public PackerHand agent;

    public float fallingThreshold = 0.5f;

    public float distance = 0f;

    public bool passedGravityCheck;

    private RaycastHit hit;


    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 5);

    }



    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"OCC COLLISION OBJECT NAME IS {collision.gameObject.name}");
        if (collision.gameObject.name == "BinIso20Bottom" | collision.gameObject.name == "top") {
            GetHitDistance();
            // if fails gravity check
            // this loop should only be executed once
            Debug.Log($"SCS {gameObject.name} distance: {distance}");  
            if (distance> fallingThreshold) 
            {
                int failedBoxId = int.Parse(gameObject.name.Substring(7));
                passedGravityCheck = false;
                //agent.BoxReset(failedBoxId, "failedGravityCheck");
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
    }



    void GetHitDistance()
     {
         // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;
        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;
        Vector3 boxBottomCenter = new Vector3(transform.position.x, transform.position.y-transform.localScale.y*0.5f, transform.position.z);
         //Ray downRay = new Ray(boxBottomCenter, Vector3.down); // this is the downward ray from box bottom to ground
         if (Physics.Raycast(boxBottomCenter, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
         {
            Debug.Log($"RCS RAYCAST HIT {hit.transform.name}");
            if (hit.transform.name == "top" | hit.transform.name == "BinIso20Bottom") {
                distance = hit.distance;
                Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
            }
         }
     }

    //  void OnDrawGizmos()
    // {
    //     // Draws a 5 unit long red line in front of the object
    //     Gizmos.color = Color.yellow;
    //     Vector3 position =   new Vector3(transform.position.x, transform.position.y-transform.localScale.y*0.5f, transform.position.z);
    //     Vector3 direction = transform.TransformDirection(Vector3.down) * 30f;
    //     Gizmos.DrawRay(position, direction);
    // }
    
     
}