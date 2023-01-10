using UnityEngine;
using System.Collections.Generic;

// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
public class CollideAndCombineMesh : MonoBehaviour
{
    public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
    // public Rigidbody rb;

    public Transform transformTwin;

    public PackerHand agent;

    void Start()
    {
        // instantiate the Collider component
        c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)
        // instantiate the MeshCollider component from Collider component
        // MeshCollider mc = c.GetComponent<MeshCollider>();


        // Create a single parent mesh from all children meshes
        List<CombineInstance> combine = new List<CombineInstance>();
        MeshFilter[] meshList = GetComponentsInChildren<MeshFilter>(); 
        for (int i = 0; i < meshList.Length; i++)
        {
            // Get the mesh and its transform component
            Mesh mesh = meshList[i].GetComponent<MeshFilter>().mesh;
            Transform transform = meshList[i].transform;

            // Debug.Log($"@@@@@@@@ next: {transform.position}");
            
            // Vector3 transform2_position = new Vector3(20,0,30);
            // transformTwin.position = transform.position - transform2_position;
            // transformTwin.rotation = transform.rotation;
            // transformTwin.localPosition = transform.localPosition - transform2_position;
            // transformTwin.localRotation = transform.localRotation;
            // transformTwin.localScale = transform.localScale;

            // Set the mesh's position to the same position as its original replicant mesh (origin 0,0,0)
            // transform.position = new Vector3(0,0,0);

            // transformTwin = transform;

            // Create a new CombineInstance and set its properties
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;

            // Vector3 transform2_position = new Vector3(-20,0,-30);
            // transformTwin.position = transform.position + transform2_position;
            // // Vector3 transform_vector3 = transform.position - transform2.position;
            // Debug.Log($"@@@@@@@@ future: {transform.position}");

            // Vector3 position = new Vector3(0, 20, 0);
            // Quaternion rotation = Quaternion.identity;
            // Vector3 scale = Vector3.one;
            // Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
            // ci.transform = matrix; // Matrix4x4

            // ci.transform = transform.localToWorldMatrix;
            // Transform preprocessed_transform = new Vector3(0,0,0); 
            // ci.transform = transform.localToWorldMatrix; // Matrix4x4, position is off as it needs to be 0,0,0
            
            
            ci.transform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale); // Matrix4x4, position is off as it needs to be 0,0,0
            // ci.transform = Matrix4x4.TRS(new Vector3(0,0,0), Quaternion.identity, Vector3.one); // Matrix4x4, position is off as it needs to be 0,0,0
//Problem:  // NEED TO MINUS -Vector3(20,0,30) or (-20,0,-30) from the ci.transform (Matrix4x4) position because it is double adding the positon
//Solution: // TRS
            // Debug.Log($"@@@@@@@@ position of ci.transform before: {ci.transform.GetPosition()}");
            // ci.transform.SetColumn(3, new Vector4(2, 1, 3, 1));
            // Debug.Log($"@@@@@@@@ position of ci.transform after: {ci.transform.GetPosition()}");

            // Vector3 transform2_position = new Vector3(-20,0,-30);
            // ci.transform.GetPosition() = ci.transform.GetPosition() + transform2_position;
            // // subtract -20,0,-30 from the last colum in ci.transform

            // ci.transform.SetColumn(2, new Vector4(0,0,0,1));

            // move ci.transform to original mesh's position
            // ci.transform[0,3] = 0;
            // Matrix4x4 citgp = ci.transform;
            // Debug.Log($"@@@@@@@@ next: {ci.transform}");

            // // local-position = Vector3 of local
            // Vector3 ci_position = new Vector3(0,0,0);
            // Quaternion ci_rotation = Quaternion.Euler(0, 0, 0);
            // Vector3 ci_scale = new Vector3(1, 1, 1);
            // // Quaternion rotation = Quaternion.identity;
            // // Vector3 scale = Vector3.one;
            // ci.transform.SetTRS(ci_position, ci_rotation, ci_scale);
            // // Matrix4x4 m = ci.transform;
            // // m.TRS(ci_position, ci_rotation, ci_scale);

            // Add the CombineInstance to the list
            combine.Add(ci);
        }
        // Create a new mesh on the GameObject
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
        // Move the combined mesh to local position 
        // parent_mf.transform.position = new Vector3(0,0,0);

        // Create a mesh collider from the parent mesh
        Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        MeshCollider parent_mc = gameObject.AddComponent<MeshCollider>(); // create parent_mc mesh collider 
        parent_mc.sharedMesh = parent_m; // add the mesh shape (from the parent mesh) to the mesh collider

    }


    void Update() {
        
    }


    void OnCollisionEnter(Collision collision)
    {
        // if collision is with anything other than the ground 
        // - note: future optimization via turn off collisions with ground for optimization? turn off by turning on kinematics for ground?

        if (collision.gameObject.name != "Ground")
        {
            Debug.Log($"*@@@@@ Collision of {collision.gameObject.name} and {collision.gameObject.name} @@@@@");
        }

        // if collision object is an unorganized box (tag "0")
        if (collision.gameObject.CompareTag("0"))
        {
            // Make box child of bin
            collision.transform.parent = transform;
            
            // Combine meshes of bin and box
            meshCombiner(collision.gameObject);

            // Update parent mesh
            //UpdateMesh();

            // Get the array of contact points from the collision
            ContactPoint[] contacts = collision.contacts;
            float surfaceArea = 0;
            // Loop through each contact point
            foreach (ContactPoint contact in contacts)
            {
                // Calculate the projection of the surface area onto the normal vector
                float projection = -Vector3.Dot(contact.normal, contact.point);

                // Multiply the projection by the length of the normal vector to get the surface area
                surfaceArea = surfaceArea + projection * contact.normal.magnitude;
            }
            agent.RewardSurfaceArea(surfaceArea);
            // Update bin volume
            agent.UpdateBinVolume();
            // Update bin bounds
            agent.UpdateBinBounds();
      }

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
        ci.transform = transform.localToWorldMatrix;

        // Add the CombineInstance to the list
        ciList.Add(ci);

        // Get the mesh filter on the GameObject
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();

        // MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        // // Set the materials of the new mesh to the materials of the original meshes
        // Material[] materials = new Material[1];
        // // for (int i = 0; i < meshList.Count; i++)
        // // {
        // materials[0] = collisionObject.GetComponent<Renderer>().sharedMaterial;
        // // }
        // mr.materials = materials;

        // Combine the meshes
        mf.mesh.CombineMeshes(ciList.ToArray(), true, true);

        Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%   MESH IS COMBINED   %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
    }
}
