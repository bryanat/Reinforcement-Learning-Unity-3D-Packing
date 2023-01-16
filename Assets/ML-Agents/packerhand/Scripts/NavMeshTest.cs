using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NavMeshTest : MonoBehaviour
{
    public NavMeshSurface nv1;

    // Start is called before the first frame update
    void Start()
    {
        GameObject objectToMove = GameObject.Find("Cube");
        Debug.Log($"%%%%%%%%%%%%%%%%% object to move name  ===== {objectToMove.name}");
        // Debug.Log($"%%%%%%%%%%%%%%%%% agenttype ===== {nv1.agentTypeID}");
        Rigidbody  rb = objectToMove.GetComponent<Rigidbody>();
        Vector3 targetPosition = objectToMove.transform.position;
        Debug.Log($"old box position =========== {objectToMove.transform.position}");
        targetPosition = targetPosition + new Vector3(10,0,0);
        Debug.Log($"target position =========== {targetPosition}");
        rb.MovePosition(targetPosition);
        Debug.Log($"new box position =========== {objectToMove.transform.position}");
        Debug.Log(".......................................................................... REMOVED all NavMesh data");

        Debug.Log($" NavMeshBuildSourceShape === {NavMeshBuildSourceShape.Box}");
        Debug.Log($" NavMeshBuildSourceShape === {NavMeshBuildSourceShape.Capsule}");

        // NavMeshSurface nv1 = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshSurface>();
        // nv1.BuildNavMesh();
        // // Sample next random point & cast ray to display it
        // float range = 2.0f;
        // Vector3 point;
        // if (RandomPoint(objectToMove.transform.position, range, out point))
        // {
        //     Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
        // }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 3; i++)
        {
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

    void Update(){
        //  Remove & Rebuild the NavMesh
        NavMesh.RemoveAllNavMeshData();
        nv1 = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshSurface>();
        nv1.BuildNavMesh();
        // Sample next random point & cast ray to display it
        float range = 2.0f;
        Vector3 point;
        GameObject objectToMove = GameObject.Find("Cube");
        if (RandomPoint(objectToMove.transform.position, range, out point))
        {
            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
        }
    }

}
