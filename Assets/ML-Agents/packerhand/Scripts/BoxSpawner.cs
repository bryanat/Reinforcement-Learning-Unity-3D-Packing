using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

namespace Boxes {



public class Box
{
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public static List<int> organizedBoxes = new List<int>(); // list of organzed box indices

    public static void ResetBoxes(Box box)
    {
        box.rb.transform.position = box.startingPos; // Reset box position
    }

    public static List<Box> GetBoxPool() {
        return BoxSpawner.boxPool;
    }
}



public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public static List<Box> boxPool = new List<Box>();

    // The box area, which will be set manually in the Inspector
    public GameObject boxArea;

    public GameObject binArea;
    
    public GameObject binMini;

    public GameObject unitBox; 


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
            // Create sizes_American_pallets = new float[][] { ... }  48" X 40" = 12.19dm X 10.16dm 
            // Create sizes_EuropeanAsian_pallets = new float[][] { ... }  47.25" X 39.37" = 12dm X 10dm
            // Create sizes_AmericanEuropeanAsian_pallets = new float[][] { ... }  42" X 42" = 10.67dm X 10.67dm
            sizes = new float[][] 
            {
                // new float[] { 1.0f, 1.0f, 1.0f },
                // new float[] { 1.0f, 1.0f, 1.0f },
                // new float[] { 1.0f, 1.0f, 1.0f },
                // new float[] { 1.0f, 1.0f, 1.0f },
                // new float[] { 1.0f, 1.0f, 1.0f },
                new float[] { 1.0f, 1.0f, 1.0f },
                new float[] { 3.0f, 3.0f, 3.0f },
                new float[] { 6.0f, 3.0f, 3.0f },
                new float[] { 3.0f, 6.0f, 3.0f },
                new float[] { 3.0f, 3.0f, 6.0f },
                new float[] { 6.0f, 6.0f, 3.0f },
                new float[] { 3.0f, 6.0f, 6.0f },
                new float[] { 6.0f, 3.0f, 6.0f },
                new float[] { 6.0f, 6.0f, 6.0f },
                new float[] { 6.0f, 3.0f, 9.0f },
                new float[] { 6.0f, 9.0f, 3.0f },
                new float[] { 9.0f, 3.0f, 3.0f },
                new float[] { 9.0f, 6.0f, 6.0f },
            };
         }
        var idx = 0;
        foreach(var s in sizes) 
        {
            // Create GameObject box
            var position = GetRandomSpawnPos();
            GameObject box = Instantiate(unitBox, position, Quaternion.identity);
            box.transform.localScale = new Vector3(s[0], s[1], s[2]);
            box.transform.position = position;
            // Add compoments to GameObject box
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            box.name = idx.ToString();
            var m_rb = box.GetComponent<Rigidbody>();
            Collider [] m_cList = box.GetComponentsInChildren<Collider>();
            foreach (Collider m_c in m_cList) 
            {
                m_c.isTrigger = true;
            }
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
            idx+=1;     
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