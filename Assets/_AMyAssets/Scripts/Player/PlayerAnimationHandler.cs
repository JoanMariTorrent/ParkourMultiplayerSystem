using PurrNet;
using UnityEngine;

public class PlayerAnimationHandler : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private GameObject armsMesh;

    [Header("---- Motor procedural ----")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("1. Configuracion IDLE")]
    public float idleAmount = 0.005f;
    public float idleSpeed = 1f;

    [Header("2. Configuracion WALK")]
    public float walkBobAmount = 0.02f;
    public float walkBobSpeed = 10f;
    public float startWalkSpeedVel = 1f;

    [Header("3. Configuracion RUN / SLIDE")]
    public Vector3 runningTilt = new Vector3(15f, 15f, 15f);
    public float runTransitionSpeed = 6f;
    public float startRunSpeedVel = 12f;
    public float runBobMultiplier = 1.5f;

    [Header("4. Configuracion SLIDE IMPACT")]
    public float slideDropAmount = -0.15f;
    public float slideKickRotation = -10f;
    public float slideRecoverySpeed = 8f;

    [Header("5. Configuracion STRAFE")]
    public float strafeTiltAmount = 5f;
    public float forwardTiltAmount = 5f;
    public float strafeTiltSpeed = 5f;

    [Header("6. Configuracion SWAY")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float swaySmooth = 4f;
    
    [Header("7. Configuracion AIM")]
    public Vector3 aimPosition = new Vector3(0,0,0);
    public float aimSpeed = 10f;
    


    // IDs solo para las acciones en especifico, el movimiento se hace procedural
    private int reloadHash = Animator.StringToHash("Reload");
    private int shootHash = Animator.StringToHash("Shoot");

    private Vector3 _initialPos;
    private Quaternion _initialRot;
    private float _bobTimer;
    private float _currentStrafeTilt;
    private float _currentForwardTilt;
    
    private bool _wasSliding;
    private float _currentSlideDrop;
    private float _currentSlideKick; 
    private bool isAiming;


    void Start()
    {
        if(weaponHolder != null)
        {
            _initialPos = weaponHolder.localPosition;
            _initialRot = weaponHolder.localRotation;
        }

        if(playerCharacter == null) playerCharacter = GetComponentInParent<PlayerCharacter>();
    }


    public void RegisterWeaponAnimator(Animator newWeaponAnim)
    {
        weaponAnimator = newWeaponAnim;
    }

    public void UnRegisterWeaponAnimator()
    {
        weaponAnimator = null;
    }

    void Update()
    {
        if(!isOwner) return;

        if(armsMesh != null)
        {
            armsMesh.SetActive(weaponAnimator != null);
        }
        
        if (weaponHolder != null && playerCharacter != null)
        {
            HandleProceduralMovement();
        }
    }

    private void HandleProceduralMovement()
    {
        float dt = Time.deltaTime;

        // 1. Datos
        Vector3 vel = playerCharacter._state.Velocity;
        float speed = new Vector3(vel.x, 0, vel.z).magnitude;
        bool isGrounded = playerCharacter._state.Grounded;
        bool isCrouched = playerCharacter._state.Stance == Stance.Crouch;

        // Definir velocidad de correr
        bool isRunning = speed > startRunSpeedVel && isGrounded;
        bool isWalking = speed > startWalkSpeedVel && isGrounded;

        bool isSliding = playerCharacter._state.Stance == Stance.Slide;
        bool isOnWall = playerCharacter._state.Stance == Stance.Wall;
        
        isAiming = playerCharacter.isAiming;
        float aimReduction = isAiming ? 0.1f : 1f;  

        bool useRunTilt = !isAiming && isGrounded && (isRunning || isCrouched || isSliding || isRunning);

        if (isSliding && !_wasSliding)
        {
            _currentSlideDrop = slideDropAmount;
            _currentSlideKick = slideKickRotation;
        }
        _wasSliding = isSliding;

        // recuperacion del golpe
        _currentSlideDrop = Mathf.Lerp(_currentSlideDrop, 0f, dt * slideRecoverySpeed);
        _currentSlideKick = Mathf.Lerp(_currentSlideKick, 0f, dt * slideRecoverySpeed);

        // 2. Inputs (cambiar por el input action)
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        float currentSwayAmount = swayAmount * aimReduction;
        float currentMaxSway = maxSway * aimReduction;

        // 3. SWAY
        // hay que invertir el raton para que de el efecto contrario
        float moveX = Mathf.Clamp(-mouseX * currentSwayAmount, -currentMaxSway, currentMaxSway);
        float moveY = Mathf.Clamp(-mouseY * currentSwayAmount, -currentMaxSway, currentMaxSway);
        Vector3 finalSway = new Vector3(moveX, moveY, 0);

        // 4. Movimiento
        Vector3 finalBob = Vector3.zero;

        if(isWalking || isSliding || isOnWall)
        {
            float currentBobSpeed = isRunning || isOnWall ? walkBobSpeed * 1.3f : walkBobSpeed;
            float currentBobAmp = isRunning || isOnWall ? walkBobAmount * runBobMultiplier : walkBobAmount;

            currentBobAmp *= aimReduction;

            _bobTimer += dt * currentBobSpeed;

            // Formula del infinito
            // X se mueve en coseno (izquierda y derecha)
            // Y se mueve en seno * 2 (arriba y abajo)
            float bobX = Mathf.Cos(_bobTimer / 2) * currentBobAmp;
            float bobY = Mathf.Sin(_bobTimer) * currentBobAmp;
            float bobZ = 0f;

            if(isRunning)
            {
                bobY *= 0.5f;
                bobZ = Mathf.Sin(_bobTimer) * (currentBobAmp * 0.5f);
            }
            
            finalBob = new Vector3(bobX, bobY, bobZ);
        }
        else
        {
            _bobTimer = 0f;
            // IDLE
            float idleY = Mathf.Sin(Time.time * idleSpeed) * (idleAmount * aimReduction);
            finalBob = new Vector3(0, idleY, 0);
        }

        // 5. Run tilt
        Quaternion targetRot = _initialRot;

        if(useRunTilt || isOnWall)
        {
            Quaternion runRot = Quaternion.Euler(runningTilt) * _initialRot;
            targetRot = runRot;
        }


        // TILT (inclinacion del arma depende para donde estes caminando)
        float targetTiltZ = -inputX * strafeTiltAmount * aimReduction;
        _currentStrafeTilt = Mathf.Lerp(_currentStrafeTilt, targetTiltZ, dt * strafeTiltSpeed);

        float targetTiltX = -inputY * forwardTiltAmount * aimReduction;
        _currentForwardTilt = Mathf.Lerp(_currentForwardTilt, targetTiltX, dt * strafeTiltSpeed);

        Quaternion moveTiltRot = Quaternion.Euler(_currentForwardTilt, 0, _currentStrafeTilt);

        Quaternion slideKickRot = Quaternion.Euler(_currentSlideKick, 0, 0);

        // 6. Aplicarlo
        Vector3 slideImpactPos = new Vector3(0, _currentSlideDrop, 0);
        Vector3 basePos = isAiming ? aimPosition : _initialPos;
        Vector3 targetPos = basePos + finalSway + finalBob + slideImpactPos;

        float currentPosSpeed = isAiming ? aimSpeed : swaySmooth;
        float currentRotSpeed = isAiming ? aimSpeed : runTransitionSpeed; 
        
        weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetPos, dt * currentPosSpeed);
        weaponHolder.localRotation = Quaternion.Lerp(weaponHolder.localRotation, targetRot * slideKickRot * moveTiltRot, dt * currentRotSpeed);
    }


    // --- FUNCIONES DE ACCIÓN ---
    
    public void TriggerReload() { if (weaponAnimator != null) weaponAnimator.SetTrigger("Reload"); }

    public void TriggerShoot() { if (weaponAnimator != null) weaponAnimator.SetTrigger("Shoot"); }
}
