using System.Collections;
using PurrNet;
using UnityEngine;

public class U_FragGrenade : GeneralGrenade
{
    [Header("Settings")]
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float explosionDelay = 2f;
    [SerializeField] private float damage = 75f;
    [SerializeField] private float fullDamageRadius = 3f;

    [Header("Referemces")]
    [SerializeField] private GameObject mesh;
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private LayerMask surfaceLayer;

    private ParticleSystem fragInstance;
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
        if(explosionVFX) fragInstance = Instantiate(explosionVFX, transform.position, Quaternion.identity);
        if(mesh) mesh.SetActive(false);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        if(colliders.Length < 1) return;

        foreach(var hit in colliders)
        {
            PlayerCharacter pl = hit.GetComponent<PlayerCharacter>();
            if(pl == null) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            float distanceRatio;

            if(distance <= fullDamageRadius)
            {
                distanceRatio = 1f;
            }
            else
            {
                float adjustedDistance = (distance - fullDamageRadius) / (explosionRadius - fullDamageRadius);
                distanceRatio = 1f - Mathf.Clamp01(adjustedDistance);
            }

            
            
            float totalDamage = damage * distanceRatio;
            int realDamage = (int)totalDamage;

            pl.GetComponent<PlayerHealth>()?.ChangeHealth(-realDamage, owner.Value);
        }

        DisablePhysics();
        Destroy(gameObject, 0.5f);
    }
}
