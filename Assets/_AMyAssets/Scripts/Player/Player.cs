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
    PlayerInputsAction _inputActions;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _inputActions = new PlayerInputsAction();
        _inputActions.Enable();


        playerCharacter.Intialize();
        playerCamera.Intialize(playerCharacter.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
    }


    private void OnDisable()
    {
        _inputActions.Dispose();
    }
    void Update()
    {
        var input = _inputActions.GamePlay;
        float deltaTime = Time.deltaTime;

        // Pilla camera input y actualiza su rotacion
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

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
            Aim = input.Aim.IsPressed(),
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

    private void LateUpdate()
    {
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

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }


}
