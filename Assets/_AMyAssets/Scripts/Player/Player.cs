using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using Unity.VisualScripting;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] PlayerInputsAction _inputActions;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        playerCamera.gameObject.SetActive(isOwner);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        playerCharacter.Intialize();
        playerCamera.Intialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
        
        if (isOwner)
        {
            _inputActions = new PlayerInputsAction();
            _inputActions.Enable();
        }
    }


    private void OnDisable()
    {
        _inputActions.Dispose();
    }
    void Update()
    {
        if (isOwner)
        {
            HandleInputs();
        }
        
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();
        
        playerCharacter.UpdateBody(deltaTime);
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



    private void HandleInputs()
    {
        var input = _inputActions.GamePlay;
        float deltaTime = Time.deltaTime;

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
            RequestedGunIndex = requestedGun,
            Running = input.Running.IsPressed(),
            Interact = input.Interact.WasPressedThisFrame(),
            Reload = input.Reload.WasPressedThisFrame(),
            DropGun = input.DropGun.WasPressedThisFrame(),

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





    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }


}
