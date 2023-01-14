using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Box = Boxes.Box;


// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
public class CollideAndCombineMesh : MonoBehaviour
{
    public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
    // public Rigidbody rb;
    public PackerHand agent;

    public bool overlapped;

    public Vector3 direction;
    public float distance;

    public Transform hitObject;

    public Box box; // Box Spawner

    public MeshFilter[] meshList;



    void Start()
    {
        // instantiate the Collider component
        //c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)

        meshList = GetComponentsInChildren<MeshFilter>(); 
        
        // Combine meshes
        MeshCombiner(meshList);


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


    /// <summary>
    //// Adjust position of box and calls mesh combiner
    //// happens when the selected position is good enough
    ///</summary>
    void OnTriggerEnter() {

        Transform box = agent.carriedObject;
        Debug.Log($"HIT OBJECT INSIDE TRIGGER IS {box}");
        var parent_mc =  GetComponent<Collider>();
        var box_mc = box.GetComponent<Collider>();
        Vector3 boxPosition = box.position;
        Quaternion boxRotation = box.rotation;

        overlapped = Physics.ComputePenetration(
        parent_mc, transform.position, transform.rotation,
        box_mc, boxPosition, boxRotation,
        out direction, out distance
        );

        Debug.Log($"OVERLAPPED IS: {overlapped} for BOX {box.name}");

        if (overlapped==true) {
            // Adjust box position 
            Debug.Log($"BOX {box.name} START POSITION IS {box.position}");
            box.position += direction * (distance);
            Debug.Log($"BOX {box.name} FINAL POSITION IS {box.position}");
            // Make box child of bin
            box.parent = transform;
            // Combine bin and box meshes
            meshList = GetComponentsInChildren<MeshFilter>(); 
            MeshCombiner(meshList);
            overlapped = false;
            // Trigger the next round of picking
            agent.StateReset();
            //agent.isDroppedoff = true;
        }
    }
    
 


    void MeshCombiner(MeshFilter[] meshList) {
        Debug.Log("++++++++++++START OF MESHCOMBINER++++++++++++");
        List<CombineInstance> combine = new List<CombineInstance>();

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

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[meshList.Length];
        for (int i = 0; i < meshList.Length; i++)
        {
            materials[i] = meshList[i].GetComponent<Renderer>().sharedMaterial;
        }
        mr.materials = materials;
        
         // Create a new mesh on bin
        //MeshFilter parent_mf = gameObject.GetComponent<MeshFilter>();
        //parent_mf.mesh = new Mesh();
        MeshFilter parent_mf = gameObject.AddComponent<MeshFilter>();
        // Combine the meshes
        // Debug.Log($"PARENT_MESH IN MESH COMBINER IS: {parent_mf}");
        // Debug.Log($"COMBINE IN MESH COMBINER IS {combine}");
        parent_mf.mesh.CombineMeshes(combine.ToArray(), true, true);

        // Create a mesh collider from the parent mesh
        Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        MeshCollider parent_mc = gameObject.AddComponent<MeshCollider>(); // create parent_mc mesh collider 
        parent_mc.convex = true;
        parent_mc.sharedMesh = parent_m; // add the mesh shape (from the parent mesh) to the mesh collider



        Debug.Log("+++++++++++END OF MESH COMBINER+++++++++++++");

    }
}
