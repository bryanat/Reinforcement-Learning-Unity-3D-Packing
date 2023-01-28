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
    public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
    // public Rigidbody rb;
    public PackerHand agent;

    public float fallingThreshold = 1f;

    public float distance = 0f;

    public bool enteredCollisionWithBottom = false;

    private RaycastHit hit;


    // void Start()
    // {
    //     // instantiate the Collider component
    //     c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
    //     // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)
    //     // instantiate the MeshCollider component from Collider component
    //     // MeshCollider mc = c.GetComponent<MeshCollider>();


    // }


    void Update()
     {
        if (enteredCollisionWithBottom) {
            Debug.Log("BBB ENTERED COLLISION WITH BOTTOM");
            GetHitDistance();
        }
     }
    



    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name == "BinIso20Bottom") {
            enteredCollisionWithBottom = true;
            if (distance > fallingThreshold) 
            {
                //if fails gravity test, send box back and punishes agent for impossible box placement
                agent.carriedObject.parent = null;
                int failedBoxIdx = int.Parse(this.gameObject.name.Substring(5));
                agent.carriedObject.position = agent.boxPool[failedBoxIdx].startingPos;
                Box.organizedBoxes.Remove(failedBoxIdx);
                agent.StateReset();
                agent.isVertexSelected = true;
                Debug.Log($"SCS {this.gameObject.name} FAILED GRAVITY CHECK --- RESET TO SPAWN POSITION");
                
            }
            else 
            {
                Debug.Log($"SCS {this.gameObject.name} PASSED GRAVITY CHECK");
            }   
            //Destroy(this.gameObject);
        } 
    }


    void GetHitDistance()
     {
        Vector3 boxBottomCenter = new Vector3(transform.position.x, transform.position.y-transform.localScale.y, transform.position.z);
         Ray downRay = new Ray(boxBottomCenter, Vector3.down); // this is the downward ray from box to bottom mesh
         if (Physics.Raycast(downRay, out hit))
         {
            distance = hit.distance;
            Debug.Log($"RCS ENTERED RAYCAST HIT DISTANCE FROM BOX TO {hit.transform.name} IS: {distance}");
         }
     }
    
     
}