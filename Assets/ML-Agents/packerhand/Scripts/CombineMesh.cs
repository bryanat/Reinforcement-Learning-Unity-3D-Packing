using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Box = Boxes.Box;


// Trimesh
[RequireComponent(typeof(MeshRenderer))]
public class CombineMesh : MonoBehaviour
{
    [HideInInspector] public PackerHand agent;
    [HideInInspector] public MeshFilter parent_mf;

    public bool isCollidedGreen = false;
    public bool isCollidedBlue = false;
    public bool isCollidedRed = false;

    public GameObject oppositeSideObject;
    public GameObject sameSideObject;

    public GameObject binBottom;
    public GameObject binSide;
    public GameObject binBack;

    [HideInInspector] CombineMesh m_BackMeshScript; // cache combine mesh script
    [HideInInspector] CombineMesh m_SideMeshScript; // cache combine mesh script
    [HideInInspector] CombineMesh m_BottomMeshScript; // cache combine mesh script
    // [HideInInspector] MeshFilter [] m_BackMeshFilterList; 
    // [HideInInspector] MeshFilter [] m_SideMeshFilterList; 
    // [HideInInspector] MeshFilter [] m_BottomMeshFilterList; 

    public Material clearPlastic;

    void Start() 
    {
        m_BackMeshScript = binBack.GetComponent<CombineMesh>();
        m_SideMeshScript = binSide.GetComponent<CombineMesh>();
        m_BottomMeshScript = binBottom.GetComponent<CombineMesh>();
        // m_BackMeshFilterList =  binBack.GetComponentsInChildren<MeshFilter>();
        // m_SideMeshFilterList =  binSide.GetComponentsInChildren<MeshFilter>();
        // m_BottomMeshFilterList =  binBottom.GetComponentsInChildren<MeshFilter>();

    }


    void OnCollisionEnter(Collision collision) 
    { 
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)
        // Packerhand.cs  : deals parent box : position (math)

        Debug.Log($"ENTERED COLLISION for BOX {collision.gameObject.name} AND MESH {name}");
        
        // BLUE
        //Debug.Log($"{name} RRR blue {isCollidedBlue == false}, {name == "BinIso20Back"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedBlue: {isCollidedBlue == false} name == BinIso20Back: {name == "BinIso20Back"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        if (isCollidedBlue == false && name == "BinIso20Back" && collision.gameObject.tag == "pickupbox" && collision.gameObject.name == "back")
        {
        //if (isCollidedBlue == false && name == "BinIso20Back" && collision.gameObject.tag == "pickupbox"){
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isCollidedBlue = true;
            Debug.Log($"{name}: BCA isCollidedBlue triggered, value of isCollidedBlue: {isCollidedBlue}");

            // get the name of the opposite side using the collision gameObject 
            string blue_opposite_side_name = GetOppositeSide(collision.transform); // back => front

            // get the gameObject of the opposite side using the name of the opposite side
            // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject = GameObject.Find($"{collision.gameObject.transform.parent.name}/{blue_opposite_side_name}");
            //Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");

            sameSideObject = collision.gameObject;
        }

        // GREEN
        //Debug.Log($"{name} RRR green {isCollidedGreen == false}, {name == "BinIso20Bottom"}, {collision.gameObject.tag == "pickupbox"}, {collision.gameObject.name == "bottom"} | collision side:{collision.gameObject.name} AAA isCollidedGreen: {isCollidedGreen == false} name == BinIso20Bottom: {name == "BinIso20Bottom"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
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
            //Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");
            
