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
<<<<<<< HEAD:Assets/ML-Agents/packerhand/Scripts/SensorDetectBin.cs
            agent.RewardDroppedBox();
=======
            // agent.DroppedBox();
>>>>>>> heuristicInput:Assets/ML-Agents/packerhand/Scripts/BinDetect2.cs
        }

    }
}