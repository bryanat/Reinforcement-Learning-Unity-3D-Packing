using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public class Box : Monobehavior
{
    public Transform t;
    [HideInInspector]
    public BinDetect binDetect;

    public PickupScript ps; 
    public Rigidbody rb;

    public Vector3 startingPos;

    public Vector3 boxSize; 

    public Quaternion StartingRot;


}







