using UnityEngine;
using System.Collections;

public class ExampleClass : MonoBehaviour
{

    public PackerHand agent;

    public SensorCollision sensorCollision;

    // void OnDrawGizmos() {
    //     var mesh = GetComponent<MeshFilter>().sharedMesh;
    //     Vector3[] vertices = mesh.vertices;
    //     int[] triangles = mesh.triangles;

    //     Matrix4x4 localToWorld = transform.localToWorldMatrix;
 
    //     for(int i = 0; i<mesh.vertices.Length; ++i){
    //         Vector3 world_v = localToWorld.MultiplyPoint3x4(mesh.vertices[i]);
    //         Debug.Log($"Vertex position is {world_v}");
    //         // if (name == "BinIso20Bottom") {
    //         //     Gizmos.color = Color.green;
    //         //     Gizmos.DrawSphere(world_v, 0.1f);
    //         // }
    //         if (name == "BinIso20Back") {
    //             Gizmos.color = Color.blue;
    //             Gizmos.DrawSphere(world_v, 0.1f);
    //         }
    //         // if (name == "BinIso20Side") {
    //         //     Gizmos.color = Color.red;
    //         //     Gizmos.DrawSphere(world_v, 0.1f);
    //         // }
    //     }
    // }

    // Draws vertices array
    void OnDrawGizmos() { 
        if (agent.verticesArray!=null)
        {
            foreach (Vector3 vertex in agent.verticesArray) {
                Vector3 scaledVertex =  new Vector3(((vertex.x* agent.binscale_x) + agent.origin.x), ((vertex.y* agent.binscale_y) + agent.origin.y), ((vertex.z* agent.binscale_z) + agent.origin.z));
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(vertex, 0.2f);
            }
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(agent.selectedVertex, 0.2f);
        }

        if (sensorCollision.transform!=null)
        {
            Gizmos.color = Color.magenta;
            Vector3 position =   new Vector3(sensorCollision.transform.position.x, sensorCollision.transform.position.y-sensorCollision.transform.localScale.y*0.5f, sensorCollision.transform.position.z);
            Vector3 direction = sensorCollision.transform.TransformDirection(Vector3.down) * 30f;
            Gizmos.DrawRay(position, direction);
        }
    }

}