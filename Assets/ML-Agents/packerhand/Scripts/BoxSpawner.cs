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
using Newtonsoft.Json;
using BinSpawner = Bins.BinSpawner;
using Container = Bins.Container;


namespace Boxes {

public class Box
{
    public Color boxColor;  // stores box color, boxes of the same product_id will have same color (for front_end)
    public Rigidbody rb; // stores transform information
    public Vector3 boxSize; // for sensor, changes after selected action
    public Quaternion boxRot = Quaternion.identity; // for sensor, changes after selected action
    public Vector3 boxVertex = Vector3.zero; // for sensor, changes after selected action
    public Vector3 boxBinScale = Vector3.zero; //for sensor, changes after selected actiong
    public bool isOrganized = false; // for sensor, changes after selected action
    public GameObject gameobjectBox; // stores gameobject box reference created during box creation, for destroying old boxes
    public Vector3 boxRotEuler;
    public string boxId;
}

[System.Serializable]
public class BoxSize
{
    public Vector3 box_size;
}

public class Item
{
    public int Product_id { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public int Quantity { get; set; }
    public string Color {get; set; }
}


public class BoxSpawner : MonoBehaviour 
{
    [HideInInspector] public List<Box> boxPool = new List<Box>(); //list of Box class objects that stores most of the box information
    public List<Color> Colors = new List<Color>(); // stores local box colors

    public List<string> Ids = new List<string>();

    // The box area, which will be set manually in the Inspector
    public GameObject boxArea; // place where boxes are spawned
    public GameObject unitBox; // prefab for box generation
    
    public int maxBoxQuantity; // maximum box quantity (default set to 50)

    public float total_box_surface_area;

    public float total_box_volume;

    public BoxSize [] sizes; // array of box sizes

    [HideInInspector] public int idx_counter = 0; //counter for sizes array

    public BinSpawner binSpawner;

    string homeDir;


    public void Start()
    {
        homeDir = Environment.GetEnvironmentVariable("HOME"); // AWS: /home/ubuntu/
    }

    public void Reset()
    {
        boxPool.Clear();
        Colors.Clear();
        total_box_surface_area = 0;
        total_box_volume = 0;
        idx_counter = 0;

    }
    public void SetUpBoxes(string name , int seed=123) 
    {
        Reset();

        // randomly generates boxes
        if (name == "uniform" | name == "mix")
        {
            RandomBoxGenerator(name, seed);
            // Read random boxes using existing ReadJson function
            ReadJson($"{homeDir}/Unity/data/Boxes_Random.json");
            PadZeros();
            // Delete the created json file to reuse the name next iteration
            File.Delete($"{homeDir}/Unity/data/Boxes_Random.json");

        }
        // read box from file
        else
        {
            // once sizes is populated, don't have to read from file again
            ReadJson(name);
            PadZeros();
        }
        // populate box pool
        var idx = 0;
        foreach(BoxSize s in sizes) 
        {
            Vector3 box_size = new Vector3(s.box_size.x, s.box_size.y, s.box_size.z);
            // if box is not of size zeros
            if (box_size.x != 0) 
            {
                // Create GameObject box
                var position = GetRandomSpawnPos();
                GameObject box = Instantiate(unitBox, position, Quaternion.identity);
                box.transform.localScale = box_size;
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
                    boxColor = Colors[idx],
                    boxSize = box.transform.localScale,
                    gameobjectBox = box,
                    boxId = Ids[idx]
                };
                // Add box to box pool
                boxPool.Add(newBox);  
                // update total box surface rea
                total_box_surface_area+=2*box_size.x*box_size.y + 2*box_size.y*box_size.z + 2* box_size.z*box_size.x;
                total_box_volume += box_size.x*box_size.y*box_size.z;
                idx+=1;     
            }
        }
        // sort box pool
        boxPool.OrderBy(n=>n.boxSize.x).ThenBy(n=>n.boxSize.y).ThenBy(n=>n.boxSize.z);
    }


    public Vector3 GetRandomSpawnPos()
    {
        var areaBounds = boxArea.GetComponent<Collider>().bounds;
        var randomPosX = UnityEngine.Random.Range(-areaBounds.extents.x, areaBounds.extents.x);
        var randomPosZ = UnityEngine.Random.Range(areaBounds.extents.z, areaBounds.extents.z);
        var randomSpawnPos = boxArea.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
        return randomSpawnPos;
    }

