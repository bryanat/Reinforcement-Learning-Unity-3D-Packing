using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using static PickupScript;
using static BinDetect2;
using static BoxDetect2;

namespace Boxes2 {

public class Box2 : MonoBehaviour
{

    public GameObject m_box;
    //public Transform t;
    [HideInInspector]

    public GameObject ground;
    public BinDetect binDetect;

    public PickupScript ps; 
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public Quaternion StartingRot;


    public Bounds areaBounds;

    public BoxSpawner2 boxSpawnerRef;


    public void ResetBoxes(Box2 box)
    {
        box.rb.transform.position = new Vector3(0, 1, 0);//GetRandomSpawnPos();
        box.rb.velocity = Vector3.zero;
        box.rb.angularVelocity = Vector3.zero;

    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// Cannot overlap with the agent or overlap with the bin area
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        areaBounds = boxSpawnerRef.ground.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x, areaBounds.extents.x);

            var randomPosZ = Random.Range(-areaBounds.extents.z, areaBounds.extents.z);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }


}



public class BoxSpawner2 : MonoBehaviour {



    [HideInInspector]
    public List<Box2> boxPool = new List<Box2>();

    [HideInInspector]
    public Box2 boxRef; 

    public PackerHand agent;

    public GameObject ground;



    public void SetUpBoxes() {
        //for each box in json, get a list of box sizes;
        //sizes = readJson(); 
        float[][] sizes = new float[][] {
            new float[] { 1.0f, 2.0f, 3.0f },
            new float[] { 3.0f, 3.0f, 3.0f },
            new float[] { 5.0f, 10.0f, 8.0f }};

        foreach(var size in sizes) {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(size[0], size[1], size[2]);
            ////need to fix randomspawn position function to make it work//////////////////
            var position = new Vector3(0, 1, 0);
            box.transform.position = position;
            box.AddComponent<BinDetect2>();
            box.AddComponent<PickupScript2>();
            box.AddComponent<BoxDetect2>();
            box.AddComponent<Rigidbody>();
            box.AddComponent<BoxCollider>();
            BinDetect2 binDetect = box.GetComponent<BinDetect2>();
            BoxDetect2 boxDetect = box.GetComponent<BoxDetect2>();
            Debug.Log($"BINDETECT SCRIPT IS: {binDetect}");
            binDetect.agent = agent;
            boxDetect.agent = agent;
            var box2 = new Box2{
                rb = box.transform.GetComponent<Rigidbody>(), 
                startingPos = box.transform.position,
                boxSize = box.transform.localScale
            };
            boxPool.Add(box2);          
        }
    }
 

    }
}
