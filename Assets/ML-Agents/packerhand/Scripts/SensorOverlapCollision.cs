using UnityEngine;
using System.Collections.Generic;
using Box = Boxes.Box;

// Checks for overlap
public class SensorOverlapCollision : MonoBehaviour
{
    public bool passedOverlapCheck = true;
    private RaycastHit hit;


    void Start()
    {

    }


    void Update()
    {

    }
    

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"CBO {collision.gameObject.name}               |          overlap with side: {name} ");

        int layerMask = 1 << 6;

        if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.left), out hit, (transform.localScale.x * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        {
            passedOverlapCheck = false;
           Debug.Log($"CBOx {name} FAILED OVERLAP TEST LEFT colliding with {collision.gameObject.name} ");
        }
        if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.right), out hit, (transform.localScale.x * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        {
            passedOverlapCheck = false;
            Debug.Log($"CBOx {name} FAILED OVERLAP TEST RIGHT colliding with {collision.gameObject.name} ");
        }

        if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.up), out hit, (transform.localScale.y * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        {
            passedOverlapCheck = false;
            Debug.Log($"CBOx {name} FAILED OVERLAP TEST UP colliding with {collision.gameObject.name} ");
        }
        if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.down), out hit, (transform.localScale.y * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        {
            passedOverlapCheck = false;
            Debug.Log($"CBOx {name} FAILED OVERLAP TEST DOWN colliding with {collision.gameObject.name} ");
        }

        if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.forward), out hit, (transform.localScale.z * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        {
            passedOverlapCheck = false;
            Debug.Log($"CBOx {name} FAILED OVERLAP TEST FORWARD colliding with {collision.gameObject.name} ");
        }
        // if (Physics.Raycast( transform.position, transform.TransformDirection(Vector3.back), out hit, (transform.localScale.z * 0.5f) - 0.1f, layerMask, QueryTriggerInteraction.Collide )) 
        // {
        //     passedOverlapCheck = false;
        //     Debug.Log($"CBOx {name} FAILED OVERLAP TEST BACK colliding with {collision.gameObject.name} ");
        // }

        

        // // if a testbox overlaps perpendicularly with a trimesh side then reset the box (impossible placement)
        // if (collision.gameObject.tag == "outerbin") {
        // if (collision.gameObject.tag == "SIDENSAFKJSDNFK") {
        //     passedOverlapCheck = false;
        //     Debug.Log($"CBO {collision.gameObject.name} box reset due to collision with outer bin side: {name} ");
        // } 

        // var checkmyBox = Physics.CheckBox(transform.position, new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z));

        // var wuttPlus = new Vector3( (middle_position + (bounds - tinysliver)), (transform.position.y + ((transform.localScale.y * 0.5f) - 0.02f)), (transform.position.z + ((transform.localScale.z * 0.5f) - 0.02f)) )
        
        
        
        
        // var wuttPlus = new Vector3( (transform.position.x + ((transform.localScale.x * 0.5f) - 0.02f)), (transform.position.y + ((transform.localScale.y * 0.5f) - 0.02f)), (transform.position.z + ((transform.localScale.z * 0.5f) - 0.02f)) )
        // var wuttMinus = new Vector3( (transform.position.x - ((transform.localScale.x * 0.5f) - 0.02f)), (transform.position.y - ((transform.localScale.y * 0.5f) - 0.02f)), (transform.position.z - ((transform.localScale.z * 0.5f) - 0.02f)) )

        // bounds

        // // if true there is an overlap and therefor set passed overlap check false
        // // // if (Physics.CheckBox(transform.position, new Vector3(((transform.localScale.x * 0.5f) - 0.02f), ((transform.localScale.y * 0.5f) - 0.02f), ((transform.localScale.z * 0.5f) - 0.02f)))) 
        // if (
        //     // GetComponent<BoxCollider>().CheckBox(transform.position, new Vector3(((transform.localScale.x * 0.5f) - 0.02f), ((transform.localScale.y * 0.5f) - 0.02f), ((transform.localScale.z * 0.5f) - 0.02f)))

        // ) 
        // {
        //     passedOverlapCheck = false;
        //     Debug.Log($"CBOx {collision.gameObject.name} box reset due to collision with outer bin side: {name} ");
        // } 
    }
    
     
}
