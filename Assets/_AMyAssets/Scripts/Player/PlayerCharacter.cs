using UnityEngine;
using KinematicCharacterController;
using PurrNet;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using Interfaces;

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public bool Running;
    public CrouchInput Crouch;
    public bool Shoot;
    public bool ShootThisFrame;
    public bool StopShooting;
    public bool Aim;
    public bool ChangeGun;
    public int RequestedGunIndex;
    public bool Interact;
    public bool Reload;
    public bool DropGun;
    public bool Emote;
    public bool MovementBlocked;
}

public enum LastGunEquiped { None, Primary, Secondary, Utility }
public enum CrouchInput { None, Toggle }
public enum Stance { Stand, Crouch, Slide, Wall, Climb, Grapple }
public enum WallSide { Left, Right, None }

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
    public Vector3 Acceleration;
    public Vector3 WallNormal; 
}

public class PlayerCharacter : NetworkBehaviour, ICharacterController
{
    [Header("References")]
    public int currentGunIndex;
    [SerializeField] private LayerMask interactLayerMask;
    [Range(0f, 10f)][SerializeField] private float interactDistance;
    [Space]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform root;
    [Space]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 12f;
    [SerializeField] private float runSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float aimWalkSpeed = 6f;
    [SerializeField] private float walkResponse = 20f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    [Header("Jetpack")]
    [SerializeField] private float maxJetpackFuel = 2.0f;
    [SerializeField] private float jetpackThrust = 45f;
    [SerializeField] private float jetpackFuelRegenDelay = 0.2f;
    [SerializeField] private float jetpackFuelRegenSpeed = 1.5f;
    [SerializeField] private float jetpackGravityMult = 0.3f;
    private float currentJetpackFuel;
    private bool firstJumpUsed;
    private bool isJetpacking = false;
    private float lastJetpackTime;

    [Space][Header("Salto")]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float gravity = -90f;
    [Header("Movement Penalties")]
    [SerializeField] private float reloadSpeedMultiplier = 0.6f;
    [SerializeField] private float hitSlowdownMultiplier = 0.5f;
    [SerializeField] private float hitSlowdownDuration = 1.0f;
    
    [Header("Wall Run Settings")]
    [SerializeField] private float wallRunSpeed = 18f; 
    [SerializeField] private float wallGravity = -10f; 
    [SerializeField] private float wallCheckDistance = 0.8f; 
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallJumpOutForce = 15f;
    [SerializeField] private float wallJumpUpForce = 12f;
    [SerializeField] private float wallEnterMinHeight = 0.5f;
    [SerializeField] private float wallJumpPostTime = 0.35f;
    [Tooltip("Qué tan rápido pierdes el impulso extra (valores bajos = más deslizamiento)")]
    [SerializeField] private float wallMomentumDecay = 2f; 
    [Tooltip("Qué tan rápido aceleras hasta la velocidad base de wallrun")]
    [SerializeField] private float wallRunAcceleration = 10f;

    [Header("Wall Climb Settings")]
    [SerializeField] private float climbSpeed = 20f; 
    [SerializeField] private float maxClimbDuration = 1.0f; 
    [SerializeField] private float climbJumpBackForce = 10f;
    [SerializeField] private float climbJumpUpForce = 15f;
    [Tooltip("Ángulo máximo de mirada a la pared para permitir escalar (ej: 30 grados). Si miras más de lado, hará Wall Run.")]
    [SerializeField] private float maxClimbAngle = 30f; 

    [Header("Slide Settings")]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.5f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;
    [Tooltip("Tiempo de espera en segundos antes de poder deslizarse de nuevo")]
    [SerializeField] private float slideCooldown = 1.0f; 

    [Header("Grapple Settings")]
    public Vector3 _grapplePoint; 
    [SerializeField] private float grapplePullSpeed = 25f; 
    [SerializeField] private float grappleAirControl = 40f; 
    [SerializeField] private float maxGrappleSpeed = 50f;

    [Space]
    [SerializeField] private float standheight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1.5f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    [Header("Camera Effects")]
    [SerializeField] private float wallRunTiltAngle = 15f; 
    [SerializeField] private float tiltSpeed = 10f;
    [Header("Audios")]
    [SerializeField] private AudioClip dontShoot;
    private bool cantEmote = false;

    // State
    public CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;
    public LastGunEquiped _lastGunEquiped;

    // Input
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;
    
    public bool _requestedShoot;
    public bool _requestedShootThisFrame;
    public bool _stopShooting;
    public bool _requestedAim;
    public bool _requestedRun;
    public bool _requestedInteract;
    public bool _requestedReload;
    public bool _requestedDropGun;
    public bool _requestedEmote;
    public int gunToSwitchIndex;
    public bool isAiming;


