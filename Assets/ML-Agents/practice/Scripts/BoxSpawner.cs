using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public class BoxSpawner : Agent
{

    public GameObject block; 
    // Start is called before the first frame update
    Rigidbody m_BlockRb;  //cached on initialization
   
    public override void Initialize()
    {
         // Cache the block rigidbody
        m_BlockRb = block.GetComponent<Rigidbody>();
    }

    void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = Vector3.zero;//GetRandomSpawnPos();

        // Reset block velocity back to zero.
        m_BlockRb.velocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        m_BlockRb.angularVelocity = Vector3.zero;
    }

    public override void OnEpisodeBegin()
    {
        ResetBlock();
        //setBlockProperties()
    }

    // public void SetBlockProperties()
    // {
    //     var scale = m_ResetParams.GetWithDefault("block_scale", 2);
    //     //Set the scale of the block
    //     m_BlockRb.transform.localScale = new Vector3(scale, 0.75f, scale);

    //     // Set the drag of the block
    //     m_BlockRb.drag = m_ResetParams.GetWithDefault("block_drag", 0.5f);
    // }

    // void SetResetParameters()
    // {
    //     SetBlockProperties();
    // }
}
