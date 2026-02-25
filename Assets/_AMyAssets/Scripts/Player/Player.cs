using UnityEngine;
using PurrNet;
using Steamworks;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System;

public class Player : NetworkBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAnimationHandler animHandler;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    public PlayerInputsAction _inputActions;
    public string playerName;
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private bool isSpinning = false;
    public Canvas canvas;
    public SettingsSystem settingsSystem;
    public SlotMachine slotMachine;
    public bool canMove; 
    public bool cameraBlocked;
    public bool prueba = false;
    [SerializeField] private PruebasRPC pruebasRPC;
    [SerializeField] private WeaponDatabase weaponDataBase;
    [SerializeField] private UtilityDatabase utilityDatabase;

    [Space][Header("Settings")]
    [SerializeField] private SettingsData settings;
    [SerializeField] private Volume globalVolume;
    public bool cameraActive = true;

    [Space(20f)][Header("TUTORIAL")]
    public bool tutorialMode;

    

    //Sistema de guardado
    private string RebindFilePath => SavePathManager.GetPath("rebinds.json");

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
            
            string steamName = SteamFriends.GetPersonaName();
            CmdPlayerName(steamName);
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
            LoadInputOverrides();
            
            _inputActions.Enable();

            settingsSystem.Intialize(globalVolume);
            if(tutorialMode) canMove = true;
        }
    }
    void Update()
    {
        if (isOwner)
        {
            HandleInputs(); 

            playerCharacter.UpdateBody(Time.deltaTime);

            if(Input.GetKeyDown(KeyCode.H))
                playerHealth.ChangeHealth(-5);
        }
    }

    void LateUpdate()
    {
        if (isOwner)
        {
            var cameraTarget = playerCharacter.GetCameraTarget();
            var cameraInput = new CameraInput { Look = _inputActions.GamePlay.Look.ReadValue<Vector2>() };
            

            
            if(cameraActive && !cameraBlocked) 
                playerCamera.UpdateRotation(cameraInput);

            playerCamera.UpdatePosition(cameraTarget);


            // Weapon Sway / Spring
            cameraSpring.UpdateSpring(Time.deltaTime, cameraTarget.up);

            // Weapon Lean
            var state = playerCharacter.GetState();
            cameraLean.UpdateLean(
                Time.deltaTime,
                state.Stance is Stance.Slide,
                state.Acceleration,
                cameraTarget.up
            );
        }
    }


    private void HandleInputs()
    {
        //if (playerHealth.health <= 0) return;

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

        //if (!canMove) return;

        // Pilla camera input y actualiza su rotacion
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        if(cameraActive && !cameraBlocked) playerCamera.UpdateRotation(cameraInput);



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
            StopShooting = input.Shoot.WasReleasedThisFrame(),
            Aim = input.Aim.IsPressed(),
            ChangeGun = requestedGun > 0,
            RequestedGunIndex = requestedGun,
            Running = input.Running.IsPressed(),
            Interact = input.Interact.WasPressedThisFrame(),
            Reload = input.Reload.WasPressedThisFrame(),
            DropGun = input.DropGun.WasPressedThisFrame(),
            Emote = input.Emote.WasPressedThisFrame(),
            MovementBlocked = !canMove,
        };

        if(cameraActive) playerCharacter.UpdateInput(characterInput);

        
    }



    [TargetRpc(requireServer: false)]
    public void TargetShowFinalScreen(PlayerID target, bool win)
    {
        if(canvas == null) return;
        if(win)
        {
            canvas.ShowView<WinGameView>(true);
        }
        else
        {
            canvas.ShowView<LoseGameView>(true);
        }

        if(playerHealth == null) return;
        playerHealth.DieVisualsObserversRpc();
    }

    


    // En Player.cs
    [TargetRpc(requireServer: false)]
    public void TargetStartSpin(PlayerID target, int[] winners, int[] p1, int[] p2, int[] p3)
    {
        StartCoroutine(SpinCoroutine(target, winners, p1, p2, p3));
    }


    private IEnumerator SpinCoroutine(PlayerID target, int[] winners, int[] p1, int[] p2, int[] p3)
    {
        if (slotMachine == null)
        {
            slotMachine = canvas._allViews.OfType<SlotMachine>().FirstOrDefault();
        }

        if (slotMachine == null)
        {
            yield break;
        }
        canMove = false;
        playerHealth.SetImmunityRpc(true);
        isSpinning = true;


        slotMachine.GetComponent<CanvasGroup>().alpha = 1f;
        slotMachine.gameObject.SetActive(true);

        slotMachine.startMultiSpinFlat(winners, p1, p2, p3);

        while (!slotMachine.allFinished)
        {
            yield return null;
        }

        slotMachine.GetComponent<CanvasGroup>().alpha = 0f;
        slotMachine.gameObject.SetActive(false);

        for (int i = 0; i < winners.Length; i++)
        {
            int currentID = winners[i];

            if(currentID == -1) continue;

            bool isUtility = (i == 2);

            GiveGuncs_ServerRPC(owner.Value, currentID, isUtility);
        }

        NotifySpinFinished_ServerRPC(owner.Value);

        playerHealth.SetImmunityRpc(false);
        canMove = true;
    }


    [ServerRpc]
    public void NotifySpinFinished_ServerRPC(PlayerID playerID)
    {
        if(InstanceHandler.TryGetInstance(out SpawningGunsState spawningGunsState))
        {
            //spawningGunsState.OnPlayerFinishedSpin(playerID);
            if(SpawningGunsState.SpawningGunsStateActiveInstance != null)
            {
                SpawningGunsState.SpawningGunsStateActiveInstance.OnPlayerFinishedSpin(playerID);
            }
        }
        
    }

    [ServerRpc]
    public void GiveGuncs_ServerRPC(PlayerID playerID, int idWeapon, bool isUtility)
    {
        GiveGuns(idWeapon, isUtility);

        TargetSetArmsAnimations(playerID, idWeapon);
    }

    private void GiveGuns(int id, bool isUtility)
    {
        var weaponManager = GetComponent<WeaponManager>();
        if(weaponManager == null) return;

        if(isUtility)
        {
            var selected = utilityDatabase.GetUtilityByID(id);
            if(selected != null)
            {
                weaponManager.AddUtility(selected.utilityPrefab, false);
            }
        }
        else
        {
            var selected = weaponDataBase.GetWeaponByID(id);
            if(selected == null) return;

            if(selected.weaponType == WeaponScripteableType.Primary)
                weaponManager.NewWeapon(selected.gunPrefab, true, false, false);
            else if (selected.weaponType == WeaponScripteableType.Secondary)
                weaponManager.NewWeapon(selected.gunPrefab, false, false, false);
        }        
    }


    [TargetRpc]
    private void TargetSetArmsAnimations(PlayerID target, int idWeapon)
    {       
        var selectedWeapon = weaponDataBase.GetWeaponByID(idWeapon);
        var animHandler = GetComponentInChildren<PlayerAnimationHandler>();

        if (animHandler != null && selectedWeapon.animatorOverride != null)
        {
            //animHandler.SetWeaponAnimator(selectedWeapon.animatorOverride);
        }


        
    }

    #region INPUTS

    public void ApplyInputOverrides(string rebindsJson)
    {
        if(_inputActions == null) return;
        
        _inputActions.asset.LoadBindingOverridesFromJson(rebindsJson);
    }

    private void LoadInputOverrides()
    {
        if (System.IO.File.Exists(RebindFilePath))
        {
            try 
            {
                string jsonFile = System.IO.File.ReadAllText(RebindFilePath);
                _inputActions.asset.LoadBindingOverridesFromJson(jsonFile);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar rebinds en el Jugador: {e.Message}");
            }
        }
    }

    #endregion


    public void SettingsEnabled() { cameraActive = false; }

    public void SettingsDisabled() { cameraActive = true; }


}
