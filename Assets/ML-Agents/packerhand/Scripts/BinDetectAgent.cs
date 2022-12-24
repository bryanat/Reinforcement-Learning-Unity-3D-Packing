using UnityEngine;

public class BinDetectAgent : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// </summary>
    [HideInInspector]
    public PackerHand agent;  //

    void OnCollisionEnter(Collision col)
    {
        // Touched goal.
        if (col.gameObject.CompareTag("agent"))
        {
            agent.GotToBin();
        }
    }
}