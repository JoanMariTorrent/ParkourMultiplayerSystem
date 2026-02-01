using PurrNet;
using PurrNet.Modules;
using UnityEngine;

public class HitscanGun : Gun
{
    [Header("Hitscan Settings")]
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private ParticleSystem _enviormentHit; 
    [SerializeField] private ParticleSystem _playerHitEffect; 

    protected override void ExecuteShootingLogic(Vector3 position, Vector3 direction, double tick)
    {
        Debug.DrawRay(position, direction * range, Color.green, 2f);

        VerifyHitScanServerRpc(tick, position, direction);
    }

    // --- 2. SERVIDOR  ---
    //[ServerRpc(requireOwnership: false)]
    private void VerifyHitScanServerRpc(double tick, Vector3 position, Vector3 direction)
    {
        // Seguridad: Si no hay módulo, usamos física normal del server
        if (rollbackModule == null)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hitNormal, range, _hitLayer))
                HandleHitServer(hitNormal);
            return;
        }

        Ray ray = new Ray(position, direction);
        RaycastHit hit;

        // ROLLBACK: El servidor rebobina al 'tick' que envió el cliente
        if (rollbackModule.Raycast(tick, ray, out hit, range, _hitLayer))
        {
            HandleHitServer(hit);
        }
    }

    private void HandleHitServer(RaycastHit hit)
    {
        // 1. Evitar dispararse a uno mismo
        if (playerCharacter != null && hit.collider.gameObject == playerCharacter.gameObject) return;

        // A. JUGADOR
        if (hit.transform.TryGetComponent(out PlayerHealth victim))
        {
            if (victim.owner.Value == this.owner.Value) return;

            victim.ChangeHealth(-_gunDamage, owner.Value);
            
            if (InstanceHandler.TryGetInstance(out ScoreManager sm)) 
                sm.AddDamageServerRpc(victim.PlayerID, owner.Value, _gunDamage);

            SpawnHitEffectObserversRpc(true, hit.point, hit.normal, victim.transform);
        }
        // B. OBJETO
        else if (hit.transform.TryGetComponent(out HealthObject objVictim))
        {
            objVictim.ChangeHealth(-_gunDamage, hit.point);
            SpawnHitEffectObserversRpc(true, hit.point, hit.normal, objVictim.transform);
        }
        // C. ENTORNO
        else
        {
            SpawnHitEffectObserversRpc(false, hit.point, hit.normal, null);
        }
    }

    // --- EFECTOS VISUALES ---
    [ObserversRpc]
    private void SpawnHitEffectObserversRpc(bool isPlayer, Vector3 pos, Vector3 normal, Transform parent)
    {
        if (isPlayer && _playerHitEffect && parent)
        {
            var effect = Instantiate(_playerHitEffect, pos, Quaternion.LookRotation(normal));
            effect.transform.SetParent(parent);
        }
        else if (!isPlayer && _enviormentHit)
        {
            Instantiate(_enviormentHit, pos, Quaternion.LookRotation(normal));
        }
    }
}