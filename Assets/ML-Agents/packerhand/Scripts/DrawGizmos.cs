using UnityEngine;
using System.Collections;
using BinSpawner=Bins.BinSpawner;


// Draws vertices as yellow dots in Unity scene view
public class ExampleClass : MonoBehaviour
{
    public PackerHand agent;

    public BinSpawner binSpawner;

    public bool on;


    void OnDrawGizmos() 
    { 
        if (on)
        {
            if (agent.verticesArray!=null && binSpawner.binscales_x.Count>0)
            {
                // foreach (Vector4 vertex in agent.verticesArray) 
                // {
                for (int i = 0; i<agent.verticesArray.Length;i++)
                {
                    Vector4 vertex = agent.verticesArray[i];
                    if (!agent.maskedVertexIndices.Contains(i)) 
                    {
                        int bin = Mathf.RoundToInt(vertex.w);
                        Vector3 scaledVertex =  new Vector3(((vertex.x* binSpawner.binscales_x[bin]) + binSpawner.origins[bin].x), ((vertex.y* binSpawner.binscales_y[bin]) + binSpawner.origins[bin].y), ((vertex.z* binSpawner.binscales_z[bin]) + binSpawner.origins[bin].z));
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(scaledVertex, 0.2f);
                    }
                }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(agent.selectedVertex, 0.5f);
            }
        }
    }
}