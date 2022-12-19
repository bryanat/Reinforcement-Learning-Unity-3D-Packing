using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public class Box : Monobehavior
{



    
 void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();
        block1.transform.position = GetRandomSpawnPos();

        // Reset box velocity and angularVelocity back to zero.
        foreach (var box in BoxList) {
            box.velocity = Vector3.zero;
            box.angularVelocity = Vector3.zero;
        }

        // Reset block angularVelocity back to zero.
        // m_BlockRb.angularVelocity = Vector3.zero;
        // m_Block1Rb.angularVelocity = Vector3.zero;
    }
}
