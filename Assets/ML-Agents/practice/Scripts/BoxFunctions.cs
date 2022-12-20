using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using static PickupScript;
using static BinDetect;

namespace Boxes {

public class Box : MonoBehaviour
{

    public GameObject m_box;
    public Transform t;
    [HideInInspector]
    public BinDetect binDetect;

    public PickupScript ps; 
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public Quaternion StartingRot;
}




///////////////////////// NEED TO CHECK WHERE TO ATTACH THIS SCRIPT ///////////////////////////////////

public class BoxSpawner : MonoBehaviour {

    public PackerAgent agent; // idk if this stays here


    [HideInInspector]
    public List<Box> boxPool = new List<Box>();

    public GameObject ground;

    public GameObject m_box;



    public void SetUpBoxes(Bounds areaBounds) {
        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 
        float[,] sizes = new float[,] { { 1.0f, 2.0f, 3.0f }, { 3.0f, 2.0f, 3.0f } , { 5.0f, 2.0f, 3.0f }, { 7.0f, 2.0f, 3.0f }, { 5.0f, 5.0f, 5.0f } };


        foreach(var size in sizes) {
            var position = GetRandomSpawnPos(areaBounds);
            var box = new GameObject();
            box.AddComponent<Transform>();
            box.AddComponent<BinDetect>();
            box.AddComponent<PickupScript>();
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            box.transform.localScale = new Vector3(1.0f, 2.0f, 3.0f);
            box.transform.position = position;
            //box.binDetect.agent = agent;
            var box2 = new Box{m_box = box};
            if (box2.rb) {Debug.Log("RIGID BODY DDED DURING CREATION!!!!!");}
            boxPool.Add(box2);          
        }
    }





    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// Cannot overlap with the agent or overlap with the bin area
    /// </summary>
    public Vector3 GetRandomSpawnPos(Bounds areaBounds)
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);

            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }






    ///////////////////////////NEED TO CHECK ON HOW EACH COMPONENT IS RESSET//////////////////////
    public void ResetBoxes(Bounds areaBounds)
    {
        foreach(var box in boxPool) {
            box.rb.transform.position = GetRandomSpawnPos(areaBounds);
            // box.velocity = Vector3.zero;
            // box.angularVelocity = Vector3.zero;

        }
        // // Get a random position for the block.
        // block.transform.position = Vector3.zero;//GetRandomSpawnPos();

        // // Reset block velocity back to zero.
        // m_BlockRb.velocity = Vector3.zero;

        // // Reset block angularVelocity back to zero.
        // m_BlockRb.angularVelocity = Vector3.zero;
    }
 

    }
}