    private Collider[] _unCrouchOverlapResults;
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;
    
    public bool primaryIndex = false;
    private bool secondaryIndex = false;

    // Wall Run & Climb vars
    private Vector3 _wallNormal; 
    private WallSide _currentWallSide;
    private float _timeSinceWallJump = 10f;
    private bool _isFacingWall = false; 
    private float _currentClimbTimer = 0f;
    private Vector3 _lastWallNormal;
    private Vector3 _externalImpulse;

    // Slide vars
    private float _timeSinceLastSlide = 10f; 

    // GodMode
    private float initGravity;
    private float initAirAcceleration;
    public bool GodMode;

    // Netcode
    private float syncTimer;
    private float syncRate = 0.05f;
    private Vector3 latestReceivedPosition;
    private Quaternion latestReceivedRotation;
    private bool hasReceivedRemote = false;



    [Header("Tutorial Progression")]
    public bool canJumpTutorial = true;
    public bool canSprintTutorial = true;
    public bool canCrouchTutorial = true;
    public bool canSlideTutorial = true;
    public bool canWallRunTutorial = true;
    public bool canClimbTutorial = true;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        playerCamera.gameObject.SetActive(isOwner);
        if (isOwner)
        {
            foreach (var rend in renderers)
            {
                rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
        else if (!isOwner)
        {
            motor.enabled = false;
        }

        initGravity = gravity;
        initAirAcceleration = airAcceleration;
    }

    public void Intialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _unCrouchOverlapResults = new Collider[8];
        motor.CharacterController = this;
        currentJetpackFuel = maxJetpackFuel;
    }

    public void UpdateInput(CharacterInput input)
    {
        if(playerHealth != null && (playerHealth.IsDead || playerHealth.health <= 0) || input.MovementBlocked)
        {
            _requestedMovement = Vector3.zero;
            _requestedJump = false;
            _requestedShoot = false;
            _requestedAim = false;
            return;
        }
        _requestedRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;

        _requestedShoot = input.Shoot;
        _requestedShootThisFrame = input.ShootThisFrame;
        _stopShooting = input.StopShooting;
        _requestedAim = input.Aim;
        _requestedRun = input.Running;
        _requestedReload = input.Reload;
        _requestedDropGun = input.DropGun;
        _requestedInteract = input.Interact;
        _requestedEmote = input.Emote;

        bool canGunAim = weaponManager != null && 
                weaponManager._currentGun != null && 
                weaponManager._currentGun.canAim;

        isAiming = _requestedAim && canGunAim;

        if (_requestedInteract)
        {
            Physics.Raycast(cameraTransform.transform.position, cameraTransform.forward, out var hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide);
            Debug.DrawLine(cameraTransform.transform.position, hit.point, Color.red, 1.0f);
            
            if (hit.transform != null)
            {
                if (hit.collider.TryGetComponent(out ITakeGun takeGun))
                {
                    takeGun.TakeGun(this);
                }
            }
        }

        var wasRequestedJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if (_requestedJump && !wasRequestedJump)
            _timeSinceJumpRequest = 0f;

        _requestedSustainedJump = input.JumpSustain;

        var wasRequestingCrouch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };

        if (_requestedCrouch && !wasRequestingCrouch)
            _requestedCrouchInAir = !_state.Grounded;
        else if (!_requestedCrouch && wasRequestingCrouch)
            _requestedCrouchInAir = false;

        if (_requestedReload && weaponManager != null) weaponManager._currentGun.Reload();
        if (_requestedDropGun) weaponManager.DropGun();

        if (input.ChangeGun && input.RequestedGunIndex > 0)
        {
            currentGunIndex = input.RequestedGunIndex;
            ChangeGun(currentGunIndex);
        }

