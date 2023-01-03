using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System;
using System.Linq;


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

    /// <summary>
    /// The box area.
    /// This will be set manually in the Inspector
    /// </summary>
    public GameObject boxArea;


    public void SetUpBoxes(int flag, float size) {

        float[][] sizes = new float[][]{};
        var n_boxes = (int)Math.Floor(8/size)*(int)Math.Floor(8.5/size)*(int)Math.Floor(19.88/size);

        sizes = Enumerable.Repeat(Enumerable.Repeat(size, 3).ToArray(), n_boxes).ToArray();
;
        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 
        if (flag ==0) {
            sizes = Enumerable.Repeat(Enumerable.Repeat(size, 3).ToArray(), n_boxes).ToArray();
        //     sizes = new float[][] {
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size },
        //     new float[] { size, size, size }};
         }
        else {
        //temporary box sizes array (to be fed from json later)
            sizes = new float[][] {
            new float[] { 1.0f, 2.0f, 3.0f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.0f, 2.0f, 3.5f },
            new float[] { 2.0f, 2.0f, 2.0f },
            new float[] { 1.0f, 1.0f, 2.0f },
            new float[] { 3.0f, 4.0f, 4.0f },
            new float[] { 1.0f, 2.0f, 3.5f },
            new float[] { 1.0f, 1.5f, 0.5f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.5f, 0.5f, 0.5f },
            new float[] { 2.0f, 3.0f, 4.0f },
            new float[] { 0.5f, 0.5f, 0.5f },
            new float[] { 1.0f, 2.0f, 3.5f },
            new float[] { 1.0f, 1.5f, 0.5f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.0f, 2.0f, 2.0f },
            new float[] { 1.0f, 1.0f, 2.0f },
            new float[] { 3.0f, 4.0f, 4.0f },
            new float[] { 1.0f, 2.0f, 3.5f },
            new float[] { 1.0f, 1.5f, 0.5f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.5f, 0.5f, 0.5f },
            new float[] { 1.0f, 2.0f, 3.0f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 2.0f, 2.0f, 3.5f },
            };
        }
        foreach(var s in sizes) {
            // Create GameObject box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(s[0], s[1], s[2]); 
            var position = GetRandomSpawnPos();
            box.transform.position = position;
            // Add compoments to GameObject box
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            box.tag = "0";
            var m_rb = box.GetComponent<Rigidbody>();
            // not be affected by forces or collisions, position and rotation will be controlled directly through script
            //m_rb.isKinematic = true;
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
        var randomPosX = UnityEngine.Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = UnityEngine.Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }
 

    }
}
