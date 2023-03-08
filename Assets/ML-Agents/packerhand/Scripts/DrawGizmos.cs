using UnityEngine;
using System.Collections;
using BinSpawner=Bins.BinSpawner;


// Draws vertices as yellow dots in Unity scene view
public class ExampleClass : MonoBehaviour
{
    public PackerHand agent;

    public bool on;


    void OnDrawGizmos() 
    { 
        if (on)
        {
            if (agent.verticesArray!=null)
            {
                foreach (Vector4 vertex in agent.verticesArray) 
                {
                    int bin = Mathf.RoundToInt(vertex.w);
                    Vector3 scaledVertex =  new Vector3(((vertex.x* BinSpawner.binscales_x[bin]) + BinSpawner.origins[bin].x), ((vertex.y* BinSpawner.binscales_y[bin]) + BinSpawner.origins[bin].y), ((vertex.z* BinSpawner.binscales_z[bin]) + BinSpawner.origins[bin].z));
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(scaledVertex, 0.2f);
                }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(agent.selectedVertex, 0.5f);
            }
        }
    }
}