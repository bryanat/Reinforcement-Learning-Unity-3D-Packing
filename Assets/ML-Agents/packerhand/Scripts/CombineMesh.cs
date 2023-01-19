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

    // public bool overlapped;

    // public Vector3 direction;
    // public float distance;

    // public Transform hitObject;

    // public Box box; // Box Spawner

    public MeshFilter[] meshList;

    public string meshname;

    public bool enteredTrigger;

    public Transform [] allSidesOfBox;

    ///public GameObject unitbox;

    public bool isCollidedGreen;
    public bool isCollidedBlue;
    public bool isCollidedRed;

    public List<GameObject> greenlist_CollisionGameObjects;
    public List<GameObject> bluelist_CollisionGameObjects;
    public List<GameObject> redlist_CollisionGameObjects;

    public bool dontloopinfinitely;


    public MeshFilter parent_mf;


    void Start()
    {
        // instantiate the Collider component
        //c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
        // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)

        meshList = GetComponentsInChildren<MeshFilter>(); 
        Debug.Log($"{name}: beging meshList length: {meshList.Length}");
        
        // Combine meshes
        MeshCombiner(meshList);

        // Identify ground, side or back mesh
        meshname = this.name;

        Debug.Log($"this name is {name}");
        Debug.Log($"{name}: AAA isCollidedGreen: {isCollidedGreen}");
        Debug.Log($"{name}: AAA isCollidedBlue: {isCollidedBlue}");
        Debug.Log($"{name}: AAA isCollidedRed: {isCollidedRed}"); 
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
    

    void OnCollisionEnter(Collision collision) { // COLLISION IS HAPPENING FIRST BEFORE DROPOFFBOX()
                                                 // COLLISION NEEDS TO HAPPEN AFTER DROPOFF BOX
                                                 // NEED TO DROPOFF BOX BEFORE COLLISION
                                                 // SET isTrigger IN DROPOFF BEFORE COLLISION USES isTrigger
        Debug.Log($"ENTERED COLLISION for BOX {collision.gameObject.name} AND MESH {meshname}");


        ///////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////
        ///////////////I WILL CLEANUP & REFACTOR///////////////////
        //////////////////ALL OF THIS TOMORROW/////////////////////
        ///////////////////////IT IS LATE//////////////////////////
        ///////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////


        Debug.Log($"{name}: ACA collision.gameObject: {collision.gameObject}");
        Debug.Log($"{name}: ACA collision.gameObject.tag: {collision.gameObject.tag}");
        
        // GREEN
        // if this mesh is Bottom Green mesh and a box collides with it then set isCollidedGreen collision property to true
        if (name == "BinIso20Bottom" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "bottom")
        {
            // set mesh property isCollidedGreen to true, used when all three colors are true then combinemeshes
            isCollidedGreen = true;
            Debug.Log($"{name}: BCA isCollidedGreen triggered, value of isCollidedGreen: {isCollidedGreen}");
            // add collision.gameObject to color collisionList
            // greenlist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                greenlist_CollisionGameObjects.Add(go_mf.gameObject);
            }

            
            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // top
            string green_opposite_side_name = GetOppositeSide(collision.gameObject.GetComponent<Transform>()); // bottom => top
            Debug.Log($"{name}: JAC opposite side name: {green_opposite_side_name}");
            // get the gameObject of the opposite side using the name of the opposite side // top gameObject
            GameObject green_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{green_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            Debug.Log($"{name}: JAC opposite side name: supsupsup_green");
            Debug.Log($"{name}: JAC opposite side name gameobject green: {green_opposite_side_gameObject}");
            // the actual adding part, add the gameObject of the opposite side to the list of objects to merge into the mesh later
            greenlist_CollisionGameObjects.Add(green_opposite_side_gameObject); // add opposite side instead
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            
            // Debug.Log($"{name}: BFB collision.gameObject: {collision.gameObject.name} added to greenlist");
            Debug.Log($"{name}: JAC opposite collision.gameObject: {green_opposite_side_gameObject.name} added to greenlist");
            Debug.Log($"{name}: hcc greenlist_CollisionGameObjects: {greenlist_CollisionGameObjects.Count}"); // error: 0
        }
        // BLUE
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        if (name == "BinIso20Back" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "back"){
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isCollidedBlue = true;
            Debug.Log($"{name}: BCA isCollidedBlue triggered, value of isCollidedBlue: {isCollidedBlue}");
            // add collision.gameObject to color collisionList
            // bluelist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                bluelist_CollisionGameObjects.Add(go_mf.gameObject);
            }

            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // front
            string blue_opposite_side_name = GetOppositeSide(collision.gameObject.GetComponent<Transform>()); // back => front
            Debug.Log($"{name}: JAC opposite side name: {blue_opposite_side_name}");
            // get the gameObject of the opposite side using the name of the opposite side // front gameObject
            GameObject blue_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            Debug.Log($"{name}: JAC opposite side name: supsupsup_blue");
            Debug.Log($"{name}: JAC opposite side name gameobject blue: {blue_opposite_side_gameObject}");
            // the actual adding part, add the gameObject of the opposite side to the list of objects to merge into the mesh later
            bluelist_CollisionGameObjects.Add(blue_opposite_side_gameObject);
            ///////////////////////////////////////////////////////////////////////////////////////////////////

            // Debug.Log($"{name}: BFB collision.gameObject: {collision.gameObject.name} added to bluelist");
            Debug.Log($"{name}: JAC opposite collision.gameObject: {blue_opposite_side_gameObject.name} added to bluelist");
            Debug.Log($"{name}: hcc bluelist_CollisionGameObjects: {bluelist_CollisionGameObjects.Count}"); // error: 0
        }
        // RED
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        if (name == "BinIso20Side" && collision.gameObject.tag == "pickedupbox" && collision.gameObject.name == "left"){
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isCollidedRed = true;
            Debug.Log($"{name}: BCA isCollidedRed triggered, value of isCollidedRed: {isCollidedRed}");
            // add collision.gameObject to color collisionList
            // redlist_CollisionGameObjects.Add(gameObject.GetComponentInChildren<MeshFilter>().gameObject);
            foreach (MeshFilter go_mf in gameObject.GetComponentsInChildren<MeshFilter>()) 
            {
                redlist_CollisionGameObjects.Add(go_mf.gameObject);
            }

            ////////////////ADD OPPOSITE COLLIDING GAMEOBJECT TO LIST FOR LATER MESH MERGE//////////////////////
            // get the name of the opposite side using the collision gameObject // right
            string red_opposite_side_name = GetOppositeSide(collision.gameObject.GetComponent<Transform>()); // left => right
            Debug.Log($"{name}: JAC opposite side name: {red_opposite_side_name}");
            // get the gameObject of the opposite side using the name of the opposite side // right gameObject
            GameObject red_opposite_side_gameObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}"); // synatax for getting a child is GameObject.Find("Parent/Child")
            Debug.Log($"{name}: JAC opposite side name: supsupsup_red");
            Debug.Log($"{name}: JAC opposite side name gameobject red: {red_opposite_side_gameObject}");
            // the actual adding part, add the gameObject of the opposite side to the list of objects to merge into the mesh later
            redlist_CollisionGameObjects.Add(red_opposite_side_gameObject);
            ///////////////////////////////////////////////////////////////////////////////////////////////////

            // Debug.Log($"{name}: BFB collision.gameObject: {collision.gameObject.name} added to redlist");
            Debug.Log($"{name}: JAC opposite collision.gameObject: {red_opposite_side_gameObject.name} added to redlist");
            Debug.Log($"{name}: hcc redlist_CollisionGameObjects: {redlist_CollisionGameObjects.Count}"); // error: 0
        }

        // if all three meshes have contact, then allow combining meshes // TRIGGERED BY EACH BINISO20SIDE 
        if (GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().isCollidedGreen && GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().isCollidedBlue && GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().isCollidedRed)
        {
        if (dontloopinfinitely == false) {
        
            isCollidedGreen = false;
            isCollidedBlue = false;
            isCollidedRed = false;

            var m_BinIso20Bottom = GameObject.Find("BinIso20Bottom");
            var m_BinIso20Back = GameObject.Find("BinIso20Back");
            var m_BinIso20Side = GameObject.Find("BinIso20Side");

            Debug.Log($"{name}: {collision.gameObject.name}: CCC TRIMESH CONTACT ON ALL THREE MESHES. GameObject.Find(BinIso20Bottom).GetComponent<CombineMesh>().isCollidedGreen: {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().isCollidedGreen} isCollidedBlue: {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().isCollidedBlue} isCollidedRed: {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().isCollidedRed} ");
            // Debug.Log($"{name}: CCCc TRIMESH CONTACT ON ALL THREE MESHES. BinIso20Bottom.GetComponent<CombineMesh>().isCollidedGreen: {m_BinIso20Bottom.GetComponent<CombineMesh>().isCollidedGreen} BinIso20Back.GetComponent<CombineMesh>().isCollidedBlue: {m_BinIso20Back.GetComponent<CombineMesh>().isCollidedBlue} BinIso20Side.GetComponent<CombineMesh>().isCollidedRed: {m_BinIso20Side.GetComponent<CombineMesh>().isCollidedRed} ");
            
            // COMBINE MESHES
            {
                // var currentCollision = collision;
                Debug.Log($"{name}: ABC INSIDE LOOP COLLISION for BOX {collision.gameObject.name} AND MESH {meshname}");
                // Debug.Log($"RIGID BODY INSIDE COLLISION IS {agent.carriedObject.GetComponent<Rigidbody>().position}");
                //SenteredTrigger = false;
                // Transform box = agent.carriedObject.transform; // error since agent drops off box now (DropoffBox()) before collision (OnCollisionEnter()) 
                Transform box = agent.targetBox.transform; // error since agent drops off box now (DropoffBox()) before collision (OnCollisionEnter()) 
                Debug.Log($"{name}: ABC agent.targetBox.transform for BOX {box.name} AND MESH {meshname}");

                // colliding side = side
                Debug.Log($"{name}: DAB Colliding side collision.gameObject.name: {collision.gameObject.name}");
                // // get opposite side
                Debug.Log($"{name}: DAB Colliding opposite side collision.gameObject.name: {GetOppositeSide(collision.gameObject.GetComponent<Transform>())}");
                // add opposite side to Colored mesh
                // combine mesh
                Debug.Log($"{name} ffx test 1");

                // Get all transform component and drop the parent transform with Skip(1)
                // allSidesOfBox = box.GetComponentsInChildren<Transform>().Skip(1).ToArray();
                // Get the side that collided into mesh
                // Transform collidedSide = collision.GetContact(0).otherCollider.transform; // error
            // Transform collidedSide = collision.gameObject.GetComponent<Transform>(); // left
            // Debug.Log($"{name}: DABx COLLIDED SIDE 0 IS {collidedSide.name} ON MESH {meshname}");
            // get opposite side
            // string oppositeSideName = GetOppositeSide(collidedSide); // right
            // Debug.Log($"{name}: DABx SIDE TO BE COMBINED NAME IS {oppositeSideName} FOR MESH {meshname}");


                ///////// all good up to here ///////////
                
                // Set parent of side (aka set side as a child of BinIso20Side) where GameObject.Find(name) is the parent 
                // var sideX = GameObject.Find(oppositeSideName).GetComponent<Transform>().parent;
                // var parentX = GameObject.Find(name).transform; // BinIso20Side
                // GameObject.Find(oppositeSideName).GetComponent<Transform>().parent = GameObject.Find(name).transform; // right.parent = BinIso20Side
                // Debug.Log($"EBB sideX: {sideX}"); // 
                // Debug.Log($"EBB parentX: {parentX}"); // 
                // Debug.Log($"EBC parentX: {GameObject.Find(name).transform}");
                // oppositeSideName.parent = GameObject.Find(name); // right.parent = BinIso20Side
            // Debug.Log($"{name}: EBC parentX: {GameObject.Find(oppositeSideName).GetComponent<Transform>().parent}");
            // Debug.Log($"{name}: EBC should match: {GameObject.Find(name).transform}");

                Debug.Log($"{name} ffx test 2");

            Debug.Log($"{name}: gab length 1 greenlist_CollisionGameObjects: {greenlist_CollisionGameObjects}");

            // 2
            Debug.Log($"{name}: gab length 2 greenlist_CollisionGameObjects: {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 3 greenlist_CollisionGameObjects: {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 4 greenlist_CollisionGameObjects: {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().redlist_CollisionGameObjects.Count}"); // error: 0
            
            Debug.Log($"{name}: gab length 5 bluelist_CollisionGameObjects: {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.Count}"); // error: 0
            // 6
            Debug.Log($"{name}: gab length 6 bluelist_CollisionGameObjects: {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 7 bluelist_CollisionGameObjects: {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().redlist_CollisionGameObjects.Count}"); // error: 0
            
            Debug.Log($"{name}: gab length 8 redlist_CollisionGameObjects: {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 9 redlist_CollisionGameObjects: {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.Count}"); // error: 0
            // 0
            Debug.Log($"{name}: gab length 0 redlist_CollisionGameObjects: {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.Count}"); // error: 0
            
            Debug.Log($"{name}: gab length 01 greenlist_CollisionGameObjects: {greenlist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 02 bluelist_CollisionGameObjects: {bluelist_CollisionGameObjects.Count}"); // error: 0
            Debug.Log($"{name}: gab length 03 redlist_CollisionGameObjects: {redlist_CollisionGameObjects.Count}"); // error: 0

                Debug.Log($"{name} ffx test 3");

            // var test_parent_transform = GameObject.Find("BinIso20Bottom").transform;
            //     Debug.Log($"{name}: test_parent_transform: {test_parent_transform}"); // BinIo20Bottom
            // var test_first_element = greenlist_CollisionGameObjects.First();
            //     Debug.Log($"{name}: test_first_element: {test_first_element}"); // bottom



////////////////////////////////////////////////
////////////////////////////////////////////////
////////////OPPOSITE SIDE & GG//////////////////
////////////////////////////////////////////////
////////////////////////////////////////////////



        Debug.Log($"{name}: before setparent meshList length: {meshList.Length}");

            Debug.Log($"{name}: iaa greenlist ElementAt(0) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(0).name}");    
            Debug.Log($"{name}: iaa greenlist ElementAt(1) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(1).name}");    
            Debug.Log($"{name}: iaa greenlist ElementAt(2) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(2).name}");    
            
            Debug.Log($"{name}: iaa bluelist ElementAt(0) {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(0).name}");    
            Debug.Log($"{name}: iaa bluelist ElementAt(1) {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(1).name}");    
            Debug.Log($"{name}: iaa bluelist ElementAt(2) {GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(2).name}");    
            
            Debug.Log($"{name}: iaa redlist ElementAt(0) {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(0).name}");    
            Debug.Log($"{name}: iaa redlist ElementAt(1) {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(1).name}");    
            Debug.Log($"{name}: iaa redlist ElementAt(2) {GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(2).name}");    


            // .ElementAt(2) is the gameObject side (left, right, top) added from the if statements around code lines 110 to 140 (yes i know its a bad idea to comment like this as code line change, this is debugging comment)
            ///////////////////////////SETPARENT//////////////////////////
            GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Bottom").transform); // bottom.parent = BinIso20Bottom
            GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Back").transform); // back.parent = BinIso20Back
            GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.ElementAt(2).GetComponent<Transform>().SetParent(GameObject.Find("BinIso20Side").transform); // left.parent = BinIso20Side
            // GameObject.Find(oppositeSideName).GetComponent<Transform>().SetParent(GameObject.Find(name).transform); // right.parent = BinIso20Side
            ///////////////////////////SETPARENT//////////////////////////
                Debug.Log($"{name} ffx test 4");
            // Debug.Log($"{name}: iab ElementAt(0) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(0).name}");    
            // Debug.Log($"{name}: iab ElementAt(1) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(1).name}");    
            // // Debug.Log($"{name}: iaa ElementAt(2) {GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.ElementAt(2).name}");    
            //     Debug.Log($"ffx test 5");
                


        Debug.Log($"{name}: after setparent meshList length: {meshList.Length}");

    // MeshCombiner (these two lines) will work once the boxsides (right, top, front) are added to the ////////parent/////// BinIso20Side 
    // meshList = GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
        // Debug.Log($"{name}: afterrr getcomponentsChildren meshList length: {meshList.Length}");

    // MeshCombiner(meshList); // interestingly, this 1 single MeshCombiner call will combine meshes for all 3 separate Meshes, dont need 3 calls (probably something to do with within the MeshCombiner function definition many lines below)

    
                Debug.Log($"{name} ffx test 5");

    // GREEN
    if (name == "BinIso20Bottom")
    {
        List<MeshFilter> mflist_meshListGreen = new List<MeshFilter>();
                    Debug.Log($"{name} ffx- test -x-51 count list: {greenlist_CollisionGameObjects.Count}");
    
        foreach (GameObject m_go in GameObject.Find("BinIso20Bottom").GetComponent<CombineMesh>().greenlist_CollisionGameObjects.Skip(1))
        {
                    //////////////////////////////INSIDE THIS LOOP IS THE DECIDING FACTOR//////////////////
                    // if mflist_meshListGreen.Add(m_go.GetComponent<MeshFilter>()); doesnt run then doesnt combine mesh
                    Debug.Log($"{name} ffx- test -x-52-1 GameObject m_go: {m_go}");
                    Debug.Log($"{name} ffx key -x-green m_go: {m_go}");
            mflist_meshListGreen.Add(m_go.GetComponent<MeshFilter>());
                    Debug.Log($"{name} ffx key after -x-green");
        }
                    Debug.Log($"{name} ffx- test -x-53");
        var meshListGreen = mflist_meshListGreen.ToArray();
                    Debug.Log($"{name} ffx- test -x-54");
            // Debug.Log($"{name}: kab greenlist ElementAt(0) {meshListGreen.ElementAt(0).name}");    
            // Debug.Log($"{name}: kab greenlist ElementAt(1) {meshListGreen.ElementAt(1).name}");    
        // var meshListGreen = GameObject.Find("BinIso20Bottom").GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
                    Debug.Log($"{name} ffx test 6");
        MeshCombiner(meshListGreen); 
    }
                Debug.Log($"{name} ffx- test 7");


    // BLUE
    if (name == "BinIso20Back"){
        List<MeshFilter> mflist_meshListBlue = new List<MeshFilter>();
                    Debug.Log($"{name} ffx- test -x-01");
                    Debug.Log($"{name} ffx- test -x-71 count list: {bluelist_CollisionGameObjects.Count}");
        foreach (GameObject m_go in GameObject.Find("BinIso20Back").GetComponent<CombineMesh>().bluelist_CollisionGameObjects.Skip(1))
        {
                    Debug.Log($"{name} ffx- test -x-62");
                    Debug.Log($"{name}: ffx key -x-blue m_go: {m_go}");
            mflist_meshListBlue.Add(m_go.GetComponent<MeshFilter>());
                    Debug.Log($"{name} ffx key after -x-blue");
        }
                    Debug.Log($"{name} ffx- test -x-03");

        var meshListBlue = mflist_meshListBlue.ToArray();
            // Debug.Log($"{name}: kab bluelist ElementAt(0) {meshListBlue.ElementAt(1).name}");    
                    Debug.Log($"{name} ffx- test -x-04");

        // var meshListBlue = GameObject.Find("BinIso20Back").GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
        // ps if youre watching this is really bad up above ^^^ can be done in less lines but just trying to do asap  

                    Debug.Log($"{name} ffx test 8");
        MeshCombiner(meshListBlue); 
    }
                Debug.Log($"{name} ffx test 9");

    // RED
    if (name == "BinIso20Side") { 
        List<MeshFilter> mflist_meshListRed = new List<MeshFilter>();
                    Debug.Log($"{name} ffx- test -x-91 count list: {redlist_CollisionGameObjects.Count}");
        foreach (GameObject m_go in GameObject.Find("BinIso20Side").GetComponent<CombineMesh>().redlist_CollisionGameObjects.Skip(1))
        {
                    Debug.Log($"{name}: ffx key -x-red m_go: {m_go}");
            mflist_meshListRed.Add(m_go.GetComponent<MeshFilter>());
                    Debug.Log($"{name} ffx key after -x-red");
        }
        var meshListRed = mflist_meshListRed.ToArray();
            // Debug.Log($"{name}: kab redlist ElementAt(0) {meshListRed.ElementAt(0).name}");    
        // var meshListRed = GameObject.Find("BinIso20Side").GetComponentsInChildren<MeshFilter>().Skip(1).ToArray();
                    Debug.Log($"{name} ffx test 10");
        MeshCombiner(meshListRed); 
    }

                Debug.Log($"{name} ffx test 0-");

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



            
    
        // // two colliders cannot have isTrigger=true and collide, hack: to collide turn one trigger of with isTrigger=false allowing collision
        // if (collision.gameObject.GetComponent<Collider>().isTrigger==false) {
        // if (collision.gameObject.CompareTag("droppedoff"))
        // {
        //     Debug.Log($"ABC INSIDE LOOP COLLISION for BOX {collision.gameObject.name} AND MESH {meshname}");
        //     Debug.Log($"RIGID BODY INSIDE COLLISION IS {agent.carriedObject.GetComponent<Rigidbody>().position}");
        //     //SenteredTrigger = false;
        //     Transform box = agent.carriedObject.transform;
        //     // Get all transform component and drop the parent transform with Skip(1)
        //     allSidesOfBox = box.GetComponentsInChildren<Transform>().Skip(1).ToArray();
        //     // Get the side that collided into mesh
        //     Transform collidedSide = collision.GetContact(0).otherCollider.transform;
        //     Debug.Log($"COLLIDED SIDE 0 IS {collidedSide.name} ON MESH {meshname}");
        //     string oppositeSideName = GetOppositeSide(collidedSide);
        //     Debug.Log($"SIDE TO BE COMBINED NAME IS {oppositeSideName} FOR MESH {meshname}");
        //     Transform sideTobeCombined = allSidesOfBox.Where(k => k.gameObject.name == oppositeSideName).FirstOrDefault();
        //     ///Debug.Log($"Side to be combined for mesh {meshname} is {sideTobeCombined}");

        //     if (sideTobeCombined != null) 
        //     {
        //          // Set side to be combined as child of ground, side, or back
        //         sideTobeCombined.parent = transform;
        //         meshList = GetComponentsInChildren<MeshFilter>();
        //         // Combine side mesh into bin mesh
        //         MeshCombiner(meshList); 
        //     }      
        // }}
    }


    // void OnTriggerEnter(Collider box) {
    //     // Refactored OnTriggerEnter code into OnCollisionEnter

    //     // var box_mc = box.GetComponent<Collider>();
    //     // // Set trigger to false so bin won't be triggered by this box anymore

    //     // box_mc.isTrigger = false;
    //     Collider [] m_cList = box.GetComponentsInChildren<Collider>();
    //     foreach (Collider m_c in m_cList) {
    //         m_c.isTrigger = false;
    //     }
    // }

        // // // Make box child of bin
        // Transform boxObject = box.transform;
        // Transform [] allSides = box.GetComponentsInChildren<Transform>();
        // allSides = allSides.Skip(1).ToArray();

        // // Select a child to combine the mesh
        // foreach(Transform side in allSides) 
        // {
        //     Debug.Log($"COLLIDING SIDE IS {side.name}");
        //     //check which side collided with this mesh
        //     if (CheckSideCollided(side)) {
        //         string oppositeSideName = GetOppositeSide(side);

        //         Transform sideTobeCombined = allSides.Where(k => k.gameObject.name == oppositeSideName).FirstOrDefault();
        //         Debug.Log($"Side to be combined for mesh {meshname} is {sideTobeCombined}");

        //         // Set side to be combined as child of ground, side, or back
        //         if (sideTobeCombined != null) {
        //             sideTobeCombined.parent = transform;
        //             meshList = GetComponentsInChildren<MeshFilter>();
        //              // Combine side mesh into bin mesh
        //             MeshCombiner(meshList);
        //         }
        //         //agent.StateReset();
        //         //break;
        //     }
        // }
    
        /////////////////////////////////////////////////////
        //GetVertices();
        ////////////////////////////////////////////////////       
        // Trigger the next round of picking
        //agent.StateReset();
   // }


    bool CheckSideCollided(Transform side) 
    {
        if (meshname == "BinIso20Back") {
            Debug.Log($"Z POSITION OF SIDE {side.name} IS {side.position.z}");
            if (side.localPosition.z - agent.targetBin.position.z < 0.01f) {
                Debug.Log($"BACK MESH AND THE SIDE {side.name} TO COMBINE");
                return true;
            }
        }
        // needs to know to left or right side depends on direction
        else if (meshname == "BinIso20Side") {
            Debug.Log($"X POSITION OF SIDE {side.name} IS {side.position.x}");
            if (side.localPosition.x - agent.targetBin.position.x < 0.01f) {
                Debug.Log($"SIDE MESH AND THE SIDE {side.name} TO COMBINE");
                return true;
            }

        }
        else if (meshname == "BinIso20Bottom") {
            Debug.Log($"Y POSITION OF SIDE {side.name} IS {side.position.y}");
            if (side.localPosition.y - agent.targetBin.position.y < 0.01f) {
                Debug.Log($"BOTTOM MESH AND THE SIDE {side.name} TO COMBINE");
                return true;
            }

        }
        return false;
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
        
         // Create a new mesh on bin
        // parent_mf = gameObject.GetComponent<MeshFilter>();
        if (!parent_mf)  {
            parent_mf = gameObject.AddComponent<MeshFilter>();
        }
        Mesh oldmesh = parent_mf.sharedMesh;
        DestroyImmediate(oldmesh);
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
        // Mesh parent_m = GetComponent<MeshFilter>().mesh; // reference parent_mf mesh filter to create parent mesh
        Mesh parent_m = parent_mf.mesh; // reference parent_mf mesh filter to create parent mesh
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