using Unity.Cinemachine;
using UnityEngine;

public class AimSystem : MonoBehaviour
{
    [Header("Variables")]

    [Space]
    public float normalFOV = 70f;
    public float normalGunFOV = 70f;

    [Header("Referencias")]
    public CinemachineCamera cameraPlayer;
    public Camera cameraGun;
    [Space]
    public PlayerCharacter playerCharacter;
    public PlayerCamera playerCamera;
    public WeaponManager weaponManager;

    float currentAimProgress = 0f;


    void Update()
    {
        if (weaponManager._currentGun == null) cameraPlayer.Lens.FieldOfView = normalFOV;


        if(weaponManager == null || !weaponManager._currentGun.canAim) 
        {
             if(playerCamera != null) playerCamera.SetSensitivityMode(AimType.Normal);
             return;
        }


        bool isAimingRequest = playerCharacter._requestedAim;


        // --- CAMBIO DE SENSIBILIDAD ---
        if (playerCamera != null)
        {
            if (isAimingRequest) playerCamera.SetSensitivityMode(weaponManager._currentGun.aimType);
            else playerCamera.SetSensitivityMode(AimType.Normal);
        }
        
        float targetProgress = playerCharacter._requestedAim ? 1 : 0;
        weaponManager._currentGun.isAiming = playerCharacter._requestedAim ? true : false;

        float step = Time.deltaTime / weaponManager._currentGun.timeToAim;

        currentAimProgress = Mathf.MoveTowards(currentAimProgress, targetProgress, step);

        float curveValue = weaponManager._currentGun.aimCurve.Evaluate(currentAimProgress);

        if(cameraPlayer != null)
        {
            cameraPlayer.Lens.FieldOfView = Mathf.Lerp(
                normalFOV, 
                weaponManager._currentGun.aimingFOV, 
                curveValue
            );
        }
        if (cameraGun != null)
        {    
            cameraGun.fieldOfView = Mathf.Lerp(
                normalGunFOV, 
                weaponManager._currentGun.gunAimingFOV, 
                curveValue
            );
        }
    }
}
