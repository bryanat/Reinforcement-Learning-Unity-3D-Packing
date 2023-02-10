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
                foreach (Vector3 vertex in agent.verticesArray) 
                {
                    Vector3 scaledVertex =  new Vector3(((vertex.x* agent.binscale_x) + agent.origin.x), ((vertex.y* agent.binscale_y) + agent.origin.y), ((vertex.z* agent.binscale_z) + agent.origin.z));
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(scaledVertex, 0.2f);
                }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(agent.selectedVertex, 0.2f);
            }
        }
    }
}