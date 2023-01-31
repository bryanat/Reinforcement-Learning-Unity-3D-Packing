using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
public class SensorOuterCollision : MonoBehaviour
{
    public PackerHand agent;


    void Start()
    {

    }


    void Update()
    {

    }
    

    void OnCollisionEnter(Collision collision)
    {
        // if a testbox physically penetrates the inner bin walls and touches the outer bin walls then reset the box (impossible placement)
        if (collision.gameObject.tag == "testbox") {
            int failedBoxId = int.Parse(gameObject.name.Substring(7));
            // reset box
            agent.BoxReset(failedBoxId, "failedGravityCheck");
            Debug.Log($"CTU {collision.gameObject.name} box reset due to collision with outer bin side: {name} ");
        } 
    }
    
     
}