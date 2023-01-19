using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NavMeshTest : MonoBehaviour
{
    // Start is called before the first frame update

    private float range;
    private Vector3 targetPosition;
    private Vector3 spawnPos;
    private NavMeshSurface nv1;
    private Vector3 point;
    private GameObject cubeX;
    private Rigidbody rb;
    private bool spawnModifier;
    // private NavMeshAgent agentX;

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
        spawnPos = new Vector3(10,0.5f,0);
        nv1 = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshSurface>();
        // agentX = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshAgent>();
    }

    void Start(){
        spawnModifier = true;
        targetPosition = new Vector3(20,0,0);
    //     //  Sample random point and draw ray 
    //     // if (RandomPoint(cubeX.transform.position, range, out point))
    //     // {
    //     //     Debug.DrawRay(point, Vector3.up, Color.blue, 5.0f);
    //     //     Debug.Log($" random point === {point}");
    //     // }
    }

    void SpawnAndMove(){
        if ( spawnModifier == true ){
            spawnModifier = false;
            // Spawn box
            Debug.Log("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%5   Spawn ");
            cubeX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeX.transform.position = spawnPos;
            cubeX.name = "cubeX";
            cubeX.AddComponent<Rigidbody>();
            rb = cubeX.GetComponent<Rigidbody>();

            // cubeX.AddComponent<NavMeshAgent>();
            // agentX.radius = 3.1f;
            // nv1.agentTypeID = agentX.agentTypeID;

            // NavMeshBuildSettings buildSettings = new NavMeshBuildSettings();

            // nv1.agentRadius = 5.f;         
            // NavMesh.CreateSettings();
            // Debug.Log($" agent type ============ {buildSettings.agentTypeID}");
        }
        if ( spawnModifier == false ){
            // Move box <rb>
            var dx = targetPosition - cubeX.transform.position;
            rb.MovePosition(cubeX.transform.position + dx / 100);
            dx = targetPosition - cubeX.transform.position;
            // Debug.Log($" %%%%%%%%%%%%%%% dx.x ======== {dx.x}");
            if ( dx.x < 2f ){
                spawnModifier = true;
                targetPosition = targetPosition + new Vector3(0,0,2);
            }
        }
    }
    
    void FixedUpdate(){
        SpawnAndMove();
    }

    void LateUpdate(){
        nv1.BuildNavMesh();
    }
}