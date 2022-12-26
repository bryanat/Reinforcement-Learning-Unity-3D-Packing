using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using static PickupScript;
//using static SensorDetectBox;

namespace Boxes2 {

public class Box2 : MonoBehaviour
{


    public PickupScript2 ps; 

    //public SensorDetectBox sdb;
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public Quaternion StartingRot;


    public Bounds areaBounds;

    public BoxSpawner2 boxSpawnerRef;


    public void ResetBoxes(Box2 box)
    {
        //Reset box position
        box.rb.transform.position = new Vector3(0, 5, 0);//GetRandomSpawnPos();
        
        //Reset box's PickupScript
        box.ps.isHeld = false;
        box.ps.isOrganized = false;


    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// Cannot overlap with the agent or overlap with the bin area
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        areaBounds = boxSpawnerRef.ground.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);

            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
            randomSpawnPos = boxSpawnerRef.ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }


}



public class BoxSpawner2 : MonoBehaviour {



    [HideInInspector]
    public List<Box2> boxPool = new List<Box2>();

    /// <summary>
    /// The ground.
    /// This will be set manually here because Box2 class propoerties do not appear in Inspector
    /// </summary>
    public GameObject ground;



    public void SetUpBoxes() {

        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 

        //temporary box sizes array (to be fed from json later)
        float[][] sizes = new float[][] {
            new float[] { 1.0f, 2.0f, 3.0f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 5.0f, 10.0f, 8.0f }};
        
        foreach(var size in sizes) {
            // Create GameObject box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(size[0], size[1], size[2]); 
            var position = new Vector3(0, 5, 0); //GetRandomSpawnPos()
            box.transform.position = position;
            // Add compoments to GameObject box
            box.AddComponent<PickupScript2>();
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            box.tag = "box";
            // Transfer GameObject box properties to Box object 
            var newBox = new Box2{
                rb = box.transform.GetComponent<Rigidbody>(), 
                ps = box.transform.GetComponent<PickupScript2>(),
                startingPos = box.transform.position,
                boxSize = box.transform.localScale
            };
            // Add box to box pool
            boxPool.Add(newBox);          
        }
    }
 

    }
}
