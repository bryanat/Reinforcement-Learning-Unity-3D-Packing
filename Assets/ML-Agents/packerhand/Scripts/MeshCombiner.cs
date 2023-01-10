using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    // The list of meshes to combine
    public List<GameObject> meshList;

    void Start()
    {

        Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%   START MESH COMBINING   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
        Debug.Log($" GameObject name ==== {gameObject.name}");

        // Create a list of CombineInstance structures
        List<CombineInstance> combine = new List<CombineInstance>();

        // MeshFilter[] meshList = GetComponentsInChildren<MeshFilter>(); 

        for (int i = 0; i < meshList.Count; i++)
        {
            // Get the mesh and its transform component
            Mesh mesh = meshList[i].GetComponent<MeshFilter>().mesh;
            Transform transform = meshList[i].transform;

            // Create a new CombineInstance and set its properties
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            ci.transform = transform.localToWorldMatrix;

            // Add the CombineInstance to the list
            combine.Add(ci);
        }

        // Create a new mesh on the GameObject
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();

        // // Set the materials of the new mesh to the materials of the original meshes
        // Material[] materials = new Material[meshList.Count];
        // for (int i = 0; i < meshList.Count; i++)
        // {
        //     materials[i] = meshList[i].GetComponent<Renderer>().sharedMaterial;
        // }
        // mr.materials = materials;

        // Combine the meshes
        mf.mesh.CombineMeshes(combine.ToArray(), true, true);

    Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%   MESH IS COMBINED   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");

    }
}