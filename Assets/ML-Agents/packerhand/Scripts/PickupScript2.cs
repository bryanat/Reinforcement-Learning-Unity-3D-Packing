using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupScript2 : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// </summary>
    [HideInInspector]
    public PackerHand agent;
    public bool isHeld = false; // Determines if agent is holding box
    public bool isOrganized  = false; //Determines if box is inside bin
    

}