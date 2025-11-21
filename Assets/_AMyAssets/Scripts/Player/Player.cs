using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using Steamworks;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerHealth playerHealth;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] private PlayerInputsAction _inputActions;
    [SerializeField] private string playerName;
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private bool isSpinning = false;
    public Canvas canvas;
    public SlotMachine slotMachine;
    public bool canMove;
    public bool prueba = false;
    [SerializeField] private PruebasRPC pruebasRPC;
    [SerializeField] private WeaponDatabase weaponDataBase;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        PlayerRegistry.AllPlayers.Add(this);
    }


    protected override void OnSpawned()
    {
        base.OnSpawned();
        playerCamera.gameObject.SetActive(isOwner);
        canvas.gameObject.SetActive(isOwner);

        if (isOwner)
        {
            
            //string steamName = SteamFriends.GetPersonaName();
            //CmdPlayerName(steamName);
        }

        pruebasRPC = FindFirstObjectByType<PruebasRPC>();
        pruebasRPC.players.Add(this);
    }

    [ServerRpc]
    private void CmdPlayerName(string newPlayerName)
    {
        playerName = newPlayerName;
        gameObject.name = $"Player: {playerName}";
        RpcSetPlayerName(newPlayerName);
    }

    [ObserversRpc]
    private void RpcSetPlayerName(string newPlayerName)
    {
        playerName = newPlayerName;
        gameObject.name = $"Player: {playerName}";
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
            if (_inputActions == null)
            {
                _inputActions = new PlayerInputsAction();
                _inputActions.Enable();
            }
        }
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

        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHealth.ChangeHealth(-10);
            Debug.Log($"Nueva vida del jugador: {playerHealth.health}");
        }
    }



    private void HandleInputs()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputsAction();
            _inputActions.Enable();
            if (_inputActions == null)
            {
                _inputActions = new PlayerInputsAction();
                _inputActions.Enable();
            }
        }
        var input = _inputActions.GamePlay;
        float deltaTime = Time.deltaTime;

        if (!canMove) return;

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


    public void DeleteGround()
    {
        Debug.Log("<color=blue> Player test </color>");
    }





    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }


    // En Player.cs
    [TargetRpc(requireServer: false)]
    public void TargetStartSpin(PlayerID target, int idWeapon, int[] filteredWeapons)
    {
        StartCoroutine(SpinCoroutine(target, idWeapon, filteredWeapons));
    }


    private IEnumerator SpinCoroutine(PlayerID target, int idWeapon, int[] filteredWeapons)
    {
        if (slotMachine == null)
        {
            slotMachine = canvas._allViews.OfType<SlotMachine>().FirstOrDefault();
        }

        if (slotMachine == null)
        {
            Debug.LogError($"SlotMachine no encontrada en Player {gameObject.name}");
            yield break;
        }

        var selectedWeapon = weaponDataBase.GetWeaponByID(idWeapon);
        List<WeaponScripteableObject> filteredWeaponsList = new();
        foreach (var id in filteredWeapons)
            filteredWeaponsList.Add(weaponDataBase.GetWeaponByID(id));

        

        isSpinning = true;
        slotMachine.GetComponent<CanvasGroup>().alpha = 1f;
        slotMachine.gameObject.SetActive(true);

        slotMachine.startSpin(selectedWeapon, filteredWeaponsList);
        while (slotMachine.finalWeapon == null)
        {
            Debug.Log("<color=red> Slot machine is running!");
            yield return null;
        }

        slotMachine.GetComponent<CanvasGroup>().alpha = 0f;
        slotMachine.gameObject.SetActive(false);

        NotifySpinFinished_ServerRPC(owner.Value, idWeapon);
    }


    [ServerRpc]
    public void NotifySpinFinished_ServerRPC(PlayerID playerID, int idWeapon)
    {
        var selectedWeapon = weaponDataBase.GetWeaponByID(idWeapon);
        var weaponManager = GetComponent<WeaponManager>();
        if(selectedWeapon.weaponType == WeaponScripteableType.Primary)
            weaponManager.NewWeapon(selectedWeapon.gunPrefab, true, false, false);
        else if (selectedWeapon.weaponType == WeaponScripteableType.Secondary)
            weaponManager.NewWeapon(selectedWeapon.gunPrefab, false, false, false);
        else if (selectedWeapon.weaponType == WeaponScripteableType.Utility)
            weaponManager.NewWeapon(selectedWeapon.gunPrefab, false, true, false);
        
        if(InstanceHandler.TryGetInstance(out SpawningGunsState spawningGunsState))
        {
            spawningGunsState.OnPlayerFinishedSpin(playerID);
        }
    }


}
