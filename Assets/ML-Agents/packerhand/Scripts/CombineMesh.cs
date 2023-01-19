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

    //public string meshname;

    //public Transform [] allSidesOfBox;

    ///public GameObject unitbox;

    public bool isCollidedGreen;
    public bool isCollidedBlue;
    public bool isCollidedRed;

    public List<GameObject> greenlist_CollisionGameObjects;
    public List<GameObject> bluelist_CollisionGameObjects;
    public List<GameObject> redlist_CollisionGameObjects;

    public GameObject binBottom;
    public GameObject binBack;
    public GameObject binSide;

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

        // Identify ground, side or back mesh
        //meshname = this.name;

        binBottom = GameObject.Find("BinIso20Bottom");
        binSide = GameObject.Find("BinIso20Side");
        binBack = GameObject.Find("BinIso20Back");


        if (name == "BinIso20Bottom") {
   
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                 greenlist_CollisionGameObjects.Add(go_mf.gameObject);
            }
            // foreach (GameObject obj in binBottom.transform) 
            // {
            //     greenlist_CollisionGameObjects.Add(obj);
            //     Debug.Log($"GREEN LIST GAME OBJECTS ON START ARE: {greenlist_CollisionGameObjects} ");
            // }
        }
        else if (name == "BinIso20Back") {
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                 bluelist_CollisionGameObjects.Add(go_mf.gameObject);
            }
        }
         
        else if (name == "BinIso20Side") {
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                redlist_CollisionGameObjects.Add(go_mf.gameObject);
            }
        }
     }

    

    void OnCollisionEnter(Collision collision) { // COLLISION IS HAPPENING FIRST BEFORE DROPOFFBOX()
                                                 // COLLISION NEEDS TO HAPPEN AFTER DROPOFF BOX
                                                 // NEED TO DROPOFF BOX BEFORE COLLISION
                                                 // SET isTrigger IN DROPOFF BEFORE COLLISION USES isTrigger
        Debug.Log($"ENTERED COLLISION for BOX {collision.gameObject.name} AND MESH {name}");



        
        // GREEN
        // if this mesh is Bottom Green mesh and a box collides with it then set isCollidedGreen collision property to true
        // if (name == "BinIso20Bottom" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "bottom")
        // {
        if (name == "BinIso20Bottom" && collision.gameObject.tag == "pickedupbox")
        {
            // set mesh property isCollidedGreen to true, used when all three colors are true then combinemeshes
            isCollidedGreen = true;
            Debug.Log($"{name}: BCA isCollidedGreen triggered, value of isCollidedGreen: {isCollidedGreen}");
            // add collision.gameObject to color collisionList
            // greenlist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            // foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            // {
            //     greenlist_CollisionGameObjects.Add(go_mf.gameObject);
            // }

            
            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // top
            string green_opposite_side_name = GetOppositeSide(collision.transform); // bottom => top
            // get the gameObject of the opposite side using the name of the opposite side // top gameObject
            GameObject green_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{green_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")

            greenlist_CollisionGameObjects.Add(green_opposite_side_gameObject); // add opposite side instead
            ///////////////////////////////////////////////////////////////////////////////////////////////////
        }
        // BLUE
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        // if (name == "BinIso20Back" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "back"){
        if (name == "BinIso20Back" && collision.gameObject.tag == "pickedupbox"){
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isCollidedBlue = true;
            Debug.Log($"{name}: BCA isCollidedBlue triggered, value of isCollidedBlue: {isCollidedBlue}");
            // add collision.gameObject to color collisionList
            // bluelist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            // foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            // {
            //     bluelist_CollisionGameObjects.Add(go_mf.gameObject);
            // }

            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // front
            string blue_opposite_side_name = GetOppositeSide(collision.transform); // back => front

            // get the gameObject of the opposite side using the name of the opposite side // front gameObject
            GameObject blue_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")

            // the actual adding part, add the gameObject of the opposite side to the list of objects to merge into the mesh later
            bluelist_CollisionGameObjects.Add(blue_opposite_side_gameObject);
            ///////////////////////////////////////////////////////////////////////////////////////////////////

        }
        // RED
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        //if (name == "BinIso20Side" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "left")
        if (name == "BinIso20Side" && collision.gameObject.tag == "pickedupbox")
        {
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isCollidedRed = true;

            // add collision.gameObject to color collisionList
            // redlist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            // foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            // {
            //     redlist_CollisionGameObjects.Add(go_mf.gameObject);
            // }

            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // right
            string red_opposite_side_name = GetOppositeSide(collision.transform); // left => right

            // get the gameObject of the opposite side using the name of the opposite side // right gameObject
            GameObject red_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")

            // the actual adding part, add the gameObject of the opposite side to the list of objects to merge into the mesh later
            redlist_CollisionGameObjects.Add(red_opposite_side_gameObject);
            ///////////////////////////////////////////////////////////////////////////////////////////////////
        }

        // if all three meshes have contact, then allow combining meshes // TRIGGERED BY EACH BINISO20SIDE 
        // if (GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().isCollidedGreen && GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().isCollidedBlue && GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().isCollidedRed)
        // {
        if (binBottom.GetComponent<CombineMesh>().isCollidedGreen && binBack.GetComponent<CombineMesh>().isCollidedBlue && binSide.GetComponent<CombineMesh>().isCollidedRed)
        {
        
            if (dontloopinfinitely == false) {
        
                isCollidedGreen = false;
                isCollidedBlue = false;
                isCollidedRed = false;

                // var m_BinIso20Bottom = GameObject.Find("BinIso20Bottom");
                // var m_BinIso20Back = GameObject.Find("BinIso20Back");
                // var m_BinIso20Side = GameObject.Find("BinIso20Side");



                Transform box = agent.targetBox.transform; // error since agent drops off box now (DropoffBox()) before collision (OnCollisionEnter()) 


                // GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Bottom").transform); // bottom.parent = BinIso20Bottom
                // GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Back").transform); // back.parent = BinIso20Back
                // GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Side").transform); // left.parent = BinIso20Side
                
                ///// CANNOT HARD CODE ELEMENT AT 2 IF WE HAVE MORE THAN ONE BOX//////////////////////////
                binBottom.GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Bottom").transform); // bottom.parent = BinIso20Bottom
                binBack.GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Back").transform); // back.parent = BinIso20Back
                binSide.GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Side").transform); // left.parent = BinIso20Side
                // binBottom.GetComponent<CombineMesh>().greenlist_CollisionGameObjects.LastOrDefault().GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Bottom").transform); // bottom.parent = BinIso20Bottom
                // binBack.GetComponent<CombineMesh>().bluelist_CollisionGameObjects.LastOrDefault().GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Back").transform); // back.parent = BinIso20Back
                // binSide.GetComponent<CombineMesh>().redlist_CollisionGameObjects.LastOrDefault().GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Side").transform); // left.parent = BinIso20Side
                

                // GREEN
                if (name == "BinIso20Bottom")
                {
                    // List<MeshFilter> mflist_meshListGreen = new List<MeshFilter>();

                
                    // foreach (GameObject m_go in GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.Skip(1))
                    // {

                    // // foreach (GameObject m_go in greenlist_CollisionGameObjects)
                    // // {
                    //             //////////////////////////////INSIDE THIS LOOP IS THE DECIDING FACTOR//////////////////
                    //             // if mflist_meshListGreen.Add(m_go.GetComponent<MeshFilter>()); doesnt run then doesnt combine mesh

                    //     mflist_meshListGreen.Add(m_go.GetComponent<MeshFilter>());

                    // }

                    // var meshListGreen = mflist_meshListGreen.ToArray();
                    var meshListGreen = binBottom.GetComponentsInChildren<MeshFilter>(); 
                    MeshCombiner(meshListGreen); 
                }



            // BLUE
            if (name == "BinIso20Back"){
                // List<MeshFilter> mflist_meshListBlue = new List<MeshFilter>();
                //             Debug.Log($"{name} ffx- test -x-01");
                //             Debug.Log($"{name} ffx- test -x-71 count list: {bluelist_CollisionGameObjects.Count}");
                // foreach (GameObject m_go in GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.Skip(1))
                // {
                //             Debug.Log($"{name} ffx- test -x-62");
                //             Debug.Log($"{name}: ffx key -x-blue m_go: {m_go}");
                //     mflist_meshListBlue.Add(m_go.GetComponent<MeshFilter>());
                //             Debug.Log($"{name} ffx key after -x-blue");
                // }
                //             Debug.Log($"{name} ffx- test -x-03");

                // var meshListBlue = mflist_meshListBlue.ToArray();
                //     // Debug.Log($"{name}: kab bluelist ElementAt(0) {meshListBlue.ElementAt(1).name}");    
                //             Debug.Log($"{name} ffx- test -x-04");

                // // var meshListBlue = GameObject.Find("BinIso20Back").GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
                // // ps if youre watching this is really bad up above ^^^ can be done in less lines but just trying to do asap  

                //             Debug.Log($"{name} ffx test 8");
                var meshListBlue = binBack.GetComponentsInChildren<MeshFilter>();
                MeshCombiner(meshListBlue); 
            }


            // RED
            if (name == "BinIso20Side") { 
                // List<MeshFilter> mflist_meshListRed = new List<MeshFilter>();
                //             Debug.Log($"{name} ffx- test -x-91 count list: {redlist_CollisionGameObjects.Count}");
                // foreach (GameObject m_go in GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.Skip(1))
                // {
                //             Debug.Log($"{name}: ffx key -x-red m_go: {m_go}");
                //     mflist_meshListRed.Add(m_go.GetComponent<MeshFilter>());
                //             Debug.Log($"{name} ffx key after -x-red");
                // }
                // var meshListRed = mflist_meshListRed.ToArray();
                //     // Debug.Log($"{name}: kab redlist ElementAt(0) {meshListRed.ElementAt(0).name}");    
                // // var meshListRed = GameObject.Find("BinIso20Side").GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
                //             Debug.Log($"{name} ffx test 10");
                var meshListRed = binSide.GetComponentsInChildren<MeshFilter>();
                MeshCombiner(meshListRed); 
            }


    //////////////////////////////////////////////////
    ///////////RUN SUCCESSFULLY 1 LOOP////////////////
    ///////AFTER MESHCOMBINE INFINITE LOOP////////////
    //////////////////////////////////////////////////
                isCollidedGreen = false;
                isCollidedBlue = false;
                isCollidedRed = false; 

                dontloopinfinitely = true;
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