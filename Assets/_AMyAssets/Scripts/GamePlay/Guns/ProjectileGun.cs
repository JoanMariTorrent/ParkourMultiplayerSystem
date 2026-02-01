using PurrNet;
using UnityEngine;

public class ProjectileGun : Gun
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private LayerMask aimLayerMask;
    protected override void ExecuteShootingLogic(Vector3 cameraPosition, Vector3 cameraForward, double tick)
    {
        // Solo el dueño dispara la bala real
        if (!isOwner) return;

        Vector3 targetPoint;
        if (Physics.Raycast(cameraPosition, cameraForward, out RaycastHit hit, 500f, aimLayerMask))
            targetPoint = hit.point;
        else
            targetPoint = cameraPosition + (cameraForward * 500f);

        Vector3 directionToTarget = (targetPoint - shootTransform.position).normalized;

        // Instanciar localmente
        GameObject bulletObj = Instantiate(projectilePrefab, shootTransform.position, Quaternion.LookRotation(directionToTarget));
        
        BallisticProjectile bulletScript = bulletObj.GetComponent<BallisticProjectile>();

        Collider ownCollider = GetComponent<Collider>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(_gunDamage, directionToTarget, this, ownCollider);
        }
    }

    // --- DAÑO ---
    
    public void ReportPlayerHit(PlayerHealth victim, int dmg)
    {
        ApplyDamageServerRpc(victim, dmg);
    }

    public void ReportObjectHit(HealthObject obj, int dmg, Vector3 point)
    {
        ApplyDamageObjectServerRpc(obj, dmg, point);
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(PlayerHealth victim, int dmg)
    {
        victim.ChangeHealth(-dmg, owner.Value);
        
        if (InstanceHandler.TryGetInstance(out ScoreManager sm)) 
            sm.AddDamageServerRpc(victim.PlayerID, owner.Value, dmg);
    }

    [ServerRpc]
    private void ApplyDamageObjectServerRpc(HealthObject obj, int dmg, Vector3 point)
    {
        obj.ChangeHealth(-dmg, point);
    }
}