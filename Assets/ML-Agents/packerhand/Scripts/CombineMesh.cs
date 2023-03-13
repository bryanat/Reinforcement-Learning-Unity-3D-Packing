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
    public bool isBoxPlaced = false;

    public Transform oppositeSideObject;
    public Transform sameSideObject;

    public GameObject binBottom;
    public GameObject binSide;
    public GameObject binBack;

    [HideInInspector] CombineMesh m_BackMeshScript; // cache combine mesh script
    [HideInInspector] CombineMesh m_SideMeshScript; // cache combine mesh script
    [HideInInspector] CombineMesh m_BottomMeshScript; // cache combine mesh script
    [HideInInspector] MeshRenderer parent_mr;
    [HideInInspector] MeshFilter parent_mf;
    [HideInInspector] MeshCollider parent_mc;


    public Material clearPlastic;

    void Start() 
    {
        m_BackMeshScript = binBack.GetComponent<CombineMesh>();
        m_SideMeshScript = binSide.GetComponent<CombineMesh>();
        m_BottomMeshScript = binBottom.GetComponent<CombineMesh>();
        parent_mr = gameObject.GetComponent<MeshRenderer>();
        parent_mf = gameObject.GetComponent<MeshFilter>();
        parent_mc = gameObject.GetComponent<MeshCollider>();

    }


    void OnCollisionEnter(Collision collision) 
    { 
        // CombineMesh.cs : deals with child sides (left, top, bottom) : collision (physics)
        // Packerhand.cs  : deals parent box : position (math)
        
        // BLUE
        //Debug.Log($"{name} RRR blue {isCollidedBlue == false}, {name == "BinIso20Back"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedBlue: {isCollidedBlue == false} name == BinIso20Back: {name == "BinIso20Back"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Back Blue mesh and a box collides with it then set isCollidedBlue collision property to true
        if (isBoxPlaced == false && name == "BinIso20Back" && collision.gameObject.tag == "pickupbox" && collision.gameObject.name == "back")
        {
            // set mesh property isCollidedBlue to true, used when all three colors are true then combinemeshes
            isBoxPlaced = true;
        }

        // GREEN
        //Debug.Log($"{name} RRR green {isCollidedGreen == false}, {name == "BinIso20Bottom"}, {collision.gameObject.tag == "pickupbox"}, {collision.gameObject.name == "bottom"} | collision side:{collision.gameObject.name} AAA isCollidedGreen: {isCollidedGreen == false} name == BinIso20Bottom: {name == "BinIso20Bottom"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Bottom Green mesh and a box collides with it then set isCollidedGreen collision property to true
         if (isBoxPlaced == false && name == "BinIso20Bottom" && collision.gameObject.tag == "pickupbox" && collision.gameObject.name == "bottom")
        {
            // set mesh property isCollidedGreen to true, used when all three colors are true then combinemeshes
            isBoxPlaced = true;
        }

        // RED
        //Debug.Log($"{name} RRR red {isCollidedRed == false}, {name == "BinIso20Side"}, {collision.gameObject.tag == "pickupbox"} | collision side:{collision.gameObject.name} AAA isCollidedRed: {isCollidedRed == false} name == BinIso20Side: {name == "BinIso20Side"} collision.gameObject.tag == pickupbox: {collision.gameObject.tag == "pickupbox"}");
        // if this mesh is Side Red mesh and a box collides with it then set isCollidedRed collision property to true
        if (isBoxPlaced == false && name == "BinIso20Side" && collision.gameObject.tag == "pickupbox" && (collision.gameObject.name=="left" | collision.gameObject.name=="right"))
        {
            // set mesh property isCollidedRed to true, used when all three colors are true then combinemeshes
            isBoxPlaced = true;

        }

        // if one of the three meshes have contact, then allow combining meshes 
        // only entered for the last one mesh 
        if (m_BottomMeshScript.isBoxPlaced | m_BackMeshScript.isBoxPlaced | m_SideMeshScript.isBoxPlaced)
        {
            // m_BottomMeshScript.isBoxPlaced = true;
            // m_BackMeshScript.isBoxPlaced = true;
            // m_SideMeshScript.isBoxPlaced = true;
        
            // BLUE
            if (agent.isBackMeshCombined==false) 
            {
                m_BackMeshScript.sameSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("back");
                m_BackMeshScript.oppositeSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("front");
                m_BackMeshScript.oppositeSideObject.parent = binBack.transform;
                m_BackMeshScript.sameSideObject.parent = binBack.transform;
                var blueMeshList = binBack.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(blueMeshList);
                //Debug.Log("MMM MESH COMBINED FOR BACK MESH");
                agent.isBackMeshCombined = true;
                // oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                // sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                m_BackMeshScript.oppositeSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
                m_BackMeshScript.sameSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
            }

            // GREEN
            if (agent.isBottomMeshCombined==false) 
            {
                m_BottomMeshScript.sameSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("bottom");
                m_BottomMeshScript.oppositeSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("top");      
                m_BottomMeshScript.oppositeSideObject.parent = binBottom.transform;
                m_BottomMeshScript.sameSideObject.parent = binBottom.transform;          
                var greenMeshList = binBottom.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(greenMeshList);
                //Debug.Log("MMM MESH COMBINED FOR BOTTOM MESH");
                agent.isBottomMeshCombined = true;  
                // oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                // sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;  
                m_BottomMeshScript.oppositeSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
                m_BottomMeshScript.sameSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
            }

            // RED
            if (agent.isSideMeshCombined==false) 
            {
                m_SideMeshScript.sameSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("left");
                m_SideMeshScript.oppositeSideObject = agent.boxPool[agent.selectedBoxIdx].rb.transform.Find("right");
                m_SideMeshScript.oppositeSideObject.parent = binSide.transform;
                m_SideMeshScript.sameSideObject.parent = binSide.transform;
                var redMeshList = binSide.GetComponentsInChildren<MeshFilter>(); 
                MeshCombiner(redMeshList);
                //Debug.Log("MMM MESH COMBINED FOR SIDE MESH");
                agent.isSideMeshCombined = true;
                // oppositeSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                // sameSideObject.GetComponent<MeshRenderer>().material = clearPlastic;
                m_SideMeshScript.oppositeSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
                m_SideMeshScript.sameSideObject.GetComponent<MeshRenderer>().material.color = agent.boxPool[agent.selectedBoxIdx].boxColor;
            }
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
            // Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList);
        }
        if (name == "BinIso20Back")
        {
            while (binBack.transform.childCount > 1) 
            {
                DestroyImmediate(binBack.transform.GetChild(binBack.transform.childCount-1).gameObject);
            }  
            MeshFilter [] meshList = binBack.GetComponentsInChildren<MeshFilter>();
            // Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList);
        }
        if (name == "BinIso20Side")
        {
            while (binSide.transform.childCount > 2) 
            {
                DestroyImmediate(binSide.transform.GetChild(binSide.transform.childCount-1).gameObject);
            } 
            MeshFilter [] meshList = binSide.GetComponentsInChildren<MeshFilter>();
            // Debug.Log($"MMB meshList length: {meshList.Length}, NAME: {meshList[0].gameObject.name}");
            MeshCombiner(meshList);
        }

    }

    public void MeshCombiner(MeshFilter[] meshList) 
    {
        //Debug.Log($"++++++++++++START OF MESHCOMBINER++++++++++++ for {transform.parent.name}");
        List<CombineInstance> combine = new List<CombineInstance>();

        // save the parent pos+rot
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // move to the origin for combining
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        for (int i = 1; i < meshList.Length; i++)
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

        // Set the materials of the new mesh to the materials of the original meshes
        Material[] materials = new Material[1];

        // parent_mr.materials = materials;
        materials[0] = meshList[0].GetComponent<Renderer>().material;
        parent_mr.materials = materials;
        
         // Add mesh fileter if doesn't exist
        if (!parent_mf)
        {
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
        if (!parent_mc) 
        {
            parent_mc = gameObject.AddComponent<MeshCollider>();
            parent_mc.material.bounciness = 0f;
            parent_mc.material.dynamicFriction = 1f;
            parent_mc.material.staticFriction = 1f;
        }
        parent_mc.convex = true;
        parent_mc.sharedMesh = parent_mf.mesh; // add the mesh shape (from the parent mesh) to the mesh collider

        //Debug.Log("+++++++++++END OF MESH COMBINER+++++++++++++");
    }
}