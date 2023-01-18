using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NavMeshTest : MonoBehaviour
{
    // Start is called before the first frame update

    private float range;
    private Vector3 movePos;
    private Vector3 spawnPos;
    private NavMeshSurface nv1;
    private Vector3 point;
    private GameObject objectToMove;
    private GameObject cube1;
    private Rigidbody rb;
    private bool spawnModifier;

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

    IEnumerator Delay(){
        yield return new WaitForSeconds(3);
        spawnModifier = true;
    }

    void Awake(){
        spawnModifier = true;
        spawnPos = new Vector3(10,0.5f,0);
        movePos = new Vector3(20,0,0);
        objectToMove = GameObject.Find("Cube");
        nv1 = GameObject.Find("NavMesh Surface XX").GetComponent<NavMeshSurface>();
    }

    void Start(){
        Rigidbody rb = objectToMove.GetComponent<Rigidbody>();
        rb.MovePosition(objectToMove.transform.position + movePos);
        // if (RandomPoint(objectToMove.transform.position, range, out point))
        // {
        //     Debug.DrawRay(point, Vector3.up, Color.blue, 5.0f);
        //     Debug.Log($" random point === {point}");
        // }
    }

    void Spawn(){
        cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.transform.position = spawnPos;
        cube1.name = "cube1";
        cube1.AddComponent<Rigidbody>();
        // rb is the box is getting moved
        rb = cube1.GetComponent<Rigidbody>();
    }
    void Move(){   
        Vector3 targetPosition = movePos + new Vector3(0,0,1);
        var dx = targetPosition - cube1.transform.position;
        rb.MovePosition(cube1.transform.position + dx/100);
        dx = targetPosition - cube1.transform.position;
        if ( dx.x < 2f ){
            spawnModifier = true;
        };

    }
    
    void FixedUpdate(){
        if (spawnModifier == true){ 
            Spawn();
            spawnModifier = false;
        }
        if (spawnModifier == false){ 
            Move();
        }
    }

    void LateUpdate(){
        nv1.BuildNavMesh();
    }
}