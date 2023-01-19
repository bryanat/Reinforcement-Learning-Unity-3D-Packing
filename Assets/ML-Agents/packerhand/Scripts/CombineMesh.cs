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

    public bool dontloopinfinitely;


    public MeshFilter parent_mf;



    void Start()
    {
        // instantiate the Collider component
        //c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)

        var meshList = GetComponentsInChildren<MeshFilter>(); 
        Debug.Log($"{name}: beging meshList length: {meshList.Length}");
        
        // Combine meshes
        MeshCombiner(meshList);


     }

    

    void OnCollisionEnter(Collision collision) { // COLLISION IS HAPPENING FIRST BEFORE DROPOFFBOX()
                                                 // COLLISION NEEDS TO HAPPEN AFTER DROPOFF BOX
                                                 // NEED TO DROPOFF BOX BEFORE COLLISION
                                                 // SET isTrigger IN DROPOFF BEFORE COLLISION USES isTrigger
        Debug.Log($"ENTERED COLLISION for BOX {collision.gameObject.name} AND MESH {name}");

        Debug.Log($"AGENT TESTN INSIDE ONCOLLISION ENTER IS {agent.testn}");



        
        // GREEN
        // if this mesh is Bottom Green mesh and a box collides with it then set isCollidedGreen collision property to true
        if (name == "BinIso20Bottom" && collision.gameObject.tag == "pickedupbox")
        {
            // set mesh property isCollidedGreen to true, used when all three colors are true then combinemeshes
            isCollidedGreen = true;
            Debug.Log($"{name}: BCA isCollidedGreen triggered, value of isCollidedGreen: {isCollidedGreen}");

            // get the name of the opposite side using the collision gameObject
            string green_opposite_side_name = GetOppositeSide(collision.transform); // bottom => top
            // get the gameObject of the opposite side using the name of the opposite side 
            GameObject green_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{green_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{green_opposite_side_name}");
            Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject}");
        }
        // BLUE
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        if (name == "BinIso20Back" && collision.gameObject.tag == "pickedupbox"){
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isCollidedBlue = true;
            Debug.Log($"{name}: BCA isCollidedBlue triggered, value of isCollidedBlue: {isCollidedBlue}");

            // get the name of the opposite side using the collision gameObject 
            string blue_opposite_side_name = GetOppositeSide(collision.transform); // back => front

            // get the gameObject of the opposite side using the name of the opposite side
            GameObject blue_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}");
             Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject}");

        }

        // RED
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        if (name == "BinIso20Side" && collision.gameObject.tag == "pickedupbox")
        {
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isCollidedRed = true;

            Debug.Log($"{name}: BCA isCollidedRed triggered, value of isCollidedRed: {isCollidedRed}");

            // get the name of the opposite side using the collision gameObject // right
            string red_opposite_side_name = GetOppositeSide(collision.transform); // left => right

            // get the gameObject of the opposite side using the name of the opposite side 
            GameObject red_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject =  GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}");
             Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject}");
        }

        // if all three meshes have contact, then allow combining meshes // TRIGGERED BY EACH BINISO20SIDE 
        if (GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().isCollidedGreen && GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().isCollidedBlue && GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().isCollidedRed)
        {
        
            //if (dontloopinfinitely == false) {


                Debug.Log($"AGENT TESTN INSIDE INFINITEY LOOP IS {agent.testn}");
        
                //////NOT ADDED TO THE CORRECT PARENT////////////////
                oppositeSideObject.transform.parent = this.transform;
                var meshList = GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(meshList);

                isCollidedGreen = false;
                isCollidedBlue = false;
                isCollidedRed = false; 

                dontloopinfinitely = true;

                agent.isDroppedoff = true;
           // }
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


    void GetVertices() 
    {
        // var mesh = GetComponent<MeshFilter>().mesh;
        var mesh = parent_mf.mesh;
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
    

    void MeshCombiner(MeshFilter[] meshList) 
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