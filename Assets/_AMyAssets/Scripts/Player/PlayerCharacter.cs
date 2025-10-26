using UnityEngine;
using KinematicCharacterController;
using PurrNet;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Rendering;

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
    public bool Aim;
    public bool ChangeGun;
    public int RequestedGunIndex;
    public bool Interact;
}

public enum LastGunEquiped
{
    None, Primary, Secondary, Utility
}

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide, Wall
}

public enum WallSide
{
    Left, Right, None
}

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
    public Vector3 Acceleration;
}


public class PlayerCharacter : NetworkBehaviour, ICharacterController
{
    public int currentGunIndex;
    [Space]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    [Space]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private List<Renderer> renderers = new();
    [SerializeField] private CinemachineCamera playerCamera;
    [Space]
    [SerializeField] private float walkSpeed = 12f;
    [SerializeField] private float runSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 20f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    [SerializeField] private int jumps = 2;
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float wallVelocity = 15f;
    [SerializeField] private float wallGravity = -10f;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private LayerMask wallLayer;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.5f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;
    [Space]
    [SerializeField] private float standheight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1.5f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    public CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;
    public LastGunEquiped _lastGunEquiped;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;
    public bool _requestedShoot;
    public bool _requestedShootThisFrame;
    public bool _requestedAim;
    public bool _requestedRun;
    public bool _requestedInteract;
    private Collider[] _unCrouchOverlapResults;

    public bool primaryIndex = false;
    private bool secondaryIndex = false;
    

    //Netcode
    private float syncTimer;
    private float syncRate = 0.05f;
    
