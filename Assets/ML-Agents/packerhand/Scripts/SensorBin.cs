using UnityEngine;

public class SensorBin : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// Don't need to manually set.
    /// </summary>
    [HideInInspector]

    public PackerHand agent;

    /// Drop off (agent) -> collision (box) -> mesh combine (?) and contact SA calculation (box) 
    /// Mesh combine (?) -> bin volume and bin bounds recalculation (agenet)
    /// contact SA calculation (box) -> reward (agent)

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("0")) 
        {
            /////MESH COMBINE HERE?/////

        // Update bin volume
        agent.UpdateBinVolume();
        // Update bin bounds
        //agent.UpdateBinBounds();
        }

    }
}