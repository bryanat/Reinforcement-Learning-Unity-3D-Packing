using UnityEngine;
using System.Collections.Generic;
using System.Collections;


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




    void Start()
    {
        // instantiate the Collider component
        c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)


        List<CombineInstance> combine = new List<CombineInstance>();

        MeshFilter[] meshList = GetComponentsInChildren<MeshFilter>(); 
        for (int i = 0; i < meshList.Length; i++)
        {
            // Get the mesh and its transform component
            Mesh mesh = meshList[i].GetComponent<MeshFilter>().mesh;
            Transform transform = meshList[i].transform;

            // Create a new CombineInstance and set its properties
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            
            ci.transform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale); // Matrix4x4, position is off as it needs to be 0,0,0

            // Add the CombineInstance to the list
            combine.Add(ci);
        }
        // Create a new mesh on bin
        MeshFilter parent_mf = gameObject.AddComponent<MeshFilter>();

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[meshList.Length];
        for (int i = 0; i < meshList.Length; i++)
        {
            materials[i] = meshList[i].GetComponent<Renderer>().sharedMaterial;
        }
        mr.materials = materials;

        // Combine the meshes
        parent_mf.mesh.CombineMeshes(combine.ToArray(), true, true);
        
        // Create a mesh collider from the parent mesh
        Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        MeshCollider parent_mc = gameObject.AddComponent<MeshCollider>(); // create parent_mc mesh collider 
        parent_mc.sharedMesh = parent_m; // add the mesh shape (from the parent mesh) to the mesh collider

    }


    /// <summary>
    //// Use raycast and computer penetration to detect incoming boxes and check for overlapping
    ///</summary>
    void Update() {

        RaycastHit hit;
        int layerMask = 1<<5;
        if(Physics.SphereCast(transform.position, transform.localScale.z, transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            Debug.Log("INSIDE RAYCAST");
            hitObject = hit.transform;
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward)*10, Color.red ,10.0f);
            var parent_mc =  GetComponent<Collider>();
            var box_mc = hit.transform.GetComponent<Collider>();
            Vector3 otherPosition = hit.transform.position;
            Quaternion otherRotation = hit.transform.rotation;

            overlapped = Physics.ComputePenetration(
                parent_mc, transform.position, transform.rotation,
                box_mc, otherPosition, otherRotation,
                out direction, out distance
            );
            Debug.Log($"OVERLAPPED IS: {overlapped}");
        }
    }


    /// <summary>
    //// Adjust position of box and calls mesh combiner
    //// happens when the selected position is good enough
    ///</summary>
    void OnTriggerEnter() {
        Debug.Log("TRIGER OCCURRED INSIDE BIN");
        if (overlapped==true) {
            hitObject.position -= direction * (distance);
            Debug.Log($"BOX FINAL POSITION BECOMES: {hitObject.position}");
            hitObject.parent = transform;
            meshCombiner(hitObject.gameObject);
            overlapped = false;
            agent.isDroppedoff = true;
        }
        
    }
    // void OnCollisionStay() {

    //     Debug.Log("Collision occured inside bin");

    //     // Debug.Log($"OVERLAPPED IS : {overlapped}");
    //     // hitObject.position -= direction * (distance);
    //     // Debug.Log($"BOX FINAL POSITION BECOMES: {hitObject.position}");
    //     // hitObject.parent = transform;
    //     // meshCombiner(hitObject.gameObject);
    //     // agent.isDroppedoff = true;
    //     // hitObject = null;
    //     ////get surface area of contact 
    //     ////punish agent for the amouont of adjustment
    //     ///reward the agent for surface area contact if adjustment is small enough
    // }
 


    // void OnCollisionEnter(Collision collision)
    // {

    //     agent.hasCollided = true;
    //     // Make box child of bin
    //     collision.transform.parent = transform;
        
    //     // Combine meshes of bin and box
    //     meshCombiner(collision.gameObject);

    //     // Get the array of contact points from the collision
    //     ContactPoint[] contacts = collision.contacts;
    //     float surfaceArea = 0;
    //     // Loop through each contact point
    //     foreach (ContactPoint contact in contacts)
    //     {
    //         // Calculate the projection of the surface area onto the normal vector
    //         float projection = -Vector3.Dot(contact.normal, contact.point);

    //         // Multiply the projection by the length of the normal vector to get the surface area
    //         surfaceArea = surfaceArea + projection * contact.normal.magnitude;
    //     }
    //     agent.RewardSurfaceArea(surfaceArea);
    //     // Update bin volume
    //     agent.UpdateBinVolume();
    //     // Update bin bounds
    //     //agent.UpdateBinBounds();


    // }

    void updateSurfaceArea() {

    }


    void meshCombiner(GameObject collisionObject)
    {
        Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%   START MESH COMBINING   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
        Debug.Log($"*@@@@@ Collision of {collisionObject.name} inside meshCombiner @@@@@");

        // Create a list of CombineInstance structures
        List<CombineInstance> ciList = new List<CombineInstance>();

        // Get mesh from the collision object's mesh filter component
        Mesh mesh = collisionObject.GetComponent<MeshFilter>().mesh;
        Transform transform = collisionObject.transform;

        // Create a new CombineInstance and set its properties
        CombineInstance ci = new CombineInstance();
        ci.mesh = mesh;
        //ci.transform = transform.localToWorldMatrix;  ci.transform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale); 
        ci.transform = Matrix4x4.TRS(collisionObject.transform.localPosition, collisionObject.transform.localRotation, collisionObject.transform.localScale);

        // Add the CombineInstance to the list
        ciList.Add(ci);

        // Get mesh of bin+box
        Mesh parent_m = gameObject.GetComponent<MeshFilter>().mesh;

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[1];
        materials[0] = collisionObject.GetComponent<Renderer>().sharedMaterial;
        mr.materials = materials;

        // Combine the meshes
        parent_m.CombineMeshes(ciList.ToArray(), true, true);

        //Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        MeshCollider parent_mc = gameObject.AddComponent<MeshCollider>(); // create parent_mc mesh collider 
        parent_mc.sharedMesh = parent_m; // add the mesh shape (from the parent mesh) to the mesh collider

        Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%   MESH IS COMBINED   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
    }
}
