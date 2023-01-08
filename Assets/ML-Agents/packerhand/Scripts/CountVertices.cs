using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexCounter : MonoBehaviour
{
    int vertexCount;
    // Start is called before the first frame update
    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        int vertexCount = mesh.vertices.Length;

        Debug.Log($"********************************************** Original  {gameObject.name}: Vertex count=== " + vertexCount);
    }
    void Update(){
        Debug.Log($"*************************************** Updated {gameObject.name}: Vertex count=== " + vertexCount );
    }
}
