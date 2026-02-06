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

    [Header("Visuals")]
    [SerializeField] private BulletTracer tracerPrefab;

    protected override void ExecuteShootingLogic(Vector3 position, Vector3 direction, double tick)
    {
        Debug.DrawRay(position, direction * range, Color.green, 2f);

        VerifyHitScanServerRpc(tick, position, direction);
    }

    // --- 2. SERVIDOR  ---
    //[ServerRpc(requireOwnership: false)]
    private void VerifyHitScanServerRpc(double tick, Vector3 position, Vector3 direction)
    {
        Ray ray = new Ray(position, direction);
        RaycastHit hit;
        bool hasHit = false;

        if (rollbackModule != null)
        {
            hasHit = rollbackModule.Raycast(tick, ray, out hit, range, _hitLayer);
        }
        else
        {
            hasHit = Physics.Raycast(position, direction, out hit, range, _hitLayer);
        }

        if (hasHit)
        {
            HandleHitServer(hit);
        }

        else
        {
            Vector3 endPoint = position + (direction * range);
            SpawnHitEffectObserversRpc(false, endPoint, Vector3.zero, null, false);
        }
    }

    private void HandleHitServer(RaycastHit hit)
    {
        if (playerCharacter != null && hit.collider.gameObject == playerCharacter.gameObject) return;

        if (hit.transform.TryGetComponent(out PlayerHealth victim))
        {
            if (victim.owner.Value == this.owner.Value) return;

            victim.ChangeHealth(-_gunDamage, owner.Value);
            
            if (InstanceHandler.TryGetInstance(out ScoreManager sm)) 
                sm.AddDamageServerRpc(victim.PlayerID, owner.Value, _gunDamage);

            if(hitMarker != null)
                AudioManager.Instance.PlaySound2D(hitMarker, AudioType.SFX, 0.22f, Random.Range(minPitch, maxPitch));


            bool playerdeath = victim.health <= 0 ? true : false;
            SpawnHitEffectObserversRpc(true, hit.point, hit.normal, victim.transform, true, playerdeath);
        }
        else if (hit.transform.TryGetComponent(out HealthObject objVictim))
        {
            objVictim.ChangeHealth(-_gunDamage, hit.point);

            bool objDeath = objVictim.healthRef <= 0 ? true : false;
            SpawnHitEffectObserversRpc(true, hit.point, hit.normal, objVictim.transform, true, objDeath);

            if(hitMarker != null)
                AudioManager.Instance.PlaySound2D(hitMarker, AudioType.SFX, 0.22f, Random.Range(minPitch, maxPitch));
        }
        else
        {
            SpawnHitEffectObserversRpc(false, hit.point, hit.normal, null, true);
        }
    }

    // --- EFECTOS VISUALES ---
    [ObserversRpc]
    private void SpawnHitEffectObserversRpc(bool isPlayer, Vector3 pos, Vector3 normal, Transform parent, bool hit, bool lastHit = false)
    {

        SpawnTracer(pos);

        if(!hit) return;

        if (tracerPrefab != null && shootTransform != null)
        {
            var tracerObj = Instantiate(tracerPrefab, shootTransform.position, Quaternion.identity);
            
            tracerObj.Init(shootTransform.position, pos);
        }

        if(isPlayer)
            HitMarker(lastHit);

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