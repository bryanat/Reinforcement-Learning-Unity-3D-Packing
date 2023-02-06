using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexCounter : MonoBehaviour
{
    int vertexCount;

    Mesh mesh;

    MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    void Update(){
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        vertexCount = mesh.vertices.Length;
        Debug.Log($"********************************************** Original  {gameObject.name}: Vertex count=== " + vertexCount);
    }
}
