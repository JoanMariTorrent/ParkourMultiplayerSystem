using PurrNet;
using UnityEngine;

public class GeneralGrenade : NetworkBehaviour
{
    public GameObject projectilePrefab;
    public Rigidbody rb;
    public virtual void OnThrowed() { }
    
    
    public void DisablePhysics()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
