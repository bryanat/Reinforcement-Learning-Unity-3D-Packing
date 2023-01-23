using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Box = Boxes.Box;


// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
// [RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CombineMesh : MonoBehaviour
{
    public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
    // public Rigidbody rb;
    public PackerHand agent;


    //public MeshFilter[] meshList;

    public bool isCollidedGreen;
    public bool isCollidedBlue;
    public bool isCollidedRed;

    public GameObject oppositeSideObject;

    public MeshFilter parent_mf;


    public GameObject binBottom;

    public GameObject binSide;

    public GameObject binBack;



    void Start()
    {
        // instantiate the Collider component
        //c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)

        // Skip 1 is to skip the parent mesh filter
        var meshList = GetComponentsInChildren<MeshFilter>();//.Skip(1).ToArray(); 
        Debug.Log($"{name}: beging meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
        
        // Combine meshes
        MeshCombiner(meshList);

        binBottom = GameObject.Find("BinIso20Bottom");
        binSide = GameObject.Find("BinIso20Side");
        binBack = GameObject.Find("BinIso20Back");
        isCollidedBlue=false;
        isCollidedRed=false;
        isCollidedGreen=false;


     }


    void OnCollisionEnter(Collision collision) { // COLLISION IS HAPPENING FIRST BEFORE DROPOFFBOX()
                                                 // COLLISION NEEDS TO HAPPEN AFTER DROPOFF BOX
                                                 // NEED TO DROPOFF BOX BEFORE COLLISION
                                                 // SET isTrigger IN DROPOFF BEFORE COLLISION USES isTrigger
        Debug.Log($"ENTERED COLLISION for BOX {collision.gameObject.name} AND MESH {name}");

        
        // GREEN
        Debug.Log($"{name} green {isCollidedGreen == false}, {name == "BinIso20Bottom"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedGreen: {isCollidedGreen == false} name == BinIso20Bottom: {name == "BinIso20Bottom"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Bottom Green mesh and a box collides with it then set isCollidedGreen collision property to true
         if (isCollidedGreen == false && name == "BinIso20Bottom" && collision.gameObject.tag == "pickupbox" && collision.gameObject.name == "bottom")
        //if (isCollidedGreen == false && name == "BinIso20Bottom" && collision.gameObject.tag == "pickupbox")
        {
            // set mesh property isCollidedGreen to true, used when all three colors are true then combinemeshes
            isCollidedGreen = true;
            Debug.Log($"{name}: BCA isCollidedGreen triggered, value of isCollidedGreen: {isCollidedGreen}");

            // get the name of the opposite side using the collision gameObject
            string green_opposite_side_name = GetOppositeSide(collision.transform); // bottom => top

            // get the gameObject of the opposite side using the name of the opposite side 
            // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{green_opposite_side_name}");
            Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");
        }
        // BLUE
        Debug.Log($"{name} blue {isCollidedBlue == false}, {name == "BinIso20Back"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedBlue: {isCollidedBlue == false} name == BinIso20Back: {name == "BinIso20Back"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        if (isCollidedBlue == false && name == "BinIso20Back" && collision.gameObject.tag == "pickupbox" && collision.gameObject.name == "back"){
        //if (isCollidedBlue == false && name == "BinIso20Back" && collision.gameObject.tag == "pickupbox"){
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isCollidedBlue = true;
            Debug.Log($"{name}: BCA isCollidedBlue triggered, value of isCollidedBlue: {isCollidedBlue}");

            // get the name of the opposite side using the collision gameObject 
            string blue_opposite_side_name = GetOppositeSide(collision.transform); // back => front

            // get the gameObject of the opposite side using the name of the opposite side
            // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}");
            Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");

        }

        // RED
        Debug.Log($"{name} red {isCollidedRed == false}, {name == "BinIso20Side"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedRed: {isCollidedRed == false} name == BinIso20Side: {name == "BinIso20Side"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        if (isCollidedRed == false && name == "BinIso20Side" && collision.gameObject.tag == "pickupbox" && (collision.gameObject.name=="left" | collision.gameObject.name=="right"))
        //if (isCollidedRed == false && name == "BinIso20Side" && collision.gameObject.tag == "pickupbox")
        {
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isCollidedRed = true;

            Debug.Log($"{name}: BCA isCollidedRed triggered, value of isCollidedRed: {isCollidedRed}");

            // get the name of the opposite side using the collision gameObject // right
            string red_opposite_side_name = GetOppositeSide(collision.transform); // left => right

            // get the gameObject of the opposite side using the name of the opposite side 
            // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject =  GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}");
            Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");
        }


        // if all three meshes have contact, then allow combining meshes 
        // only entered for the last one mesh 
        if (binBottom.GetComponent<CombineMesh>().isCollidedGreen && binBack.GetComponent<CombineMesh>().isCollidedBlue && binSide.GetComponent<CombineMesh>().isCollidedRed)
        {

            // // // Dont need this if unit box side scale 1 works (in the past side scale 1 caused mis-collision due to thiccness, but no longer)
            // // Turn (0.5) scaled side back to unit (1.0) scale side by multiplying each transform.scale by 2 (was halved before)
            // Transform[] array_children_transforms = collision.gameObject.transform.parent.GetComponentsInChildren<Transform>();
            // foreach( Transform child_transform in array_children_transforms){
            //     // child transform is 0.5, multiple by 2 to get 1.
            //     child_transform.localScale = child_transform.localScale * 2f;
            // }
        
            // GREEN
            if (name == "BinIso20Bottom") {

                binBottom.GetComponent<CombineMesh>().oppositeSideObject.transform.parent = binBottom.transform;
                var greenMeshList = binBottom.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(greenMeshList);
                Debug.Log("MMM MESH COMBINED FOR BOTTOM MESH");
                isCollidedGreen = false;
                /// if this state change is called outside in the script of all three meshes, isDroppedoff will be called 3 times and vertices updated 3 times
                if (agent.isBottomMeshCombined == false) {
                    agent.isBottomMeshCombined = true;
                }    
            }
            // BLUE
            if (name == "BinIso20Back") {

                binBack.GetComponent<CombineMesh>().oppositeSideObject.transform.parent = binBack.transform;
                var blueMeshList = binBack.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(blueMeshList);
                Debug.Log("MMM MESH COMBINED FOR BACK MESH");
                isCollidedBlue = false;
                if (agent.isBackMeshCombined == false) {
                    agent.isBackMeshCombined = true;
                }
            }
            // RED
            if (name == "BinIso20Side") {

                binSide.GetComponent<CombineMesh>().oppositeSideObject.transform.parent = binSide.transform;
                var redMeshList = binSide.GetComponentsInChildren<MeshFilter>(); 
                isCollidedRed = false;
                MeshCombiner(redMeshList);
                Debug.Log("MMM MESH COMBINED FOR SIDE MESH");
                if (agent.isSideMeshCombined == false) {
                    agent.isSideMeshCombined = true;
            
                }

            }
        }
    }


    string GetOppositeSide(Transform side) 
    {
        if (side.name == "left") {
            return "right";
        }
        else if (side.name == "right") {
            return "left";
        }
        else if (side.name == "top") {
            return "bottom";
        }
        else if (side.name == "bottom") {
            return "top";
        }
        else if (side.name == "front") {
            return "back";
        }
        else {
            return "front";
        }
    }


    void OnDrawGizmos() {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Matrix4x4 localToWorld = transform.localToWorldMatrix;
 
        for(int i = 0; i<mesh.vertices.Length; ++i){
            Vector3 world_v = localToWorld.MultiplyPoint3x4(mesh.vertices[i]);
            Debug.Log($"Vertex position is {world_v}");
            if (name == "BinIso20Bottom") {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(world_v, 0.1f);
            }
            if (name == "BinIso20Back") {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(world_v, 0.1f);
            }
            if (name == "BinIso20Side") {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(world_v, 0.1f);
            }
        }
    }


    

    public void MeshCombiner(MeshFilter[] meshList) 
    {
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
        
         // Add mesh fileter if doesn't exist
        parent_mf = gameObject.GetComponent<MeshFilter>();
        if (!parent_mf)  {
            parent_mf = gameObject.AddComponent<MeshFilter>();
        }

        // Destroy old mesh and combine new mesh
        Mesh oldmesh = parent_mf.sharedMesh;
        DestroyImmediate(oldmesh);
        parent_mf.mesh = new Mesh();
        parent_mf.mesh.CombineMeshes(combine.ToArray(), true, true);

        // restore the parent pos+rot
        transform.position = position;
        transform.rotation = rotation;

        // Create a mesh collider if doesn't exist
        MeshCollider parent_mc = gameObject.GetComponent<MeshCollider>(); // create parent_mc mesh collider 
        if (!parent_mc) {
            parent_mc = gameObject.AddComponent<MeshCollider>();
        }
        parent_mc.convex = true;
        parent_mc.sharedMesh = parent_mf.mesh; // add the mesh shape (from the parent mesh) to the mesh collider

        Debug.Log("+++++++++++END OF MESH COMBINER+++++++++++++");
    }
}