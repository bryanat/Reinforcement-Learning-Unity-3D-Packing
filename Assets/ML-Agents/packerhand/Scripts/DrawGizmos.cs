using UnityEngine;
using System.Collections;

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
                    Vector3 scaledVertex =  new Vector3(((vertex.x* agent.binscales_x[bin]) + agent.origins[bin].x), ((vertex.y* agent.binscales_y[bin]) + agent.origins[bin].y), ((vertex.z* agent.binscales_z[bin]) + agent.origins[bin].z));
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(scaledVertex, 0.2f);
                }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(agent.selectedVertex, 0.5f);
            }
        }
    }
}