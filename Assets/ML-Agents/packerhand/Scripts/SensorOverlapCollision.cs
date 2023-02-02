using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Checks for overlap
public class SensorOverlapCollision : MonoBehaviour
{
    public PackerHand agent;
    public bool passedOverlapCheck = true;
    private RaycastHit hit;


    void Start()
    {
        // This destroys the test box 3 unity seconds after creation 
        Destroy(gameObject, 3);
    }


    void Update()
    {

    }
    

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"CBO {collision.gameObject.name}               |          overlap with side: {name} ");
        if (collision.gameObject.tag == "bin" | collision.gameObject.tag == "pickupbox") 
        {
            passedOverlapCheck = false;
            // agent.AddReward(-1f);
            Debug.Log($"RWD {agent.GetCumulativeReward()} total reward | -1 reward from passedOverlapCheck: {passedOverlapCheck}");
            Debug.Log($"CBOx {name} with SensorOverlapCollision script reset due to collision with: {collision.gameObject.name}");
            Destroy(gameObject);
        }         
    }
    
     
}
