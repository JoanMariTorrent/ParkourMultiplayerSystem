using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class FlameThrowerGun : Gun
{
    [Header("Flamethrower Settings")]
    [SerializeField] private ParticleSystem flameParticles;
    [SerializeField] private Collider damageTrigger;
    [SerializeField] private AudioSource loopAudioSource;

    // Variables privadas
    private HashSet<PlayerHealth> targetsInZone = new HashSet<PlayerHealth>();
    private HashSet<HealthObject> objectsInZone = new HashSet<HealthObject>();
    private float _lastShotTime;
    private bool _isFiringEffect = false;



    protected override void Start()
    {
        base.Start();

        if (damageTrigger) damageTrigger.isTrigger = true;
        if (flameParticles) flameParticles.Stop();
        if (loopAudioSource) loopAudioSource.Stop();
    }

    protected override void Update()
    {
        base.Update();

        if(Time.time - _lastShotTime > _fireRate * 0.15f)
        {
            if(_isFiringEffect)
            {
                StopFlameVisuals();
            }
        }
    }


    protected override void ExecuteShootingLogic(Vector3 position, Vector3 direction)
    {
        UpdateFlameStateObserversRpc(true);

        targetsInZone.RemoveWhere(x => x == null);
        objectsInZone.RemoveWhere(x => x == null);

        foreach (var victim in targetsInZone)
        {
            if (victim.owner.Value != this.owner.Value)
            {
                ApplyDamageOnServerRpc(victim, _gunDamage);
            }
        }

        foreach (var obj in objectsInZone)
        {
            ApplyDamageObjectServerRpc(obj, _gunDamage);
        }
    }

    // === Sistema de deteccion (Triggers) ---

    private void OnTriggerEnter(Collider other)
    {
        if(!isServer) return;

        if(other.TryGetComponent(out PlayerHealth health))
        {
            targetsInZone.Add(health);
        }

        else if(other.TryGetComponent(out HealthObject objHealth))
        {
            objectsInZone.Add(objHealth);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out PlayerHealth health))
        {
            targetsInZone.Remove(health);
        }
        else if (other.TryGetComponent(out HealthObject objHealth))
        {
            objectsInZone.Remove(objHealth);
        }
    }

    // --- RPCs de daño --- 

    [ServerRpc]
    private void ApplyDamageOnServerRpc(PlayerHealth victim, int dmg)
    {
        victim.ChangeHealth(-dmg, owner.Value);
        if (InstanceHandler.TryGetInstance(out ScoreManager sm)) 
            sm.AddDamageServerRpc(victim.PlayerID, owner.Value, dmg);
    }

    [ServerRpc]
    private void ApplyDamageObjectServerRpc(HealthObject obj, int dmg)
    {
        obj.ChangeHealth(-dmg, transform.position);
    }

    // --- VISUALES SINCRONIZADAS ---


    [ObserversRpc(runLocally: true)]
    private void UpdateFlameStateObserversRpc(bool isFiring)
    {
        _lastShotTime = Time.time;

        if(!_isFiringEffect && isFiring)
        {
            _isFiringEffect = true;
            if (flameParticles) flameParticles.Play();
            if (loopAudioSource) loopAudioSource.Play();
        }
    }

    [ObserversRpc(runLocally: true)]
    private void StopFlameVisuals()
    {
        _isFiringEffect = false;
        if (flameParticles) flameParticles.Stop();
        if (loopAudioSource) loopAudioSource.Stop();
    }
}
