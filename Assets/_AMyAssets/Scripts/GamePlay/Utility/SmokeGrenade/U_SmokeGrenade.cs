using System.Collections;
using UnityEngine;
using PurrNet;

public class U_SmokeGrenade : GeneralGrenade
{
    [Header("References")]
    public ParticleSystem smokeVFX;
    [SerializeField] private GameObject mesh;

    [Space][Header("Variables")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float VFXLifeTime = 3f;

    private ParticleSystem smokeInstance;



    public override void OnThrowed()
    {
        gameObject.SetActive(true); 
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
        smokeInstance = Instantiate(smokeVFX, transform.position, smokeVFX.gameObject.transform.rotation);
        mesh.SetActive(false);
        DisablePhysics();
        StartCoroutine(DelayToDesactiveVFX());
    }

    private IEnumerator DelayToDesactiveVFX()
    {
        yield return new WaitForSecondsRealtime(VFXLifeTime);

        smokeInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(smokeInstance.gameObject, smokeInstance.main.startLifetime.constantMax);
        Destroy(gameObject);
    }
}
