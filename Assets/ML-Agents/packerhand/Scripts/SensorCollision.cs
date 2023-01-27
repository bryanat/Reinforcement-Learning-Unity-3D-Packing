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

    void Start()
    {
        // instantiate the Collider component
        c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)
        // instantiate the MeshCollider component from Collider component
        // MeshCollider mc = c.GetComponent<MeshCollider>();
    }


    void Update()
    {

    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name.StartsWith("clone")) {
            Debug.Log($"PHYSICS SWITCHED ON FOR {collision.transform.name}");
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb.velocity.y<-0.1f | rb.velocity.x<-0.1f | rb.velocity.z<-0.1f) {
                //if fails gravity test, send box back and punishes agent for impossible box placement
                //isGravityCheckPassed = false;
                agent.carriedObject.parent = null;
                int failedBoxIdx = int.Parse(collision.transform.name.Substring(5));
                agent.carriedObject.position = agent.boxPool[failedBoxIdx].startingPos;
                Box.organizedBoxes.Remove(failedBoxIdx);
                agent.StateReset();
                agent.isVertexSelected = true;
                Debug.Log($"SCS {collision.gameObject.name} FAILED GRAVITY CHECK --- RESET TO SPAWN POSITION");
                
            }
            else {
                Debug.Log($"SCS {collision.gameObject.name} PASSED GRAVITY CHECK");
            }
            Destroy(collision.gameObject);
        } 
    }
}