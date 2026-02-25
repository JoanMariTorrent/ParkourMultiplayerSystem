using System.Collections;
using PurrNet;
using PurrNet.Packing;
using UnityEngine;

public class U_ImpulseGrenade : GeneralGrenade
{
    [Header("Settings")]
    [SerializeField] private float explosionRadius = 7f;
    [SerializeField] private float impulseForce = 35f;
    [SerializeField] private float upModifier = 1.5f;
    [SerializeField] private float explosionDelay = 2.5f;

    [Header("References")]
    [SerializeField] private GameObject mesh;
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private LayerMask surfaceLayer;

    private ParticleSystem impulseInstance;
    private bool hasHit;

    public override void OnThrowed()
    {
        StartCoroutine(StartCooldown());
    }

    private IEnumerator StartCooldown()
    {
        yield return new WaitForSecondsRealtime(explosionDelay);
        Explode();
    }

    [ObserversRpc]
    private void Explode()
    {
        if(explosionVFX) impulseInstance = Instantiate(explosionVFX, transform.position, Quaternion.identity);
        if(mesh) mesh.SetActive(false);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        if(colliders.Length < 1) return;
        foreach(var hit in colliders)
        {
            PlayerCharacter pl = hit.GetComponent<PlayerCharacter>();
            if(pl != null)
            {
                Vector3 direction = (hit.transform.position - transform.position).normalized;

                direction.y += upModifier;
                direction = direction.normalized;
                
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float distanceRatio = Mathf.Clamp01(distance / explosionRadius);
                float forceMultiplier = Mathf.Lerp(1f, 0.5f, distanceRatio);
                float finalForce = impulseForce * forceMultiplier;

                pl.AddExplosionForce(direction * finalForce);
            }

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if(rb != null && !hit.GetComponent<PlayerCharacter>())
            {
                rb.AddExplosionForce(impulseForce * 10f, transform.position, explosionRadius);
            }

            DisablePhysics();
            Destroy(gameObject, 0.5f);
        }

    }


    // Physics
    void OnCollisionEnter(Collision collision)
    {
        if((((1 << collision.gameObject.layer)) & surfaceLayer) != 0)
        {
            if(hasHit) return;
        
            if(isServer)
            {
                hasHit = true;

                StopProjectile(transform.position);

                RpcStopProjectile(transform.position);
            }
        }

        
    }


    [ObserversRpc]
    private void RpcStopProjectile(Vector3 hitPosition)
    {
        if(isServer) return;
        StopProjectile(hitPosition);
    }

    private void StopProjectile(Vector3 hitPosition)
    {
        hasHit = true;

        transform.position = hitPosition;

        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if(col != null) col.enabled = false;
    }
}
