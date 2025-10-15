using UnityEngine;
using KinematicCharacterController;
using Unity.Mathematics;
using Unity.VisualScripting;
using Fossil;
using UnityEngine.TextCore.Text;


public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
}


public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 20f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.5f;
    [Space] 
    [SerializeField] private float standheight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private Collider[] _unCrouchOverlapResults;

    public void Intialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _unCrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;
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

        _requestedJump = _requestedJump || input.Jump;
        _requestedSustainedJump = input.JumpSustain;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
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



    public void AfterCharacterUpdate(float deltaTime)
    {
        //UnCrouch
        if (!_requestedCrouch && _state.Stance is not Stance.Stand)
        {
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
            coll.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
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
        // if on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            //Walking
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
                if (moving && crouching && (wasStanding || wasInAir))
                {
                    _state.Stance = Stance.Slide;

                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
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
                // Calculate the speed and responsiveneess of movment based on the character's stance
                var speed = _state.Stance is Stance.Stand
                    ? walkSpeed
                    : crouchSpeed;
                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;

                // And smoothly move along the ground in that direction
                var targetVelocity = groundedMovement * speed;
                currentVelocity = Vector3.Lerp
                (
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );
            }
            //Continue sliding
            else
            {
                //Friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

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

                // Add it to the current planar velocity for a target velocity
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                // Limit target velocity to air speed
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                // Stear towards current velocity
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }

            // Gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (_requestedJump)
        {
            _requestedJump = false; // Unset jump request
            _requestedCrouch = false; // and request the character uncrouches

            //Unstick the player of the ground
            motor.ForceUnground(time: 0f);

            //Set minum vertical speed to the jump speed
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            //Add the difference in current vertical speed to the character's velocity
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

        }
    }



    public Transform GetCameraTarget() => cameraTarget;

    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        if (killVelocity)
            motor.BaseVelocity = Vector3.zero;
    }
}
