using UnityEngine;

public class SensorBox : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// Don't need to manually set.
    /// </summary>
    [HideInInspector]
    public GameObject bin;  

    public GameObject minibin;

    public PackerHand agent;

    void OnCollisionEnter(Collision col)
    {
        // Touched goal.
        if (col.gameObject.CompareTag("bin") || col.gameObject.CompareTag("minibin")) 
        {
            // Get the array of contact points from the collision
            ContactPoint[] contacts = col.contacts;
            float surfaceArea = 0;
            // Loop through each contact point
            foreach (ContactPoint contact in contacts) {
                // Calculate the projection of the surface area onto the normal vector
                float projection = -Vector3.Dot(contact.normal, contact.point);

                // Multiply the projection by the length of the normal vector to get the surface area
                surfaceArea = surfaceArea +  projection * contact.normal.magnitude;
                }
            agent.RewardDroppedBox(surfaceArea);
        }

    }
}