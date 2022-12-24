using UnityEngine;

public class SensorDetectBin : MonoBehaviour
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
        if (col.gameObject.CompareTag("box"))
        {
            agent.RewardDroppedBox();
        }

        else if (col.gameObject.CompareTag("agent"))
        {

            agent.RewardGotToBin();
        }
    }
}