using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupScript2 : MonoBehaviour
{

    public bool isHeld = false; //whether the object is held by the agent or not
    public bool isOrganized  = false; //whether the box is stacked or not

    public void Pickup(Transform target) {
        //packer picks up target box not in bin, a small reward is added
        Debug.Log("AGENT ABOUT TO PICK UP BOX!!!!!!!!!!!!!!");
        PickupScript2 pickupScript = target.GetComponent<PickupScript2>();
        if (pickupScript!=null && !pickupScript.isHeld && !pickupScript.isOrganized) {
            pickupScript.isHeld = true;
            //NEEDS TO MAKE THE BOX DOESN'T TOUCH THE GROUND WHEN IT'S CARRIED SINCE COLLISION WITH GROUND IN BIN IS REWARDED 
            target.SetParent(GameObject.Find("agent").transform);
            target.gameObject.GetComponent<Rigidbody>().useGravity = false;

        }  

    }

    //WORK TO DO: CHECK THE PHYSICS AND CONTRAINTS WHEN STACKING BOXES, SET ROTATION OF BOX, ETC.
    public void DropoffBox(Transform carriedObject) {
        //drop off the box, when the box touches the bin area, reward is added
        if (carriedObject!=null) {
            PickupScript pickupScript = carriedObject.GetComponent<PickupScript>();
            pickupScript.isHeld = false;
            pickupScript.isOrganized = true;
            ///need to reset the parent, chance below////////
            carriedObject.position = this.transform.position + this.transform.forward * 0.5f; 
        }
    }
}