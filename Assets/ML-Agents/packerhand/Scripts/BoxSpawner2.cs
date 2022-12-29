using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace Boxes2 {

public class Box2 
{


    //public SensorDetectBox sdb;
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public void ResetBoxes(Box2 box)
    {
         // Reset box position
        box.rb.transform.position = box.startingPos;

    }


}



public class BoxSpawner2 : MonoBehaviour {



    [HideInInspector]
    public List<Box2> boxPool = new List<Box2>();
    public Dictionary<Box2, Vector3> boxMap = new Dictionary<Box2, Vector3>();

    /// <summary>
    /// The box area.
    /// This will be set manually in the Inspector
    /// </summary>
    public GameObject boxArea;


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
            var position = GetRandomSpawnPos();
            box.transform.position = position;
            // Add compoments to GameObject box
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            // Add tag: 0 signified disorganized outside the bin, 1 signifies organized inside the bin
            box.tag = "0";
            // Transfer GameObject box properties to Box object 
            var newBox = new Box2{
                rb = box.transform.GetComponent<Rigidbody>(), 
                startingPos = box.transform.position,
                boxSize = box.transform.localScale,
            };
            // Add box to box pool
            boxPool.Add(newBox);          
        }
    }
    public Vector3 GetRandomSpawnPos()
    {
        var areaBounds = boxArea.GetComponent<Collider>().bounds;
        var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }
 

    }
}
