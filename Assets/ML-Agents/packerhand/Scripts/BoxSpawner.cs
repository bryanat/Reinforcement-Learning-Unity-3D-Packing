using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.Serialization.Json;



namespace Boxes {



public class Box
{
    public Rigidbody rb;

    public Vector3 startingPos;

    public Quaternion startingRot;

    public Vector3 boxSize; 

    public GameObject gameobjectBox;

}

public class Blackbox
{
    public Vector3 position;
    public Vector3 vertex;
    public float volume;
    public Vector3 size;

    public GameObject gameobjectBlackbox;


}

[System.Serializable]
public class BoxSize
{
    public Vector3 box_size;
}

public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public static List<Box> boxPool = new List<Box>();


    // The box area, which will be set manually in the Inspector
    public GameObject boxArea;
    
    public GameObject unitBox; 

    public BoxSize [] sizes;

    [HideInInspector] public int idx_counter = 0;

    public void SetUpBoxes(float size) 
    {
    // read from file if boxes has not been imported from file
        if (sizes[0].box_size[0]==0) {
            ReadJson("Assets/ML-Agents/packerhand/Scripts/Boxes.json");
            // ReadJson("Assets/ML-Agents/packerhand/Scripts/Boxes_412.json");
        }
        var idx = 0;
        foreach(BoxSize s in sizes) 
        {
            // Create GameObject box
            var position = GetRandomSpawnPos();
            GameObject box = Instantiate(unitBox, position, Quaternion.identity);
            box.transform.localScale = new Vector3(s.box_size.x, s.box_size.y, s.box_size.z);
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
                rb = box.GetComponent<Rigidbody>(), 
                startingPos = box.transform.position,
                startingRot = box.transform.rotation,
                boxSize = box.transform.localScale,
                gameobjectBox = box,
            };
            // Add box to box pool
            boxPool.Add(newBox);  
            idx+=1;     
        }
        // // Create sizes_American_pallets = new float[][] { ... }  48" X 40" = 12.19dm X 10.16dm 
        // // Create sizes_EuropeanAsian_pallets = new float[][] { ... }  47.25" X 39.37" = 12dm X 10dm
        // // Create sizes_AmericanEuropeanAsian_pallets = new float[][] { ... }  42" X 42" = 10.67dm X 10.67dm
    }



    public Vector3 GetRandomSpawnPos()
    {
        var areaBounds = boxArea.GetComponent<Collider>().bounds;
        var randomPosX = UnityEngine.Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = UnityEngine.Random.Range(areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }


    public void ReadJson(string filename) 
    {
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var boxes = root.XPathSelectElement("//Items").Elements();
            foreach (XElement box in boxes)
            {
                string id = box.XPathSelectElement("./Product_id").Value;
                float length = float.Parse(box.XPathSelectElement("./Length").Value);
                float width = float.Parse(box.XPathSelectElement("./Width").Value);
                float height = float.Parse(box.XPathSelectElement("./Height").Value);
                int quantity = int.Parse(box.XPathSelectElement("./Quantity").Value);
                 Debug.Log($"JSON CONTAINER LENGTH: {length}");
                for (int n = 0; n<quantity; n++)
                {
                    sizes[idx_counter].box_size = new Vector3(width, height, length);
                    idx_counter++;
                }
                
            }

        }
        
    }
}

}


