using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using static PickupScript;

//////////////////////////////  NEED TO CHECK WHERE TO ATTACH THIS SCRIPT //////////////////////////////////////
public class Box : MonoBehaviour
{
    public Transform t;
    [HideInInspector]
    public BinDetect binDetect;

    public PickupScript ps; 
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public Quaternion StartingRot;


    public void Reset(Box box) {
        box.transform.position = box.startingPos;
    }
}







