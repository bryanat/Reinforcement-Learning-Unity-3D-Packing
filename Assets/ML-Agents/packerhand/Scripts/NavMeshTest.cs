using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NavMeshTest : MonoBehaviour
{
    // Start is called before the first frame update

    private float range=2.0f;
    private Vector3 movePos;
    private int cC;
    private bool updateNM;
    private NavMeshSurface nv1;
    private Vector3 point;
    private GameObject objectToMove;

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 3; i++)
        {
            range = range + 2.0f;
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    void Awake(){
        movePos = new Vector3(10,0.5f,0);
        cC = 1;
        updateNM = false;
        objectToMove = GameObject.Find("Cube");
        nv1 = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshSurface>();
    }

    void Start(){
        Rigidbody rb = objectToMove.GetComponent<Rigidbody>();
        Vector3 targetPosition = objectToMove.transform.position;
        targetPosition = targetPosition + movePos;
        rb.MovePosition(targetPosition);
        updateNM = true;
        // Sample next random point & cast ray to display it
        if (RandomPoint(objectToMove.transform.position, range, out point))
        {
            Debug.DrawRay(point, Vector3.up, Color.blue, 5.0f);
            Debug.Log($" random point === {point}");
        }
        GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.AddComponent<Rigidbody>();
        cube1.transform.position = movePos;
        cube1.name = "cube" + cC.ToString();
    }

    void FixedUpdate(){
        if ( updateNM == true ){
            Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%% FIXED UPDATE %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
            //  Remove & Rebuild the NavMesh
            NavMesh.RemoveAllNavMeshData();
            nv1.BuildNavMesh();
            updateNM = false;
        }
    }
}