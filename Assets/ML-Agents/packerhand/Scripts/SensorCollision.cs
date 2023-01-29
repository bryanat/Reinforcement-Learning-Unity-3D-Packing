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

    public float fallingThreshold = 2f;

    public float distance = 0f;

    public float max_topple_angle;

    public bool enteredCollisionWithBottom = false;

    private RaycastHit hit;


    void Start()
    {

        // float y_direction = gameObject.transform.localScale.y*0.5f;
        // float x_direction = gameObject.transform.localScale.x*0.5f;
        // max_topple_angle = Mathf.Tan(x_direction/y_direction);
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 3);

    }


    void Update()
     {
        if (enteredCollisionWithBottom) {
            GetHitDistance();
        }
     }
    



    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name == "BinIso20Bottom") {
            enteredCollisionWithBottom = true;
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            // if fails gravity check
            // this loop should only be executed once
            if (distance > fallingThreshold) 
            {
                // detach box from agent
                agent.targetBox.parent = null;
                int failedBoxIdx = int.Parse(gameObject.name.Substring(7));
                // add back rigidbody and collider
                agent.targetBox.gameObject.AddComponent<Rigidbody>();
                agent.targetBox.gameObject.AddComponent<BoxCollider>();
                // reset to starting position
                agent.targetBox.position = agent.boxPool[failedBoxIdx].startingPos;
                // remove from organized list to be picked again
                Box.organizedBoxes.Remove(failedBoxIdx);
                // reset states
                agent.StateReset();
                // settting isBlackboxUpdated to true allows another vertex to be selected
                agent.isBlackboxUpdated = true;
                // setting isVertexSelected to true keeps the current vertex and allows another box to be selected
                //agent.isVertexSelected = true;
                Debug.Log($"SCS {gameObject.name} FAILED GRAVITY CHECK --- RESET TO SPAWN POSITION");  
                // destroy test box  
                Destroy(gameObject);
            }  
            //rb.isKinematic = true;
        } 
    }


    void GetHitDistance()
     {
        Vector3 boxBottomCenter = new Vector3(transform.position.x, transform.position.y-transform.localScale.y, transform.position.z);
         Ray downRay = new Ray(boxBottomCenter, Vector3.down); // this is the downward ray from box bottom to ground
         if (Physics.Raycast(downRay, out hit))
         {
            distance = hit.distance;
            Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM {gameObject.name} TO {hit.transform.name} IS: {distance}");
         }
     }
    
     
}