            sameSideObject = collision.gameObject;
        }

        // RED
        //Debug.Log($"{name} RRR red {isCollidedRed == false}, {name == "BinIso20Side"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedRed: {isCollidedRed == false} name == BinIso20Side: {name == "BinIso20Side"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        if (isCollidedRed == false && name == "BinIso20Side" && collision.gameObject.tag == "pickupbox" && (collision.gameObject.name=="left" | collision.gameObject.name=="right"))
        {
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isCollidedRed = true;

            Debug.Log($"{name}: BCA isCollidedRed triggered, value of isCollidedRed: {isCollidedRed}");

            // get the name of the opposite side using the collision gameObject // right
            string red_opposite_side_name = GetOppositeSide(collision.transform); // left => right

            // get the gameObject of the opposite side using the name of the opposite side 
            // synatax for getting a child is GameObject.Find("Parent/Child")
            oppositeSideObject =  GameObject.Find($"{collision.gameObject.transform.parent.name}/{red_opposite_side_name}");
            //Debug.Log($"OPPOSITE SIDE FOR {name} IS {oppositeSideObject.name}");

            sameSideObject = collision.gameObject;
        }


        // if all three meshes have contact, then allow combining meshes 
        // only entered for the last one mesh 
        if (m_BottomMeshScript.isCollidedGreen & m_BackMeshScript.isCollidedBlue & m_SideMeshScript.isCollidedRed)
        {
            Debug.Log("ENTERED SECOND LOOP");
        
            // BLUE
            if (name == "BinIso20Back" && agent.isBackMeshCombined==false) 
            {

                m_BackMeshScript.oppositeSideObject.transform.parent = binBack.transform;
                m_BackMeshScript.sameSideObject.transform.parent = binBack.transform;
                var blueMeshList = binBack.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(blueMeshList, binBack);
                Debug.Log("MMM MESH COMBINED FOR BACK MESH");
                isCollidedBlue = true;
                agent.isBackMeshCombined = true;
                oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
            }

            // GREEN
            if (name == "BinIso20Bottom" && agent.isBottomMeshCombined==false) 
            {

                m_BottomMeshScript.oppositeSideObject.transform.parent = binBottom.transform;
                m_BottomMeshScript.sameSideObject.transform.parent = binBottom.transform;                
                var greenMeshList = binBottom.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(greenMeshList, binBottom);
                Debug.Log("MMM MESH COMBINED FOR BOTTOM MESH");
                isCollidedGreen = true;
                agent.isBottomMeshCombined = true;  
                oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;  
            }

            // RED
            if (name == "BinIso20Side" && agent.isSideMeshCombined==false) 
            {

                m_SideMeshScript.oppositeSideObject.transform.parent = binSide.transform;
                m_SideMeshScript.sameSideObject.transform.parent = binSide.transform;
                var redMeshList = binSide.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(redMeshList, binSide);
                Debug.Log("MMM MESH COMBINED FOR SIDE MESH");
                isCollidedRed = true;
                agent.isSideMeshCombined = true;
                oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
            }
        }
    }


    string GetOppositeSide(Transform side) 
    {
        if (side.name == "left") 
        {
            return "right";
        }
        else if (side.name == "right") 
        {
            return "left";
        }
        else if (side.name == "top") 
        {
            return "bottom";
        }
        else if (side.name == "bottom") 
        {
            return "top";
        }
        else if (side.name == "front") 
        {
            return "back";
        }
        else {
            return "front";
        }
    }


     public void ForceMeshCombine()
     {
        ////// this function forces combine of all meshes even without certain side collision///////

        Debug.Log("FFF MESH FORCED TO COMBINE!!!!");
        // BLUE
        if (name == "BinIso20Back" && agent.isBackMeshCombined==false) 
        {

            sameSideObject = GameObject.Find($"{agent.boxIdx}/back");
            oppositeSideObject = GameObject.Find($"{agent.boxIdx}/front");
            m_BackMeshScript.oppositeSideObject.transform.parent = binBack.transform;
            m_BackMeshScript.sameSideObject.transform.parent = binBack.transform;
            var blueMeshList = binBack.GetComponentsInChildren<MeshFilter>(); 
            MeshCombiner(blueMeshList, binBack);
            Debug.Log("FFF MESH FORCED TO BE COMBINED FOR BACK MESH");
            isCollidedBlue = false;
            agent.isBackMeshCombined = true;
            oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
            sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
        }
        // RED
        if (name == "BinIso20Side" && agent.isSideMeshCombined==false) 
        {

            sameSideObject = GameObject.Find($"{agent.boxIdx}/left");
            oppositeSideObject = GameObject.Find($"{agent.boxIdx}/right");
            m_SideMeshScript.oppositeSideObject.transform.parent = binSide.transform;
            m_SideMeshScript.sameSideObject.transform.parent = binSide.transform;
            var redMeshList = binSide.GetComponentsInChildren<MeshFilter>(); 
            MeshCombiner(redMeshList, binSide);
            Debug.Log("FFF MESH FORCED TO BE COMBINED FOR SIDE MESH");
            isCollidedRed = false;
            agent.isSideMeshCombined = true;
            oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
            sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
        }
        // GREEN
        if (name == "BinIso20Bottom" && agent.isBottomMeshCombined==false) 
        {

            sameSideObject = GameObject.Find($"{agent.boxIdx}/bottom");
            oppositeSideObject = GameObject.Find($"{agent.boxIdx}/top");
            m_BottomMeshScript.oppositeSideObject.transform.parent = binBottom.transform;
            m_BottomMeshScript.sameSideObject.transform.parent = binBottom.transform;
            var greenMeshList = binBottom.GetComponentsInChildren<MeshFilter>(); 
            MeshCombiner(greenMeshList, binBottom);
            Debug.Log("FFF MESH FORCED TO BE COMBINED FOR BOTTOM MESH");
            isCollidedGreen = false;
            agent.isBottomMeshCombined = true;
            oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
            sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
        }
     }


    public void MeshReset()
    {
        if (name == "BinIso20Bottom")
        {
            while (binBottom.transform.childCount > 2) 
            {
                DestroyImmediate(binBottom.transform.GetChild(binBottom.transform.childCount-1).gameObject);
            }     
            MeshFilter [] meshList = binBottom.GetComponentsInChildren<MeshFilter>();
            Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList, binBottom);

        }
        if (name == "BinIso20Back")
        {
            while (binBack.transform.childCount > 1) 
            {
                DestroyImmediate(binBack.transform.GetChild(binBack.transform.childCount-1).gameObject);
            }  
            MeshFilter [] meshList = binBack.GetComponentsInChildren<MeshFilter>();
            Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList, binBack);
        }
        if (name == "BinIso20Side")
        {
            while (binSide.transform.childCount > 2) 
            {
                DestroyImmediate(binSide.transform.GetChild(binSide.transform.childCount-1).gameObject);
            } 
            MeshFilter [] meshList = binSide.GetComponentsInChildren<MeshFilter>();
            Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList, binSide);
        }

    }

    public void MeshCombiner(MeshFilter[] meshList, GameObject parent) 
    {
        Debug.Log("++++++++++++START OF MESHCOMBINER++++++++++++");
        List<CombineInstance> combine = new List<CombineInstance>();

        // save the parent pos+rot
        Vector3 position = parent.transform.position;
        Quaternion rotation = parent.transform.rotation;

        // move to the origin for combining
        parent.transform.position = Vector3.zero;
        parent.transform.rotation = Quaternion.identity;

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

        MeshRenderer parent_mr = parent.gameObject.GetComponent<MeshRenderer>();
        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[meshList.Length];

        for (int i = 0; i < meshList.Length; i++)
        {
            materials[i] = meshList[0].GetComponent<Renderer>().material;
        }

        parent_mr.materials = materials;
        
         // Add mesh fileter if doesn't exist
        parent_mf = parent.gameObject.GetComponent<MeshFilter>();
        if (!parent_mf)
        {
            parent_mf = parent.gameObject.AddComponent<MeshFilter>();
        }

        // Destroy old mesh and combine new mesh
        Mesh oldmesh = parent_mf.sharedMesh;
        DestroyImmediate(oldmesh);
        parent_mf.mesh = new Mesh();
        Debug.Log($"COMBINE INSTANCE IS: {combine.ToArray().Length}");
        parent_mf.mesh.CombineMeshes(combine.ToArray(), true, true);

        // restore the parent pos+rot
        parent.transform.position = position;
        parent.transform.rotation = rotation;

        // Create a mesh collider if doesn't exist
        MeshCollider parent_mc = parent.gameObject.GetComponent<MeshCollider>(); // create parent_mc mesh collider 
        if (!parent_mc) 
        {
            parent_mc = parent.gameObject.AddComponent<MeshCollider>();
            parent_mc.material.bounciness = 0f;
            parent_mc.material.dynamicFriction = 1f;
            parent_mc.material.staticFriction = 1f;
        }
        parent_mc.convex = true;
        parent_mc.sharedMesh = parent_mf.mesh; // add the mesh shape (from the parent mesh) to the mesh collider

        Debug.Log("+++++++++++END OF MESH COMBINER+++++++++++++");
    }
}