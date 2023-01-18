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
    private int num_boxes;
    private int inum;
    private GameObject cube1;
    private Rigidbody rb;

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

    // void Delay(){Debug.Log("delay.....");}

    void Awake(){
        // interval = 3.0f;
        spawnPos = new Vector3(10,0.5f,0);
        movePos = new Vector3(1,0,0);
        num_boxes = 5;
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

    void spawnAndMove(){
        for (inum = 1; inum < num_boxes; inum++){
            Invoke("Delay",1f);
            cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube1.transform.position = spawnPos;
            cube1.name = "cube" + inum.ToString();
            cube1.AddComponent<Rigidbody>();
            rb = cube1.GetComponent<Rigidbody>();
            rb.MovePosition(spawnPos + movePos * (float)inum);
        }
    }
    
    void FixedUpdate(){
        spawnAndMove();
    }

    void Update(){
        nv1.BuildNavMesh();
    }
}