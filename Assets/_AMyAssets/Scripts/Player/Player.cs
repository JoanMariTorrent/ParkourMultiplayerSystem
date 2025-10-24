using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    PlayerInputsAction _inputActions;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        playerCamera.gameObject.SetActive(isOwner);

        if (isOwner)
        {
            _inputActions = new PlayerInputsAction();
            _inputActions.Enable();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        playerCharacter.Intialize();
        playerCamera.Intialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
    }


    private void OnDisable()
    {
        if (_inputActions != null)
            _inputActions.Dispose();
    }
    void Update()
    {
        if (isOwner)
        {
            HandleInput();
            playerCharacter.SyncStateToNetwork();
        }

        playerCharacter.UpdateBody(Time.deltaTime);        
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        if (isOwner)
        {
            playerCamera.UpdatePosition(cameraTarget);
            cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
            cameraLean.UpdateLean
            (
                deltaTime,
                state.Stance is Stance.Slide,
                state.Acceleration,
                cameraTarget.up
            );
        }
    }





    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }






    private void HandleInput()
    {
        var input = _inputActions.GamePlay;

        // Pilla camera input y actualiza su rotacion
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        //Detectar cambio de arma
        int requestedGun = 0;
        if (input.ChangeGun.triggered)
        {
            var controlName = input.ChangeGun.activeControl?.displayName; // 1, 2, 3...
            if (int.TryParse(controlName, out int parsed))
            {
                requestedGun = parsed;
            }
        }

        // Pilla el character input y lo actualiza
        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
                ? CrouchInput.Toggle
                : CrouchInput.None,
            Shoot = input.Shoot.IsPressed(),
            ShootThisFrame = input.Shoot.triggered,
            Aim = input.Aim.IsPressed(),
            ChangeGun = requestedGun > 0,
            RequestedGunIndex = requestedGun
        };

        playerCharacter.UpdateInput(characterInput);

#if UNITY_EDITOR
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }
#endif
    }


}
