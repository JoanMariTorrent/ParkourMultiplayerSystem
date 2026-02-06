using PurrNet;
using UnityEngine;

public class PlayerAnimationHandler : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator armsAnimator;
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private GameObject armsMesh;

    [Header("Configuración")]
    [SerializeField] private float dampTime = 0.1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private string crouchLayerName = "Crouch Layer";
    [SerializeField] private int crouchLayerIndex = 1;

    // IDs de los parámetros para que sea más rápido que usar strings
    private int speedHash = Animator.StringToHash("Speed");
    private int reloadHash = Animator.StringToHash("Reload");
    private int shootHash = Animator.StringToHash("Shoot");






    public void RegisterWeaponAnimator(Animator newWeaponAnim)
    {
        weaponAnimator = newWeaponAnim;

        if (armsAnimator != null && weaponAnimator != null)
        {
            weaponAnimator.SetFloat("Speed", armsAnimator.GetFloat(speedHash));

            AnimatorStateInfo armState = armsAnimator.GetCurrentAnimatorStateInfo(0);
            
            weaponAnimator.Play(armState.fullPathHash, 0, armState.normalizedTime);
        }
    }

    public void UnRegisterWeaponAnimator()
    {
        weaponAnimator = null;
    }

    void Update()
    {
        if(!isOwner) return;
        if(weaponAnimator == null)
        {
            if(armsMesh) armsMesh.SetActive(false);
        }
        else if (weaponAnimator != null)
        {
            if(armsMesh) armsMesh.SetActive(true);
        }
    }



    public void SetWeaponAnimator(AnimatorOverrideController overrideController)
    {
        if (overrideController != null)
        {
            armsAnimator.runtimeAnimatorController = overrideController;
        }
        else
        {
            Debug.LogWarning("PlayerAnimationHandler: El arma no tiene Animator Override asignado.");
        }
    }

    // --- FUNCIONES DE ACCIÓN ---
    
    public void TriggerReload()
    {
        armsAnimator.SetTrigger(reloadHash);
        if (weaponAnimator != null) weaponAnimator.SetTrigger("Reload");
    }

    public void TriggerShoot()
    {
        armsAnimator.ResetTrigger(shootHash);
        armsAnimator.SetTrigger(shootHash);
        if (weaponAnimator != null) weaponAnimator.SetTrigger("Shoot");
    }

    // --- MOVIMIENTO (Lo llamaremos desde el Update del Player) ---
    public void UpdateMovementValues(Vector3 velocity, bool isCrouching)
    {
        // 1. Calculamos la velocidad (0 a 1) para el Blend Tree
        velocity.y = 0; 
        float speed = velocity.magnitude;
        
        float maxRunSpeed = 17.5f; 
        float normalizedSpeed = Mathf.Clamp01(speed / maxRunSpeed);

        // 2. Enviamos al Animator
        armsAnimator.SetFloat(speedHash, normalizedSpeed, dampTime, Time.deltaTime);
        if (weaponAnimator != null) 
        {
            weaponAnimator.SetFloat("Speed", normalizedSpeed, dampTime, Time.deltaTime);
        }

        float targetWeight = isCrouching? 1 : 0;

        float currentWeight = armsAnimator.GetLayerWeight(crouchLayerIndex);

        float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * crouchTransitionSpeed);

        armsAnimator.SetLayerWeight(crouchLayerIndex, newWeight);
    }
}