    public void RandomBoxGenerator(string box_type, int seed)
    {
        // Create a new object with the Items list
        List<Item> items = new List<Item>();
        UnityEngine.Random.InitState(seed);
        if (box_type == "uniform") 
        {
            foreach (Container container in binSpawner.Containers)
            {
                Color randomColor = UnityEngine.Random.ColorHSV();
                int random_num_x =  UnityEngine.Random.Range(2, 4);
                int random_num_y =  UnityEngine.Random.Range(2, 4);
                int random_num_z =  UnityEngine.Random.Range(2, 4);
                float x_dimension =  (float)Math.Floor(container.Width/random_num_x * 100)/100;
                float y_dimension =  (float)Math.Floor(container.Height/random_num_y * 100)/100;
                float z_dimension = (float)Math.Floor(container.Length/random_num_z * 100)/100;
                int quantity = random_num_x*random_num_y*random_num_z;
                //Debug.Log($"RUF RANDOM UNIFORM BOX NUM: {random_num_x*random_num_y*random_num_z} | x:{x_dimension} y:{y_dimension} z:{z_dimension}");
                items.Add(new Item
                {
                    Product_id = 0,
                    Length = z_dimension,
                    Width = x_dimension,
                    Height = y_dimension,
                    Quantity = random_num_x*random_num_y*random_num_z,
                    Color = ColorUtility.ToHtmlStringRGB(randomColor)
                });
                // Set color of the boxes
                for (int i= 0; i<quantity; i++)
                {
                    Colors.Add(randomColor);
                }
            }
        var data = new { Items = items };
        // Serialize the object to json
        var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
        // Write the json to a file
        File.WriteAllText($"{homeDir}/Unity/data/Boxes_Random.json", json);
        }
        else if (box_type == "mix")
        {
            foreach (Container container in binSpawner.Containers)
            {
                int bin_z = (int) Math.Floor(container.Length);
                int bin_x = (int) Math.Floor(container.Width);
                int bin_y = (int) Math.Floor(container.Height);
                List<int> x_dimensions = new List<int>();
                List<int> y_dimensions = new List<int>();
                List<int> z_dimensions = new List<int>();
                // chop up the x, y, z dimensions
                int random_num_x =  UnityEngine.Random.Range(2, 4);
                int random_num_y =  UnityEngine.Random.Range(2, 4);
                int random_num_z =  UnityEngine.Random.Range(2, 4);
                x_dimensions.Add(bin_x);
                while (x_dimensions.Count<random_num_x)
                {
                    int largest = x_dimensions.Max();
                    int newPiece = UnityEngine.Random.Range(1, largest);
                    x_dimensions.Remove(largest);
                    x_dimensions.Add(newPiece);
                    x_dimensions.Add(largest - newPiece);
                }
                y_dimensions.Add(bin_y);
                while (y_dimensions.Count<random_num_y)
                {
                    int largest = y_dimensions.Max();
                    int newPiece = UnityEngine.Random.Range(1, largest);
                    y_dimensions.Remove(largest);
                    y_dimensions.Add(newPiece);
                    y_dimensions.Add(largest - newPiece);
                }
                z_dimensions.Add(bin_z);
                while (z_dimensions.Count<random_num_z)
                {
                    int largest = z_dimensions.Max();
                    int newPiece = UnityEngine.Random.Range(1, largest);
                    z_dimensions.Remove(largest);
                    z_dimensions.Add(newPiece);
                    z_dimensions.Add(largest - newPiece);
                }
                int id = 0;
                for (int x=0; x<x_dimensions.Count; x++){
                    for (int y=0; y<y_dimensions.Count; y++){
                        for (int z=0; z<z_dimensions.Count; z++){
                            Color randomColor = UnityEngine.Random.ColorHSV();
                            Colors.Add(randomColor);
                            items.Add(new Item{
                                Product_id = id,
                                Length = z_dimensions[z],
                                Width = x_dimensions[x],
                                Height = y_dimensions[y],
                                Quantity = 1,
                                Color = ColorUtility.ToHtmlStringRGB(randomColor)

                            }); id++;
                        }
                    }
                }

            }
            var data = new { Items = items };
            // Serialize the object to json
            var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            // Write the json to a file
            File.WriteAllText($"{homeDir}/Unity/data/Boxes_Random.json", json);           
        }
    }

    // Read from json file and construct box, then add box to sizes array of boxes
    // Schema of .json: { "Product_id": string, "Length": float, "Width": float, "Height": float, "Quantity": int },
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
                //Color randomColor = UnityEngine.Random.ColorHSV();
                string htmlValue = box.XPathSelectElement("./Color").Value;
                Color newCol;
                //Debug.Log($"JSON BOX LENGTH {length} WIDTH {width} HEIGHT {height} QUANTITY {quantity}");
                for (int n = 0; n<quantity; n++)
                {
                    sizes[idx_counter].box_size = new Vector3(width, height, length);
                    // Set color of boxes (same id (same size) with same color)
                    if (ColorUtility.TryParseHtmlString(htmlValue, out newCol))
                    {
                        Colors.Add(newCol);
                    }   
                    Ids.Add(id);            
                    // Colors.Add(randomColor);
                    idx_counter++;
                }   
            }
        }
    }

    public void PadZeros()
    {
        for (int m=idx_counter; m<maxBoxQuantity; m++)
        {
            // pad with zeros
            sizes[m].box_size = Vector3.zero;
        }
    }


    public void ExportBoxInstruction(float percent_filled)
    {
        var boxPool_copy = boxPool;
        using(StreamWriter writetext = new StreamWriter(AppHelper.instructions_file_path))
        {
            writetext.WriteLine($"{percent_filled}" + Environment.NewLine);
            foreach (Box box in boxPool_copy)
            {
                if (box.isOrganized)
                {
                    string box_info = $"Product_id: {box.boxId}, rotation: ({box.boxRotEuler.x}, {box.boxRotEuler.y}, {box.boxRotEuler.z}), position: ({box.boxVertex.x}, {box.boxVertex.y}, {box.boxVertex.z})";
                    writetext.WriteLine(box_info);
                }
            }
        }
    }
}


}
