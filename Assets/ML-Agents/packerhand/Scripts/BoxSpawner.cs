using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using static SensorBox;

namespace Boxes {



public class Box
{
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 


    public void ResetBoxes(Box box)
    {
        box.rb.transform.position = box.startingPos; // Reset box position
    }

}



public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public List<Box> boxPool = new List<Box>();

    /// <summary>
    /// The box area, which will be set manually in the Inspector
    /// </summary>
    public GameObject boxArea;

    public GameObject binArea;
    
    public GameObject binMini;

    //public SensorBox sensorBox;

    public PackerHand hand;


    public void SetUpBoxes(int flag, float size) 
    {
        float[][] sizes = new float[][]{};
        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 
        if (flag ==0) 
        {
            // Gets bounds of mini bin
            var miniBounds = binMini.transform.GetChild(0).GetComponent<Collider>().bounds;

            // Encapsulate the bounds of each additional object in the overall bounds
            for (int i = 1; i < 5; i++)
            {
                miniBounds.Encapsulate(binMini.transform.GetChild(i).GetComponent<Collider>().bounds);
            }
            var n_boxes = (int)Math.Floor(miniBounds.extents.x*2/size) *
                (int)Math.Floor(miniBounds.extents.y*2/size)*(int)Math.Floor(miniBounds.extents.z*2/size);
            sizes = Enumerable.Repeat(Enumerable.Repeat(size, 3).ToArray(), n_boxes).ToArray();
         }
        else {
        //temporary box sizes array (to be fed from json later)
            sizes = new float[][] 
            {
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
        foreach(var s in sizes) 
        {
            // Create GameObject box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(s[0], s[1], s[2]); 
            var position = GetRandomSpawnPos();
            box.transform.position = position;
            // Add compoments to GameObject box
            // automatically comes with boxCollider and mesh renderer
            box.AddComponent<Rigidbody>();
            box.tag = "0";
            box.layer = 5;
            var m_rb = box.GetComponent<Rigidbody>();
            // not be affected by forces or collisions, position and rotation will be controlled directly through script
            m_rb.isKinematic = true;
            // Transfer GameObject box properties to Box object 
            var newBox = new Box
            {
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