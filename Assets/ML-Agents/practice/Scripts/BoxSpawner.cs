using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

///////////////////////// NEED TO CHECK WHERE TO ATTACH THIS SCRIPT ///////////////////////////////////

public class BoxSpawner : MonoBehaviour {

    public PackerAgent agent; // idk if this stays here


    public List<Box> boxPool = new List<Box>();

    public GameObject ground;


    public void SetUpBoxes(Bounds areaBound) {
        //for each box in json, get a list of box sizes as Vector3;
        //sizes = readJson(); 
        float[,] sizes = new float[,] { { 1.0f, 2.0f, 3.0f }, { 3.0f, 2.0f, 3.0f } , { 5.0f, 2.0f, 3.0f }, { 7.0f, 2.0f, 3.0f }, { 5.0f, 5.0f, 5.0f } };


        foreach(var size in sizes) {
            var position = GetRandomSpawnPos(areaBound);
            Debug.Log(size);

            ////// UNCOMMENT ////
            // var box = new Box {
            //     startingPos = position, // check
            //     // how to console.log size
            //     boxSize = new Vector3(size[0], size[1], size[2]) // boxSize is no longer error, but where is boxSize used?
            //     // boxSize = new Vector3(1.0f, 2.0f, 3.0f ) // boxSize is no longer error, but where is boxSize used?
            // };
            // boxPool.Add(box);
            
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
            box.transform.position = GetRandomSpawnPos(areaBounds);
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