        if(_requestedEmote && !cantEmote)
        {
            AudioManager.Instance.PlaySound(dontShoot,transform.position, AudioType.SFX, 1, pitch: Random.Range(0.95f, 1.1f), parent: transform);
            cantEmote = true;
            StartCoroutine(Emote());
        }
    }

    private System.Collections.IEnumerator Emote()
    {
        yield return new WaitForSeconds(dontShoot.length + 0.05f);
        cantEmote = false;
    }
    
    [ObserversRpc(runLocally: false)]
    public void ChangeGun(int currentGunIndex)
    {
        switch (currentGunIndex)
        {
            case 1:
                if (weaponManager._ownedWeapons[0] != null && weaponManager._ownedWeapons[1] != null)
                {
                    if (_lastGunEquiped == LastGunEquiped.Primary)
                    {
                        _lastGunEquiped = LastGunEquiped.Primary;
                        primaryIndex = !primaryIndex;
                    }
                    else
                    {
                        _lastGunEquiped = LastGunEquiped.Primary;
                    }
                    gunToSwitchIndex = primaryIndex ? 0 : 1;
                    weaponManager.SwitchWeapon(gunToSwitchIndex);
                }
                else if (weaponManager._ownedWeapons[0] != null && weaponManager._ownedWeapons[1] == null)
                {
                    weaponManager.SwitchWeapon(0);
                    _lastGunEquiped = LastGunEquiped.Primary;
                }
                else if (weaponManager._ownedWeapons[0] == null && weaponManager._ownedWeapons[1] != null)
                {
                    weaponManager.SwitchWeapon(1);
                    _lastGunEquiped = LastGunEquiped.Primary;
                }
                break;
            case 2:
                if (weaponManager._ownedWeapons[2] != null && weaponManager._ownedWeapons[3] != null)
                {
                    if (_lastGunEquiped == LastGunEquiped.Secondary)
                    {
                        _lastGunEquiped = LastGunEquiped.Secondary;
                        secondaryIndex = !secondaryIndex;
                    }
                    else
                    {
                        _lastGunEquiped = LastGunEquiped.Secondary;
                    }
                    gunToSwitchIndex = secondaryIndex ? 2 : 3;
                    weaponManager.SwitchWeapon(gunToSwitchIndex);
                }
                else if (weaponManager._ownedWeapons[2] != null && weaponManager._ownedWeapons[3] == null)
                {
                    weaponManager.SwitchWeapon(2);
                    _lastGunEquiped = LastGunEquiped.Secondary;
                }
                else if (weaponManager._ownedWeapons[2] == null && weaponManager._ownedWeapons[3] != null)
                {
                    weaponManager.SwitchWeapon(3);
                    _lastGunEquiped = LastGunEquiped.Secondary;
                }
                break;
            case 3:
                weaponManager.SwitchWeapon(4);
                break;
            case 4:
                //weaponManager.SwitchWeapon(5);
                break;
        }
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = 3;
        var normalizeHeight = currentHeight / standheight;

        var cameraTargetHeight = currentHeight *
        (
            _state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight
        );
        var rootTargetScale = new Vector3(1f, normalizeHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
        (
            a: cameraTarget.localPosition,
            b: new Vector3(0f, cameraTargetHeight, 0f),
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        root.localScale = Vector3.Lerp
        (
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }

    void Update()
    {
        if (isOwner)
        {
            transform.position = motor.TransientPosition;
            transform.rotation = motor.TransientRotation;

            HandleCameraTilt();

            syncTimer += Time.deltaTime;
            if (syncTimer >= syncRate)
            {
                syncTimer = 0f;
                RpcSyncTransform(transform.position, transform.rotation);
            }
        }
        else
        {
            if (hasReceivedRemote)
            {
                transform.position = Vector3.Lerp(transform.position, latestReceivedPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Slerp(transform.rotation, latestReceivedRotation, Time.deltaTime * 10f);
            }
        }
    }

    private void HandleCameraTilt()
    {
        if (playerCamera == null) return;

        float targetTilt = 0f;

        if (_state.Stance == Stance.Wall)
        {
            if (_currentWallSide == WallSide.Left)
                targetTilt = -wallRunTiltAngle; 
            else if (_currentWallSide == WallSide.Right)
                targetTilt = wallRunTiltAngle;  
        }

        var lens = playerCamera.Lens;
        lens.Dutch = Mathf.Lerp(lens.Dutch, targetTilt, Time.deltaTime * tiltSpeed);
        playerCamera.Lens = lens;
    }

    [ObserversRpc]
    public void RpcSyncTransform(Vector3 pos, Quaternion rot)
    {
        latestReceivedPosition = pos;
        latestReceivedRotation = rot;
        hasReceivedRemote = true;
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // CORRECCIÓN AQUÍ: Añadimos la excepción del Grapple
        if (!_requestedCrouch && 
            _state.Stance is not Stance.Stand && 
            _state.Stance is not Stance.Wall && 
            _state.Stance is not Stance.Climb &&
            _state.Stance is not Stance.Grapple)
        {
            _state.Stance = Stance.Stand;
            motor.SetCapsuleDimensions(motor.Capsule.radius, standheight, standheight * 0.5f);

            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            
            // Check para ver si nos podemos levantar (por si había techo)
            if (motor.CharacterOverlap(pos, rot, _unCrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                _requestedCrouch = true;
                motor.SetCapsuleDimensions(motor.Capsule.radius, standheight, standheight * 0.5f);
            }
            else
                _state.Stance = Stance.Stand;
        }

        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        _lastState = _tempState;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;
        _lastWallNormal = _wallNormal;
        
        if (_requestedCrouch && _state.Stance == Stance.Stand)
        {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, crouchHeight * 0.5f);
        }
        
        _timeSinceWallJump += deltaTime;
        _timeSinceLastSlide += deltaTime; 

        DetectWall(out _currentWallSide, out _wallNormal);
        DetectFrontWall(out _isFacingWall, out Vector3 frontWallNormal);
        
        if (_isFacingWall)
        {
            float angleBetweenWalls = Vector3.Angle(_lastWallNormal, frontWallNormal);
            if(_state.Stance != Stance.Wall || angleBetweenWalls < 70f)
            {
                _wallNormal = frontWallNormal;
            }
            else
            {
                _isFacingWall = false;
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (coll.isTrigger) return false;
        if (coll.transform.IsChildOf(transform)) return false;
        return true; 
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance == Stance.Slide)
            _state.Stance = Stance.Crouch;
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, motor.CharacterUp);
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // 1. Reduccion de velocidad global (Tus modificadores de recarga y daño)
        float globalSlowdown = 1f;
        if(weaponManager != null && weaponManager._currentGun != null && weaponManager._currentGun.IsReloading)
        {
            globalSlowdown *= reloadSpeedMultiplier;
        }
        if (playerHealth != null)
        {
            float tiempoDesdeGolpe = Time.time - playerHealth.lastTimeTakenDamage;
            if (tiempoDesdeGolpe < hitSlowdownDuration)
            {
                float t = tiempoDesdeGolpe / hitSlowdownDuration;
                float targetSlowdown = Mathf.Lerp(1f, hitSlowdownMultiplier, playerHealth.lastHitIntensity);
                globalSlowdown *= Mathf.Lerp(targetSlowdown, 1f, t);
            }
        }

        _state.Acceleration = Vector3.zero;

        // 2. LÓGICA DE GANCHO (GRAPPLE)
        if (_state.Stance == Stance.Grapple)
        {
            Vector3 toHookVector = _grapplePoint - transform.position;
            Vector3 directionToHook = toHookVector.normalized;
            float distanceToHook = toHookVector.magnitude;
            float currentPull = grapplePullSpeed;

            if (transform.position.y < _grapplePoint.y) currentPull *= 1.5f; 
            currentVelocity += directionToHook * currentPull * deltaTime;

            float dot = Vector3.Dot(currentVelocity, directionToHook);
            if (dot < 0) currentVelocity = Vector3.ProjectOnPlane(currentVelocity, directionToHook);

            if (_requestedMovement.sqrMagnitude > 0f)
            {
                var planarInput = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp).normalized;
                currentVelocity += planarInput * grappleAirControl * deltaTime;
            }

            if (distanceToHook < 1.5f) currentVelocity *= 0.9f; 
            if (currentVelocity.magnitude > maxGrappleSpeed) currentVelocity = currentVelocity.normalized * maxGrappleSpeed;

            return; // Salimos de la función si estamos usando el gancho
        }

        // 3. ESTADO DE SUELO NORMAL (Correr, andar, agacharse, deslizarse)
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;
            _currentClimbTimer = 0f; 

            // RESET DE JETPACK
            firstJumpUsed = false;
            isJetpacking = false;

            if (Time.time > lastJetpackTime + jetpackFuelRegenDelay)
            {
                currentJetpackFuel = Mathf.MoveTowards(currentJetpackFuel, maxJetpackFuel, jetpackFuelRegenSpeed * deltaTime);
            }

            if (_state.Stance == Stance.Wall || _state.Stance == Stance.Climb) 
                _state.Stance = Stance.Stand;

            var groundedMovement = motor.GetDirectionTangentToSurface(_requestedMovement, motor.GroundingStatus.GroundNormal) * _requestedMovement.magnitude;

            // Sliding
            bool isMoving = groundedMovement.sqrMagnitude > 0f;
            bool isCrouching = _state.Stance == Stance.Crouch;

            if (canSlideTutorial && isMoving && isCrouching && _requestedRun && (_lastState.Stance == Stance.Stand || !_lastState.Grounded) && _state.Stance != Stance.Wall && _timeSinceLastSlide >= slideCooldown)
            {
                _state.Stance = Stance.Slide;
                _timeSinceLastSlide = 0f; 
                if (!_lastState.Grounded) currentVelocity = Vector3.ProjectOnPlane(_lastState.Velocity, motor.GroundingStatus.GroundNormal);
                var effectiveSliderStartSpeed = (!_lastState.Grounded && !_requestedCrouchInAir) ? 0f : slideStartSpeed;
                effectiveSliderStartSpeed *= globalSlowdown;
                if (!_lastState.Grounded && !_requestedCrouchInAir) _requestedCrouchInAir = false;
                var slideSpeed = Mathf.Max(effectiveSliderStartSpeed, currentVelocity.magnitude);
                currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, motor.GroundingStatus.GroundNormal) * slideSpeed;
            }

            // Movimiento base
            if (_state.Stance == Stance.Stand || _state.Stance == Stance.Crouch)
            {
                bool isShooting = _requestedShoot || _requestedShootThisFrame;
                bool isMovingForward = Vector3.Dot(_requestedMovement.normalized, motor.CharacterForward) > 0.3f;
                bool canSprint = _requestedRun && canSprintTutorial && isMovingForward && !isShooting && !isAiming;

                float targetSpeed;
                float response;

                if (_state.Stance == Stance.Stand)
                {
                    if (canSprint) targetSpeed = runSpeed;
                    else if (isAiming) targetSpeed = aimWalkSpeed; 
                    else targetSpeed = walkSpeed;
                    response = walkResponse;
                }
                else 
                {
                    targetSpeed = crouchSpeed;
                    response = crouchResponse;
                }

                targetSpeed *= globalSlowdown;
                var targetVelocity = groundedMovement * targetSpeed;
                var moveVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-response * deltaTime));
                _state.Acceleration = moveVelocity - currentVelocity;
                currentVelocity = moveVelocity;
            }
            // Fricción de deslizamiento
            else
            {
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);
                currentVelocity -= Vector3.ProjectOnPlane(-motor.CharacterUp, motor.GroundingStatus.GroundNormal) * slideGravity * deltaTime; 
                var currentSpeed = currentVelocity.magnitude;
                var targetVelocity = groundedMovement * (currentSpeed * globalSlowdown);
                var steerForce = (targetVelocity - currentVelocity) * slideSteerAcceleration * deltaTime;
                var newVelocity = currentVelocity + steerForce;
                currentVelocity = Vector3.ClampMagnitude(newVelocity, currentSpeed);
                _state.Acceleration = (currentVelocity - newVelocity) / deltaTime; 
                if (currentVelocity.magnitude < (slideEndSpeed * globalSlowdown)) _state.Stance = Stance.Crouch;
            }
        }
        // 4. ESTADO DE AIRE (Caída, Jetpack, Wallrun, Climb)
        else
        {
            _timeSinceUngrounded += deltaTime;

            if (!firstJumpUsed && _timeSinceUngrounded > coyoteTime)
            {
                firstJumpUsed = true; 
            }

            bool isMovingForward = Vector3.Dot(_requestedMovement, motor.CharacterForward) > 0.1f;
            float lookAngle = Vector3.Angle(motor.CharacterForward, -_wallNormal);
            bool isLookingAtWall = lookAngle < maxClimbAngle;

            // Wall Climb
            if (_isFacingWall && isLookingAtWall && !_state.Grounded && isMovingForward && _currentClimbTimer < maxClimbDuration)
            {
                if (_requestedCrouch || _requestedCrouchInAir)
                {
                    _requestedCrouch = false;
                    _requestedCrouchInAir = false;
                }
                if (_state.Stance != Stance.Climb)
                {
                    if (!Physics.Raycast(transform.position, -Vector3.up, wallEnterMinHeight, LayerMask.GetMask("Ground", "Default")))
                    {
                        _state.Stance = Stance.Climb;
                        motor.SetCapsuleDimensions(motor.Capsule.radius, standheight, standheight * 0.5f);
                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, _wallNormal);
                        currentVelocity.y = climbSpeed * globalSlowdown;
                    }
                }
            }
            // Wall Run
            else if (canWallRunTutorial && _currentWallSide != WallSide.None && !_state.Grounded && _requestedMovement.magnitude > 0.1f && isMovingForward)
            {
                firstJumpUsed = false;
                isJetpacking = false;
    
                if (Time.time > lastJetpackTime + jetpackFuelRegenDelay)
                {
                    currentJetpackFuel = Mathf.MoveTowards(currentJetpackFuel, maxJetpackFuel, jetpackFuelRegenSpeed * deltaTime);
                }

                if (_requestedCrouch || _requestedCrouchInAir)
                {
                    _requestedCrouch = false;
                    _requestedCrouchInAir = false;
                }

                if (_state.Stance != Stance.Wall && _state.Stance != Stance.Climb) 
                {
                    if (!Physics.Raycast(transform.position, -Vector3.up, wallEnterMinHeight, LayerMask.GetMask("Ground", "Default")))
                    {
                        _state.Stance = Stance.Wall;
                        motor.SetCapsuleDimensions(motor.Capsule.radius, standheight, standheight * 0.5f);
                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, _wallNormal);
                        currentVelocity.y = 0; 
                    }
                }
            }
            else
            {
                if (_state.Stance == Stance.Wall || _state.Stance == Stance.Climb)
                {
                    _state.Stance = Stance.Stand;
                }
            }

            // Movimiento de Wall Climb
            if (canClimbTutorial && _state.Stance == Stance.Climb)
            {
                _currentClimbTimer += deltaTime;
                float effectiveClimbSpeed = climbSpeed * globalSlowdown;
                float deceleration = effectiveClimbSpeed / maxClimbDuration; 
                currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, 0f, deceleration * deltaTime);
                Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
                currentVelocity -= horizontalVel * 5f * deltaTime; 
                currentVelocity += -_wallNormal * 2f; 
            }
            // Movimiento de Wall Run
            else if (_state.Stance == Stance.Wall)
            {
                if (Vector3.Dot(_lastWallNormal, _wallNormal) < 0.5f && _lastWallNormal != Vector3.zero)
                {
                    _state.Stance = Stance.Stand; 
                    return; // OJO AQUÍ, el return interrumpe el salto si lo dejas dentro.
                }
                _currentClimbTimer = 0f; 
                float verticalVelocity = currentVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
                float horizontalSpeed = horizontalVelocity.magnitude;
                Vector3 tangentDir = Vector3.ProjectOnPlane(horizontalVelocity, _wallNormal).normalized;
                if (horizontalSpeed > 0.1f && tangentDir.sqrMagnitude > 0.01f) horizontalVelocity = tangentDir * horizontalSpeed;

                Vector3 wallRunDirection = Vector3.ProjectOnPlane(motor.CharacterForward, _wallNormal).normalized;
                wallRunDirection.y = 0; wallRunDirection.Normalize();

                if (_requestedMovement.magnitude > 0)
                {
                   Vector3 targetVelocity = wallRunDirection * (wallRunSpeed * globalSlowdown);
                   float currentSpeedAlongWall = Vector3.Dot(horizontalVelocity, wallRunDirection);

                   if (currentSpeedAlongWall > (wallRunSpeed * globalSlowdown))
                       horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, wallMomentumDecay * deltaTime);
                   else
                       horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, wallRunAcceleration * deltaTime);
                }

                currentVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
                currentVelocity += Vector3.down * (Mathf.Abs(wallGravity) * deltaTime);
                float speedFactor = horizontalSpeed * 0.5f; 
                currentVelocity += -_wallNormal * (2f + speedFactor); 
            }
            // Aire libre y Jetpack
            else
            { 
                if (_requestedMovement.sqrMagnitude > 0f)
                {
                    float effectiveAirAcceleration = airAcceleration * globalSlowdown;
                    float effectiveAirSpeed = airSpeed * globalSlowdown;

                    var planarMovement = Vector3.zero;
                    var projected = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp);
                    if (projected.sqrMagnitude > 0) planarMovement = projected.normalized;

                    var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                    var movementForce = planarMovement * effectiveAirAcceleration * deltaTime;

                    if (currentPlanarVelocity.magnitude < effectiveAirSpeed)
                    {
                        var targetPlanarVelocity = Vector3.ClampMagnitude(currentPlanarVelocity + movementForce, effectiveAirSpeed);
                        movementForce = targetPlanarVelocity - currentPlanarVelocity;
                    }
                    else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                    {
                        movementForce = Vector3.ProjectOnPlane(movementForce, currentPlanarVelocity.normalized);
                    }

                    if (motor.GroundingStatus.FoundAnyGround)
                    {
                        if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                        {
                            var obstructionNormal = Vector3.Cross(motor.CharacterUp, Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal)).normalized;
                            movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                        }
                    }
                    currentVelocity += movementForce;
                }

                float effectiveGravity = gravity;

                // Lógica Jetpack
                if (isJetpacking && _requestedSustainedJump && currentJetpackFuel > 0)
                {
                    // 1. Calculamos qué porcentaje del tanque nos queda (1 = Lleno, 0 = Vacío)
                    float fuelRatio = currentJetpackFuel / maxJetpackFuel;
    
                    // 2. EMPUJE DINÁMICO: 
                    // Al principio (fuelRatio=1) el empuje es del 150% (1.5). 
                    // Al final (fuelRatio=0) el empuje cae al 20% (0.2).
                    float thrustMultiplier = Mathf.Lerp(0.2f, 1.5f, fuelRatio);
                    currentVelocity.y += (jetpackThrust * thrustMultiplier) * deltaTime;
    
                    // 3. Consumo de fuel
                    currentJetpackFuel = Mathf.Max(0, currentJetpackFuel - deltaTime);
                    lastJetpackTime = Time.time; 
                    
                    // Límite de seguridad
                    if (currentVelocity.y > jumpSpeed) currentVelocity.y = jumpSpeed;
    
                    // 4. GRAVEDAD DINÁMICA:
                    // Al principio flotas mucho (usa tu jetpackGravityMult, ej: 0.3).
                    // Conforme se acaba el gas, vuelve suavemente a la gravedad normal (1.0).
                    float dynamicGravity = Mathf.Lerp(1f, jetpackGravityMult, fuelRatio);
                    effectiveGravity *= dynamicGravity; 
                }
                else
                {
                    if (!_requestedSustainedJump || currentJetpackFuel <= 0) 
                    {
                        isJetpacking = false;
                    }
                }
    
                    currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
                }
        }


        // 5. LÓGICA DE SALTO (COMPLETAMENTE INDEPENDIENTE)
        if (_requestedJump && canJumpTutorial)
        {
            if (_state.Stance == Stance.Climb)
            {
                // TU CÓDIGO ORIGINAL DE CLIMB
                Vector3 jumpDirection = (_wallNormal * climbJumpBackForce) + (Vector3.up * climbJumpUpForce);
                currentVelocity = jumpDirection; 
                
                _state.Stance = Stance.Stand;
                _requestedJump = false;
                _ungroundedDueToJump = true;
                _timeSinceWallJump = 0f;
                _currentClimbTimer = maxClimbDuration; 
                
                // Bloqueamos el salto normal para que lo siguiente sea Jetpack
                firstJumpUsed = true;
                isJetpacking = false;
            }
            else if (_state.Stance == Stance.Wall)
            {
                // TU CÓDIGO ORIGINAL DE WALL RUN
                Vector3 velocityForward = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                velocityForward = Vector3.ProjectOnPlane(velocityForward, _wallNormal);

                Vector3 jumpImpulse = (_wallNormal * wallJumpOutForce) + (motor.CharacterUp * wallJumpUpForce);
                currentVelocity = velocityForward + jumpImpulse;
                
                _state.Stance = Stance.Stand;
                _requestedJump = false;
                _ungroundedDueToJump = true;
                _timeSinceWallJump = 0f; 
                
                // Bloqueamos el salto normal para que lo siguiente sea Jetpack
                firstJumpUsed = true;
                isJetpacking = false;
            }
            else
            {
                bool grounded = motor.GroundingStatus.IsStableOnGround;
                bool canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;

                // SALTO NORMAL (Suelo o Coyote Time)
                if (!firstJumpUsed && (grounded || canCoyoteJump))
                {
                    _requestedJump = false;
                    _requestedCrouch = false;
                    _requestedCrouchInAir = false;

                    motor.ForceUnground(time: 0f);
                    _ungroundedDueToJump = true;

                    // TU MATEMÁTICA ORIGINAL PARA EL SUELO
                    float currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                    float effectiveJumpSpeed = jumpSpeed * globalSlowdown;
                    float targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, effectiveJumpSpeed);

                    currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

                    firstJumpUsed = true;
                    isJetpacking = false;
                }
                // JETPACK (Ya gastaste el primer salto, estás en el aire y pulsas de nuevo)
                else if (firstJumpUsed && currentJetpackFuel > 0)
                {
                    isJetpacking = true;
                    _requestedJump = false;
                    _requestedCrouch = false;
                    _requestedCrouchInAir = false;
                    
                    // Empuje explosivo inicial:
                    // Si estás cayendo o subiendo muy lento, te impulsamos al instante 
                    // a la mitad de tu velocidad máxima de salto para que responda super rápido.
                    if (currentVelocity.y < jumpSpeed * 0.5f)
                    {
                        currentVelocity.y = jumpSpeed * 0.5f;
                    }
                }
                else
                {
                    // TU CÓDIGO ORIGINAL DE COYOTE TIME BUFFERING
                    _timeSinceJumpRequest += deltaTime;
                    _requestedJump = _timeSinceJumpRequest < coyoteTime;
                }
            }
        }

        // IMPULSOS EXTERNOS (Granadas)
        if(_externalImpulse.sqrMagnitude > 0.001f)
        {
            currentVelocity += _externalImpulse;
            _externalImpulse = Vector3.zero;
        }
    }

    public Transform GetCameraTarget() => cameraTarget;
    public CharacterState GetState() => _state;
    public CharacterState GetLastState() => _lastState;

    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        if (killVelocity) motor.BaseVelocity = Vector3.zero;
        motor.SetPosition(position);
    }

    private void DetectWall(out WallSide side, out Vector3 normal)
    {
        side = WallSide.None;
        normal = Vector3.zero;

        if (motor.GroundingStatus.IsStableOnGround || _timeSinceWallJump < wallJumpPostTime) return;

        Vector3 origin = transform.position + (motor.CharacterUp * standheight * 0.5f);
        float radius = 0.2f;
        float castDistance = wallCheckDistance;

        if (Physics.SphereCast(origin, radius, motor.CharacterRight, out RaycastHit rightHit, castDistance, wallLayer))
        {
            side = WallSide.Right;
            normal = rightHit.normal;
            return; 
        }

        if (Physics.SphereCast(origin, radius, -motor.CharacterRight, out RaycastHit leftHit, castDistance, wallLayer))
        {
            side = WallSide.Left;
            normal = leftHit.normal;
            return;
        }
    }

    private void DetectFrontWall(out bool isFacing, out Vector3 normal)
    {
        isFacing = false;
        normal = Vector3.zero;

        if (motor.GroundingStatus.IsStableOnGround || _timeSinceWallJump < wallJumpPostTime) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.SphereCast(origin, 0.2f, motor.CharacterForward, out RaycastHit hit, wallCheckDistance + 0.2f, wallLayer))
        {
            isFacing = true;
            normal = hit.normal;
        }
    }
    
    public WallSide GetWallSide => _currentWallSide;

    void OnDrawGizmos()
    {
        if (motor == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + -motor.CharacterRight * wallCheckDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + motor.CharacterRight * wallCheckDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + motor.CharacterForward * (wallCheckDistance + 0.2f));
    }

    public void ToggleGodMode(bool godMode)
    {
        GodMode = godMode;
        if (godMode)
        {
            motor.CollidableLayers = 0;
            motor.SetPosition(transform.position, true); 
            motor.BaseVelocity = Vector3.zero;
            rb.useGravity = false;
            gravity = 0;
            airAcceleration = 0;
            //jumps = 0;
        }
        else
        {
            motor.CollidableLayers = LayerMask.GetMask("Default", "Ground", "Wall");
            motor.BaseVelocity = Vector3.zero;
            rb.useGravity = true;
            gravity = initGravity;
            airAcceleration = initAirAcceleration;
        }
    }

    public void HandleFreeFly()
    {
        float speed = 20f;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) move += Vector3.down;

        if (move != Vector3.zero)
        {
            motor.SetPosition(transform.position + (move.normalized * speed * Time.deltaTime));
        }
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        motor.SetPositionAndRotation(position, rotation);
        
        motor.BaseVelocity = Vector3.zero;
        _state.Velocity = Vector3.zero;
        _state.Acceleration = Vector3.zero;
        
        _state.Stance = Stance.Stand;
        
        transform.position = position;
        transform.rotation = rotation;
    }



    public void StartGrapple(Vector3 point)
    {
        _grapplePoint = point;
        _state.Stance = Stance.Grapple;
        
        // Un pequeño impulso inicial hacia arriba ayuda a que el balanceo se sienta mejor
        // Si estabas en el suelo, esto te levanta
        if (_state.Grounded)
        {
             _state.Velocity += Vector3.up * 5f;
             motor.ForceUnground();
        }
    }

    public void StopGrapple()
    {
        if (_state.Stance == Stance.Grapple)
        {
            _state.Stance = Stance.Stand;
            
            // Al soltar, mantienes tu momento y ganas un mini salto
            // para salir disparado
            _state.Velocity += Vector3.up * 3f;
            
            // Reseteamos saltos para permitir doble salto tras el gancho
            //jumps = 1; 
        }
    }


    public void AddExplosionForce(Vector3 force)
    {
        if(isOwner)
        {
            _externalImpulse += force;
            motor.ForceUnground();
        }
    }
    
}

namespace Interfaces
{
    public interface ITakeGun
    {
        void TakeGun(PlayerCharacter playerCharacter); 
    }
}