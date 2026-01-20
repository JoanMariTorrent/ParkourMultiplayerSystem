using PurrNet;
using UnityEngine;

public class HitscanGun : Gun
{
    [Header("Hitscan Settings")]
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private ParticleSystem _enviormentHit; 
    [SerializeField] private ParticleSystem _playerHitEffect; 

    protected override void ExecuteShootingLogic(Vector3 position, Vector3 direction)
    {
        Debug.DrawRay(position, direction * range, Color.red, 2f);

        RaycastHit[] hits = Physics.RaycastAll(position, direction, range, _hitLayer);

        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            if (hit.transform.TryGetComponent(out PlayerHealth victim))
            {
                if (victim.owner.Value == this.owner.Value)
                {
                    continue; 
                }

                ApplyDamageServerRpc(victim, _gunDamage);
                SpawnHitEffectObserversRpc(true, hit.point, hit.normal, victim.transform);
                return; 
            }

            else if (hit.transform.TryGetComponent(out HealthObject objVictim))
            {
                ApplyDamageObjectServerRpc(objVictim, _gunDamage);
                SpawnHitEffectObserversRpc(false, hit.point, hit.normal, null);
                return;
            }

            else
            {
                SpawnHitEffectObserversRpc(false, hit.point, hit.normal, null);
                return;
            }
        }
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(PlayerHealth victim, int dmg)
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

    [ObserversRpc]
    private void SpawnHitEffectObserversRpc(bool isPlayer, Vector3 pos, Vector3 normal, Transform parent)
    {
        if (isPlayer && _playerHitEffect && parent)
        {
            Instantiate(_playerHitEffect, parent.TransformPoint(parent.InverseTransformPoint(pos)), Quaternion.LookRotation(normal));
        }
        else if (!isPlayer && _enviormentHit)
        {
            Instantiate(_enviormentHit, pos, Quaternion.LookRotation(normal));
        }
    }
}