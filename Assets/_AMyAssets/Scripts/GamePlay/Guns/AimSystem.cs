using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.ProBuilder;

public class AimSystem : MonoBehaviour
{
    [Header("Variables")]
    public float timeToAim;
    public AnimationCurve aimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Space]
    public float normalFOV = 70f;
    public float normalGunFOV = 70f;

    [Header("Referencias")]
    public CinemachineCamera cameraPlayer;
    public Camera cameraGun;
    [Space]
    public PlayerCharacter playerCharacter;
    public WeaponManager weaponManager;

    float currentAimProgress = 0f;


    void Update()
    {
        if(weaponManager == null || weaponManager._currentGun == null || !weaponManager._currentGun.canAim) return;

        
        float targetProgress = playerCharacter._requestedAim ? 1 : 0;

        float step = Time.deltaTime / timeToAim;

        currentAimProgress = Mathf.MoveTowards(currentAimProgress, targetProgress, step);

        float curveValue = aimCurve.Evaluate(currentAimProgress);

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
