using UnityEngine;
using static PickupScript2;

public class SensorDetectBox : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// </summary>
    [HideInInspector]
    public PackerHand agent;  //

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("agent"))
        {
            PickupScript2 pickupScript = this.GetComponent<PickupScript2>();
            // Agent checks if object is target box for pickup
            if (agent.CheckTarget()) {
                // Agent picks up the box
                agent.PickupBox();
                // Agent is rewarded
                agent.RewardPickedupTarget();
            }
        }
    }
}