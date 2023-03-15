using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using Autodesk.Fbx;


namespace Bins {
public class Container
{
    public float Length {get; set;}
    public float Width {get; set;}
    public float Height {get; set;}
}



public class BinSpawner : MonoBehaviour {


    public  List<Container> Containers = new List<Container>(); // contains dimension information of all bins
    public GameObject bin; // The bin container prefab, which will be manually selected in the Inspector
    public GameObject outerBin; // The outer shell of container prefab, which will be manually selected in the Inspector
    public GameObject Origin; // gives origin position of the first bin (for multiplatform usage)   
    [HideInInspector] public List<Vector4> origins = new List<Vector4>(); // stores origin information of all bins

    public List<float> binscales_x = new List<float>(); // stores all bin's x dimension
    public List<float> binscales_y = new List<float>(); // stores all bin's y dimension
    public List<float> binscales_z = new List<float>(); // stores all bin's z dimension
    [HideInInspector] public List<CombineMesh> m_BackMeshScripts = new List<CombineMesh>(); // stores back mesh script of all bins
    [HideInInspector] public List<CombineMesh> m_SideMeshScripts = new List<CombineMesh>(); // stores side mesh script of all bins
    [HideInInspector] public List<CombineMesh> m_BottomMeshScripts = new List<CombineMesh>(); // stores bottom mesh script of all bins

    // prefab's (BinIso20) sizes
    public float biniso_z = 59f;
    public float biniso_x = 23.5f;
    public float biniso_y = 23.9f;
    public int total_bin_num;
    public float total_bin_volume;
    public float total_bin_surface_area;
    

    // // Create sizes_American_pallets = new float[][] { ... }  48" X 40" = 12.19dm X 10.16dm 
    // // Create sizes_EuropeanAsian_pallets = new float[][] { ... }  47.25" X 39.37" = 12dm X 10dm
    // // Create sizes_AmericanEuropeanAsian_pallets = new float[][] { ... }  42" X 42" = 10.67dm X 10.67dm
    public void SetUpBins(string bin_type, int bin_quantity=0, int seed=123)
    {
        if (bin_type == "biniso20" | bin_type == "random")
        {
            // generate bin
            RandomBinGenerator(bin_type, bin_quantity, seed);
        }
        else
        {
            // read bin from file
            ReadJson(bin_type);            
        }
        Vector3 localOrigin = Origin.transform.position;
        int idx = 0;
        //Debug.Log($"CONTAINER COUNT: {Containers.Count}");
        foreach (Container c in Containers)
        {
            // make container and outer_shell from prefab
            GameObject container = Instantiate(bin);
            GameObject shell = Instantiate(outerBin);
            container.name = $"Bin{idx.ToString()}";
            shell.name = $"OuterBin{idx.ToString()}";
            float binscale_x = c.Width;
            float binscale_y  = c.Height;
            float binscale_z  = c.Length;
            binscales_x.Add(binscale_x);
            binscales_y.Add(binscale_y);
            binscales_z.Add(binscale_z);
            // Set bin and outer bin's scale and position
            container.transform.localScale = new Vector3((binscale_x/biniso_x), (binscale_y/biniso_y), (binscale_z/biniso_z));
            //Debug.Log($"CONTAINER LOCALSCALE IS: {container.transform.localScale}");
            shell.transform.localScale = new Vector3(binscale_x/biniso_x, binscale_y/biniso_y, binscale_z/biniso_z);
            // Set origin position of each bin
            localOrigin.x = localOrigin.x+binscale_x+5f;
            Vector4 originInfo = new Vector4(localOrigin.x, localOrigin.y, localOrigin.z, idx);
            //Debug.Log($"ORIGIN INFO FOR BIN {idx}: {originInfo}");
            origins.Add(originInfo);
            Vector3 container_center = new Vector3(localOrigin.x+(binscale_x/2f), 0.5f, localOrigin.z+(binscale_z/2f));
            container.transform.localPosition = container_center;
            shell.transform.localPosition = container_center;
            // Add scripts 
            CombineMesh binBottomScript = container.transform.GetChild(0).GetComponent<CombineMesh>();
            CombineMesh binBackScript = container.transform.GetChild(1).GetComponent<CombineMesh>();
            CombineMesh binSideScript = container.transform.GetChild(2).GetComponent<CombineMesh>();
            m_BottomMeshScripts.Add(binBottomScript);
            m_SideMeshScripts.Add(binSideScript);
            m_BackMeshScripts.Add(binBackScript);
            // update total volume
            total_bin_volume += binscale_x * binscale_y * binscale_z;
            total_bin_surface_area += 2*binscale_x*binscale_y + 2*binscale_x*binscale_z + 2*binscale_y*binscale_z;
            idx++;
        }
        // set total bin volume
        total_bin_num = idx;
        // hide original prefabs
        bin.SetActive(false);
        outerBin.SetActive(false);

    }
    public void RandomBinGenerator(string bin_type, int quantity, int seed)
    {

        UnityEngine.Random.InitState(seed);
        if (bin_type == "random")
        {
            for (int i = 0; i<quantity;i++)
            {
                float length = (float) Math.Round(UnityEngine.Random.Range(10.0f, 60.0f));
                float width = (float) Math.Round(UnityEngine.Random.Range(10.0f, 30.0f));
                float height = (float) Math.Round(UnityEngine.Random.Range(10.0f, 30.0f));
                Containers.Add(new Container
                {
                    Length = length,
                    Width = width,
                    Height = height,
                });   
            }      
        }
        else if (bin_type == "biniso20")
        {
            for (int i = 0; i<quantity;i++)
            {
                Containers.Add(new Container
                {
                    Length = biniso_z,
                    Width = biniso_x,
                    Height = biniso_y,
                });   
            }      
        }
    }



    public void ReadJson(string box_file) 
    {
        var homeDir = Environment.GetEnvironmentVariable("HOME");
        string filename = $"{homeDir}/Unity/data/{box_file}.json";
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var containers = root.XPathSelectElement("//Container").Elements();
            foreach (XElement container in containers)
            {
                float length = float.Parse(container.XPathSelectElement("./Length").Value)/10f;
                float width = float.Parse(container.XPathSelectElement("./Width").Value)/10f;
                float height = float.Parse(container.XPathSelectElement("./Height").Value)/10f;   
                //Debug.Log($"JSON CONTAINER LENGTH {Container.Length} WIDTH {Container.Width} HEIGHT {Container.Height}");
                Containers.Add(new Container
                    {
                        Length = length,
                        Width = width,
                        Height = height,
                    });
            }
        }
    }

    public void ExportBins()
    {
        // set the path and name for the exported file
        string filePath = Path.Combine(Application.dataPath, "Bins.fbx");
        UnityEngine.Object[] objects = new UnityEngine.Object[total_bin_num];
        for (int n=0; n<total_bin_num;n++)
        {
            objects[n] = GameObject.Find($"Bin{n}");
        }
        var x = ModelExporter.ExportObjects(filePath, objects);


    }

}
}