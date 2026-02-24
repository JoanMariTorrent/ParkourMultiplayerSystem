using UnityEngine;
using PurrNet;

public class ThrowableUtility : Utility
{
    [Header("Throwable settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upForce = 2f;
    [SerializeField] private float torqueAmount = 10f;

    
    [Space(15)][Header("Grenade")]
    [SerializeField] private GeneralGrenade greandeToThrow; 


    protected override void ExecuteUtilityLogic(Vector3 position, Vector3 direction)
    {
        if(isInCooldown) return;
        if (greandeToThrow.projectilePrefab == null) return;
        

        Vector3 spawnPos = position + (direction * 0.8f);

        GameObject projectile = Instantiate(greandeToThrow.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            projRb.isKinematic = false;
            projRb.useGravity = true;

            Vector3 force = (direction * throwForce) + (Vector3.up * upForce);
            projRb.AddForce(force, ForceMode.Impulse);
            projRb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);
        }

        if(projectile != null)
        {
            projectile.SetActive(true);

            GeneralGrenade grenadeScript = projectile.GetComponent<GeneralGrenade>();
            if(grenadeScript != null)
            {
                grenadeScript.OnThrowed();
            }
        }
        

        SetVisualsActive(false); 
    }

    private void SetVisualsActive(bool active)
    {
        if(childMeshes != null)
        {
            foreach(var mesh in childMeshes)
            {
                if(mesh) mesh.SetActive(active);
            }
        }

        Renderer r = GetComponent<Renderer>();
        if(r) r.enabled = active;
    }
}