    private Vector3 latestReceivedPosition;
    private Quaternion latestReceivedRotation;
    private bool hasReceivedRemote = false;
    



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
    }

    public void Intialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _unCrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;

        Debug.Log(motor.CollidableLayers.value);
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        // Pilla el input 2D y crea el movimiento 3D en el vector xz
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        // Clamp del movimiento para que este regulado al pulsar 2 teclas a la vez
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        // Orienta el input hacia donde mira el jugador
        _requestedMovement = input.Rotation * _requestedMovement;

        _requestedShoot = input.Shoot;
        _requestedShootThisFrame = input.ShootThisFrame;

        _requestedAim = input.Aim;

        _requestedRun = input.Running;

        _requestedInteract = input.Interact;

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


        if (input.ChangeGun && input.RequestedGunIndex > 0)
        {
            currentGunIndex = input.RequestedGunIndex;
            Debug.Log(currentGunIndex);
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
                        int gunToSwitchIndex = primaryIndex ? 0 : 1;
                        weaponManager.SwitchWeapon(gunToSwitchIndex);
                    }
                    else
                    {
                        weaponManager.SwitchWeapon(0);
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
                        int gunToSwitchIndex = secondaryIndex ? 0 : 1;
                        weaponManager.SwitchWeapon(gunToSwitchIndex);
                    }
                    else
                    {
                        weaponManager.SwitchWeapon(2);
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

    }

    public void UpdateBody(float deltaTime)
    {
        
        var currentHeight = motor.Capsule.height;
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
        Debug.Log(_state.Stance);
        if (isOwner)
        {
            // Copiamos la posición y rotación reales del motor al transform,
            // así el personaje se mueve también en el objeto de red
            transform.position = motor.TransientPosition;
            transform.rotation = motor.TransientRotation;

            // Cada cierto tiempo, enviamos esa posición por la red
            syncTimer += Time.deltaTime;
            if (syncTimer >= syncRate)
            {
                syncTimer = 0f;
                RpcSyncTransform(transform.position, transform.rotation);
            }
        }
        else
        {
            // Si no somos el dueño, actualizamos hacia la última posición recibida
            if (hasReceivedRemote)
            {
                transform.position = Vector3.Lerp(transform.position, latestReceivedPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Slerp(transform.rotation, latestReceivedRotation, Time.deltaTime * 10f);
            }
        }
    }

    
    [ObserversRpc]
    public void RpcSyncTransform(Vector3 pos, Quaternion rot)
    {
        // Esto se ejecuta en los demás clientes (no en el dueño)
        latestReceivedPosition = pos;
        latestReceivedRotation = rot;
        hasReceivedRemote = true;
    }

    


    public void AfterCharacterUpdate(float deltaTime)
    {
        //UnCrouch
        if (!_requestedCrouch && _state.Stance is not Stance.Stand)
        {
            _state.Stance = Stance.Stand;
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: standheight,
                yOffset: standheight * 0.5f
            );

            //Allowing the character to standup
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if (motor.CharacterOverlap(pos, rot, _unCrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                //Re-crouch
                _requestedCrouch = true;
                motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: standheight,
                    yOffset: standheight * 0.5f
                );
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
        //Crouch
        if (_requestedCrouch && _state.Stance is Stance.Stand)
        {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        // Ignora triggers y el propio collider del personaje
        if (coll.isTrigger) return false;
        if (coll.transform.IsChildOf(transform)) return false;

        // Solo colisiona con el suelo o paredes (opcional: filtra por layer)
        if (coll.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            coll.gameObject.layer == LayerMask.NameToLayer("Wall"))
            return true;

        return false;
    }


    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
            _state.Stance = Stance.Crouch;
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // actualiza la rotacion del character hacia la misma direccion de la rotacion requerida (camera rotation)

        var forward = Vector3.ProjectOnPlane
       (
            _requestedRotation * Vector3.forward,
            motor.CharacterUp
       );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        // if on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;
            //Velocity
            var groundedMovement = motor.GetDirectionTangentToSurface
            (
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            //Start sliding
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;
                if (moving && crouching && (wasStanding || wasInAir) && _state.Stance is not Stance.Wall)
                {
                    _state.Stance = Stance.Slide;

                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane
                        (
                            vector: _lastState.Velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                        );
                    }

                    var effectiveSliderStartSpeed = slideStartSpeed;
                    if (!_lastState.Grounded && !_requestedCrouchInAir)
                    {
                        effectiveSliderStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }
                    var slideSpeed = Mathf.Max(effectiveSliderStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface
                    (
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;
                }
            }
            //Move
            if (_state.Stance is Stance.Stand or Stance.Crouch)
            {
                // Calculate the speed and responsiveneess of movement based on the character's stance
                var speed = 0f;

                if (_state.Stance is Stance.Stand) 
                    speed = _requestedRun ? runSpeed : walkSpeed;
                else if (_state.Stance is Stance.Crouch)
                    speed = crouchSpeed;

                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;

                // And smoothly move along the ground in that direction
                var targetVelocity = groundedMovement * speed;
                var moveVelocity = Vector3.Lerp
                (
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );

                _state.Acceleration = moveVelocity - currentVelocity;
                currentVelocity = moveVelocity;
            }


            //Continue sliding
            else
            {
                //Friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                //Slope
                {
                    var force = Vector3.ProjectOnPlane
                    (
                        vector: -motor.CharacterUp,
                        planeNormal: motor.GroundingStatus.GroundNormal
                    ) * slideGravity;

                    currentVelocity -= force * deltaTime;
                }

                //Steer
                {
                    // Target velocity is the player's movement direction, at the current speed
                    var currentSpeed = currentVelocity.magnitude;
                    var targetVelocity = groundedMovement * currentSpeed;
                    var steervelocity = currentVelocity;
                    var steerForce = (targetVelocity - steervelocity) * slideSteerAcceleration * deltaTime;
                    // Add steer force, but clamp velocity so the slide speed doesn't increase due to direction movement input
                    steervelocity += steerForce;
                    steervelocity = Vector3.ClampMagnitude(steervelocity, currentSpeed);

                    _state.Acceleration = (steervelocity - currentVelocity) / deltaTime;
                    currentVelocity = steervelocity;
                }

                //Stop
                if (currentVelocity.magnitude < slideEndSpeed)
                {
                    _state.Stance = Stance.Crouch;
                }
            }
        }
        //else, in the air
        else
        {
            _timeSinceUngrounded += deltaTime;
            // Move
            if (_requestedMovement.sqrMagnitude > 0f)
            {
                // Requested movement projected on to movement plane. (,agnitude preserved)
                var planarMovement = Vector3.zero;
                var projected = Vector3.ProjectOnPlane
                (
                    vector: _requestedMovement,
                    planeNormal: motor.CharacterUp
                );
                if (projected.sqrMagnitude > 0)
                    planarMovement = projected.normalized;

                // Current velocity on movement plane
                var currentPlanarVelocity = Vector3.ProjectOnPlane
                (
                    vector: currentVelocity,
                    planeNormal: motor.CharacterUp
                );

                // Calculate movement force
                var movementForce = planarMovement * airAcceleration * deltaTime;

                // If moving slower than the max air speed, treat movementForce as a simple steering force
                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                    // Add it to the current planar velocity for a target velocity
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    // Limit target velocity to air speed
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                    // Steer towards target velocity
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                // Otherwise, nerf the movement force when it is in the direction of the current planar velocity
                // to prevent acceleration further beyond the max air speed
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    // Project movement force onto the plane whose normal is the current planar velocity
                    var constrainedMovementForce = Vector3.ProjectOnPlane
                    (
                        vector: movementForce,
                        planeNormal: currentPlanarVelocity.normalized
                    );

                    movementForce = constrainedMovementForce;
                }

                // Prevent air-climbing steep slopes
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    // If moving in the same direction as the resultant velocity...
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        // Calculate obstruction normal
                        var obstructionNormal = Vector3.Cross
                        (
                            motor.CharacterUp,
                            Vector3.Cross
                            (
                                motor.CharacterUp,
                                motor.GroundingStatus.GroundNormal
                            )
                        ).normalized;

                        // Projected movement force onto obstruction plane
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            // Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;


            // wall run
            var wall = GetWallSide;

            if (wall != WallSide.None && _state.Stance is not Stance.Wall)
            {
                _state.Stance = Stance.Wall;
                Debug.Log("Cambio de estado a pared");
            }

            if (_state.Stance is Stance.Wall)
            {
                _state.Stance = Stance.Wall;

                float verticalVelocity = Vector3.Dot(currentVelocity, motor.CharacterUp);

                // 1. Reducir gravedad
                verticalVelocity += wallGravity * deltaTime;
            }

        }

        if (_requestedJump)
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;
            if (grounded)
                jumps = 2;

            if (jumps > 0 || canCoyoteJump)
            {
                Debug.Log("JUMP!");
                _requestedJump = false; // Unset jump request
                _requestedCrouch = false; // and request the character uncrouches
                _requestedCrouchInAir = false;

                //Unstick the player of the ground
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;

                //Set minum vertical speed to the jump speed
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                //Add the difference in current vertical speed to the character's velocity
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

                jumps -= 1;
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;

                // Defer the jump request until coyote time has passed.
                var canJumpLater = _timeSinceJumpRequest < coyoteTime;
                _requestedJump = canJumpLater;
            }


        }
    }



    public Transform GetCameraTarget() => cameraTarget;
    public CharacterState GetState() => _state;
    public CharacterState GetLastState() => _lastState;

    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        if (killVelocity)
            motor.BaseVelocity = Vector3.zero;
    }

    public WallSide GetWallSide
    {
        get
        {
            if (IsOnLeftWall)
                return WallSide.Left;
            else if (IsOnRightWall)
                return WallSide.Right;
            else
                return WallSide.None;

        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + -motor.CharacterRight * wallCheckDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + motor.CharacterRight * wallCheckDistance);
    }

    public bool IsOnLeftWall => Physics.Raycast(transform.position, -motor.CharacterRight, wallCheckDistance, wallLayer);
    public bool IsOnRightWall => Physics.Raycast(transform.position, motor.CharacterRight, wallCheckDistance, wallLayer);

    public bool GetNormal(out Vector3 normal)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -motor.CharacterRight, out hit, wallCheckDistance, wallLayer))
        {
            normal = hit.normal;
            return true;
        }
        else if (Physics.Raycast(transform.position, motor.CharacterRight, out hit, wallCheckDistance, wallLayer))
        {
            normal = hit.normal;
            return true;
        }
        normal = Vector3.zero;
        return false;
    }
}