using UnityEngine;
using KinematicCharacterController;
using Unity.Mathematics;


public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public CrouchInput Crouch;
}

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float standheight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    private Stance _stance;


    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedCrouch;
    private Collider[] _unCrouchOverlapResults;

    public void Intialize()
    {
        _stance = Stance.Stand;
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
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
    }

    public void UpdateBody()
    {
        var currentHeight = motor.Capsule.height;
        var normalizeHeight = currentHeight / standheight;

        var cameraTargetHeight = currentHeight *
        (
            _stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight
        );
        var rootTargetScale = new Vector3(1f, normalizeHeight, 1f);

        cameraTarget.localPosition = new Vector3(0f, cameraTargetHeight, 0f);
        root.localScale = rootTargetScale;
    }



    public void AfterCharacterUpdate(float deltaTime)
    {
        //UnCrouch
        if (!_requestedCrouch && _stance is not Stance.Stand)
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
            }
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        //Crouch
        if (_requestedCrouch && _stance is Stance.Stand)
        {
            _stance = Stance.Crouch;
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

    public void PostGroundingUpdate(float deltaTime) { }

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

            var speed = _stance is Stance.Stand
                ? walkSpeed
                : crouchSpeed;

            currentVelocity = groundedMovement * speed;
        }
        //else, in the air
        else
        {
            // Gravity
            currentVelocity += motor.CharacterUp * gravity * deltaTime;
        }

        if (_requestedJump)
        {
            _requestedJump = false;

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
}
