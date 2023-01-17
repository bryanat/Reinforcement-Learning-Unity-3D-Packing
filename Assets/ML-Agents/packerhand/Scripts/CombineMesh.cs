using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Box = Boxes.Box;


// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CombineMesh : MonoBehaviour
{
    public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
    // public Rigidbody rb;
    public PackerHand agent;

    // public bool overlapped;

    // public Vector3 direction;
    // public float distance;

    // public Transform hitObject;

    // public Box box; // Box Spawner

    public MeshFilter[] meshList;

    public string meshtag;




    void Start()
    {
        // instantiate the Collider component
        //c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)

        meshList = GetComponentsInChildren<MeshFilter>(); 
        
        // Combine meshes
        MeshCombiner(meshList);

        meshtag = this.tag;


    }


    /// <summary>
    //// Use raycast and computer penetration to detect incoming boxes and check for overlapping
    ///</summary>
    // void Update() {

    //     RaycastHit hit;
    //     int layerMask = 1<<5;
    //     if(Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, layerMask))
    //     {
    //         Debug.Log("INSIDE RAYCAST");
    //         hitObject = hit.transform;
    //         Debug.DrawRay(transform.position, transform.forward*20f, Color.red ,10.0f);
    //         var parent_mc =  GetComponent<Collider>();
    //         var box_mc = hit.transform.GetComponent<Collider>();
    //         Vector3 otherPosition = hit.transform.position;
    //         Quaternion otherRotation = hit.transform.rotation;

    //         overlapped = Physics.ComputePenetration(
    //             parent_mc, transform.position, transform.rotation,
    //             box_mc, otherPosition, otherRotation,
    //             out direction, out distance
    //         );
    //         Debug.Log($"OVERLAPPED IS: {overlapped} for BOX {hit.transform.name}");
    //     }
    // }



    void OnTriggerEnter(Collider box) {

        // Get pos, rot, and colliders of box and bin
         //Transform box = agent.carriedObject;
        // Debug.Log($"HIT OBJECT INSIDE TRIGGER IS {box}");
        // var parent_mc =  GetComponent<Collider>();
         var box_mc = box.GetComponent<Collider>();
        // Vector3 boxPosition = box.position;
        // Quaternion boxRotation = box.rotation;

        // // Calculate overlapping distance and direction
        // overlapped = Physics.ComputePenetration(
        // parent_mc, transform.position, transform.rotation,
        // box_mc, boxPosition, boxRotation,
        // out direction, out distance
        // );

        // Debug.Log($"DIRECTION AND DISTANCE OF PENETRATION ARE: {direction}, {distance}");

        // Set trigger to false so bin won't be triggered by this box anymore
        box_mc.isTrigger = false;

        // Adjust box position 
        // Debug.Log($"BOX {box.name} START POSITION IS {box.position}");
        // box.position += direction * (distance);
        // Debug.Log($"BOX {box.name} FINAL POSITION IS {box.position}");
        // // Make box child of bin
        Transform boxObject = box.transform;
        // var tag = box.tag;
        // Debug.Log($"COLLIDED WITH TAG: {tag}");

        // Select a child to combine the mesh
        foreach(Transform side in boxObject) 
        {
            //check which side collided with back

            //side.parent = transform;
            //var sideToCombine = CheckBoxSides(side);
            if (CheckSide(side)) {
                side.parent = transform;
                meshList = GetComponentsInChildren<MeshFilter>();
                MeshCombiner(meshList);
                agent.StateReset();
                break;
                
            }
        }
        //box.transform.parent = transform;
        // Combine bin and box meshes
        //meshList = GetComponentsInChildren<MeshFilter>(); 
        //MeshFilter [] meshList = new [] {box.GetComponent<MeshFilter>()};
        //MeshCombiner(meshList);
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //GetVertices();
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////        
        // Trigger the next round of picking
        //agent.StateReset();
    }

    bool CheckSide(Transform side) {
        if (meshtag == "binback") {
            Debug.Log("COLLIDED INTO BACK BIN");
            if (side.position.z - side.localScale.z - agent.targetBin.position.z < 0.1f) {
                Debug.Log("COLLIDED INTO BACK BIN");
                return true;
            }
        }
        // needs to know to left or right side depends on direction
        else if (meshtag == "binside") {
            Debug.Log("COLLIDED INTO SIDE BIN");
            if (side.position.x - side.localScale.x - agent.targetBin.position.x < 0.1f) {
                Debug.Log("COLLIDED INTO SIDE BIN");
                return true;
            }

        }
        else if (meshtag == "binbottom") {
            Debug.Log("COLLIDED INTO BOTTOM BIN");
            if (side.position.y - side.localScale.y - agent.targetBin.position.y < 0.1f) {
                Debug.Log("COLLIDED INTO BOTTOM BIN");
                return true;
            }

        }
        return false;
    }

    // string GetTobeCombinedSide(Transform side) {
    //     if (side.tag == "1") {
    //         return "2";
    //     }
    //     else if (side.tag == "2") {
    //         return "1";
    //     }
    //     else if (side.tag == "3") {
    //         return "4";
    //     }
    //     else if (side.tag == "4") {
    //         return "3";
    //     }
    //     else if (side.tag == "5") {
    //         return "6";
    //     }
    //     else {
    //         return "5";
    //     }
    // }

    // void OnDrawGizmos() {
    //     var mesh = GetComponent<MeshFilter>().sharedMesh;
    //     Vector3[] vertices = mesh.vertices;
    //     int[] triangles = mesh.triangles;


    //     Matrix4x4 localToWorld = transform.localToWorldMatrix;
 
    //     for(int i = 0; i<mesh.vertices.Length; ++i){
    //         Vector3 world_v = localToWorld.MultiplyPoint3x4(mesh.vertices[i]);
    //         Debug.Log($"Vertex position is {world_v}");
    //         Gizmos.color = Color.blue;
    //         Gizmos.DrawSphere(world_v, 0.1f);
    //     }

    // }


    void GetVertices() {
        var mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;


        Matrix4x4 localToWorld = transform.localToWorldMatrix;
 
        for(int i = 0; i<mesh.vertices.Length; ++i){
            Vector3 world_v = localToWorld.MultiplyPoint3x4(mesh.vertices[i]);
            Debug.Log($"Vertex position is {world_v}");
            //Gizmos.DrawIcon(world_v, "Light tebsandtig.tiff", true);
        }

        // // Iterate over the triangles in sets of 3
        // for(var i = 0; i < triangles.Length; i += 3)
        // {
        //     // Get the 3 consequent int values
        //     var aIndex = triangles[i];
        //     var bIndex = triangles[i + 1];
        //     var cIndex = triangles[i + 2];

        //     // Get the 3 according vertices
        //     var a = vertices[aIndex];
        //     var b = vertices[bIndex];
        //     var c = vertices[cIndex];

        //     // Convert them into world space
        //     // up to you if you want to do this before or after getting the distances
        //     a = transform.TransformPoint(a);
        //     b = transform.TransformPoint(b);
        //     c = transform.TransformPoint(c);

        //     // Get the 3 distances between those vertices
        //     var distAB = Vector3.Distance(a, b);
        //     var distAC = Vector3.Distance(a, c);
        //     var distBC = Vector3.Distance(b, c);

        //     Debug.Log($"INSIDE GETVERTICES: a is {a}, b is {b}, c is {c} ");

        //     // Now according to the distances draw your lines between "a", "b" and "c" e.g.
        //     Debug.DrawLine(transform.TransformPoint(a), transform.TransformPoint(b), Color.red);
        //     Debug.DrawLine(transform.TransformPoint(a), transform.TransformPoint(c), Color.red);
        //     Debug.DrawLine(transform.TransformPoint(b), transform.TransformPoint(c), Color.red);
        // }

    }
    
 


    void MeshCombiner(MeshFilter[] meshList) {
        Debug.Log("++++++++++++START OF MESHCOMBINER++++++++++++");
        List<CombineInstance> combine = new List<CombineInstance>();

        // save the parent pos+rot
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // move to the origin for combining
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

         for (int i = 0; i < meshList.Length; i++)
        {
            // Get the mesh and its transform component
            Mesh mesh = meshList[i].GetComponent<MeshFilter>().mesh;
            Transform transform = meshList[i].transform;

            // Create a new CombineInstance and set its properties
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;

             // Matrix4x4, position is off as it needs to be 0,0,0
            ci.transform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale); 

            // Add the CombineInstance to the list
            combine.Add(ci);

        }

        MeshRenderer parent_mr = gameObject.GetComponent<MeshRenderer>();
        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[meshList.Length];
        for (int i = 0; i < meshList.Length; i++)
        {
            materials[i] = meshList[i].GetComponent<Renderer>().sharedMaterial;
        }
        parent_mr.materials = materials;

        
         // Create a new mesh on bin
        MeshFilter parent_mf = gameObject.GetComponent<MeshFilter>();
        if (!parent_mf)  {
            parent_mf = gameObject.AddComponent<MeshFilter>();
        }
        parent_mf.mesh = new Mesh();
        //parent_mf.mesh.CombineMeshes(combine);
        //transform.gameObject.SetActive(true);


        //MeshFilter parent_mf = gameObject.AddComponent<MeshFilter>();
        // if (!parent_mf.mesh) {
        //     var topLevelMesh = new Mesh();
        //      Debug.Log($"VERTICES IN TOPLEVELMESH {topLevelMesh.vertices}");
        //     parent_mf.mesh = topLevelMesh;
        // }
        //parent_mf.mesh = new Mesh();
        //MeshFilter parent_mf = gameObject.AddComponent<MeshFilter>();
        // Combine the meshes
        // Debug.Log($"PARENT_MESH IN MESH COMBINER IS: {parent_mf}");
        // Debug.Log($"COMBINE IN MESH COMBINER IS {combine}");
        parent_mf.mesh.CombineMeshes(combine.ToArray(), true, true);

        // restore the parent pos+rot
        transform.position = position;
        transform.rotation = rotation;

        // Create a mesh collider from the parent mesh
        Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        MeshCollider parent_mc = gameObject.GetComponent<MeshCollider>(); // create parent_mc mesh collider 
        if (!parent_mc) {
            parent_mc = gameObject.AddComponent<MeshCollider>();
        }
        parent_mc.convex = true;
        //MeshCollider parent_mc = gameObject.AddComponent<MeshCollider>(); 
        parent_mc.sharedMesh = parent_mf.mesh; // add the mesh shape (from the parent mesh) to the mesh collider

        Debug.Log("+++++++++++END OF MESH COMBINER+++++++++++++");

    }
}