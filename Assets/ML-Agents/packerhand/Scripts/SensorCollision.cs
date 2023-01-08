using UnityEngine;

// SensorCollision component to work requires:
// - Collider component (needed for a Collision)
// - Rigidbody component (needed for a Collision)
//   - "the Rigidbody can be set to be 'kinematic' if you don't want the object to have physical interaction with other objects"
// + usecase: SensorCollision component can attached to bin to detect box collisions with bin
public class SensorCollision : MonoBehaviour
{
  public Collider c; // note: don't need to drag and drop in inspector, will instantiate on line 17: c = GetComponent<Collider>();
  // public Rigidbody rb;

  void Start(){
    // instantiate the Collider component
    c = GetComponent<Collider>(); // note: right now using the generic Collider class so anyone can experiment with mesh collisions on all objects like: BoxCollider, SphereCollider, etc.
    // note: can get MeshCollider component from generic Collider component (MeshCollider inherits from Collider base class)
    // instantiate the MeshCollider component from Collider component
    // MeshCollider mc = c.GetComponent<MeshCollider>();

  }
  void Update(){

  }

  void OnCollisionEnter(Collision collision){
    // if collision is with anything other than the ground 
    // - note: future optimization via turn off collisions with ground for optimization? turn off by turning on kinematics for ground?
    if (collision.gameObject.name != "Ground"){
      Debug.Log($"*@@@@@ Collision of {c.gameObject.name} and {collision.gameObject.name} @@@@@");
    }
  }
}