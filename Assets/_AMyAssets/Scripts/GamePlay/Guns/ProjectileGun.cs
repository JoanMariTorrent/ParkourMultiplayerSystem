using PurrNet;
using UnityEngine;

public class ProjectileGun : Gun
{
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private LayerMask aimLayerMask;
    protected override void ExecuteShootingLogic(Vector3 cameraPosition, Vector3 cameraForward, double tick)
    {
        if (!isServer) return;

        // 1. CALCULAR EL PUNTO OBJETIVO 
        Vector3 targetPoint;
        
        // Lanzamos un rayo desde la camara
        if (Physics.Raycast(cameraPosition, cameraForward, out RaycastHit hit, 500f, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            // Si miramos al cielo y no chocamos con nada, inventamos un punto muy lejos
            targetPoint = cameraPosition + (cameraForward * 500f);
        }

        // 2. CALCULAR LA DIRECCIÓN REAL DE LA BALA
        Vector3 directionToTarget = (targetPoint - shootTransform.position).normalized;

        // 3. INSTANCIAR LA BALA (cambiar a object pooling en un futuro)
        GameObject bulletObj = Instantiate(projectilePrefab, shootTransform.position, Quaternion.LookRotation(directionToTarget));
        
        BallisticProjectile bulletScript = bulletObj.GetComponent<BallisticProjectile>();
        
        if (bulletScript != null)
        {
            // Pasamos owner.Value
            bulletScript.Initialize(_gunDamage, owner.Value, directionToTarget);
        }
    }
}