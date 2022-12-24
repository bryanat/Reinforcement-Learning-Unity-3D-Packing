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
        if (col.gameObject.CompareTag("box"))
        {
            agent.RewardDroppedBox();
        }

    }
}