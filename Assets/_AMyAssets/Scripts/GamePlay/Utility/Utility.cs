using System.Collections;
using Interfaces;
using PurrNet;
using UnityEngine;

public enum UtilityID {None, SmokeGrenade, FlashBang, Stim, GrapplingHook}
public abstract class Utility : EquippableItem, ITakeGun
{
    [Header("Base info")]
    public UtilityID utilityID;
    public string displayName;
    public UtilityScriptableObject utilityData;

    [Header("Runtime Stats (Modified by ScriptableObject)")]
    public SyncVar<int> currentCharges = new SyncVar<int>();
    [SerializeField] protected float cooldownTime = 3f;
    [SerializeField] protected bool isInfinite = false;
    [SerializeField] protected Transform _spawnPoint;
    [Header("Charge System")]
    protected float chargeTime = 0.0f;
    protected bool throwOnRelease = false;
    
    // Estado interno
    protected bool isCharging = false; 
    protected float chargeStartTime;

    [Header("Visuals and audios")]
    [SerializeField] protected GameObject[] childMeshes;
    [SerializeField] protected AudioClip useSound;
    
    [Header("Layers")]
    [SerializeField] protected int ownerLayer = 7;
    [SerializeField] protected int otherPlayerLayer = 8;

    [Header("World settings")]
    [SerializeField] private Vector3 equippedScale = Vector3.one;
    [SerializeField] private Vector3 droppedScale = Vector3.one;
    public Vector3 initPos = Vector3.zero;

    



    // Referencias
    protected bool isInCooldown = false;
    protected PlayerCharacter playerCharacter;
    protected Player player;
    protected WeaponManager weaponManager;
    protected PlayerAnimationHandler animHandler;
    protected Transform cameraTransform;
    protected GameMainView gameMainView;

    public void SetUp(Transform cam, PlayerCharacter pc, Player p, WeaponManager wm, PlayerAnimationHandler _animHandler)
    {
        // Variables
        if(utilityData != null)
        {
            this.cooldownTime = utilityData.cooldown;
            this.isInfinite = utilityData.isInfinite;
            this.chargeTime = utilityData.chargeTime;
            this.throwOnRelease = utilityData.throwOnRelease;
            if (isServer) this.currentCharges.value = utilityData.maxCharges;
        }

        // Escalas y posiciones
        transform.localScale = equippedScale;
        transform.localPosition = initPos;
        this.cameraTransform = cam;
        if (_spawnPoint == null) _spawnPoint = cam;


        // Referencias
        this.playerCharacter = pc;
        this.player = p;
        this.weaponManager = wm;
        this.animHandler = _animHandler;

        // UI
        if(p != null && p.isOwner && p.canvas != null)
        {
            gameMainView = p.canvas.gameMainView;
            UpdateUI();
        }

        // Configuracion del mundo
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        int targetLayer = (p != null && p.isOwner) ? ownerLayer : otherPlayerLayer;
        SetLayerRecursive(gameObject, targetLayer);

        this.enabled = true;
    }

    public void SetDown()
    {
        transform.localScale = droppedScale;
        transform.localPosition = Vector3.zero;

        gameMainView = null;

        transform.SetParent(null);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero; 
        }

        this.enabled = false;

        SetLayerRecursive(gameObject, 12); 
    }

    protected virtual void Start()
    {
        // Asignar layer dependiendo de si es owner
        if (!isOwner)
        {
            enabled = false;
            SetLayerRecursive(gameObject, otherPlayerLayer);
        }
        else
        {
            enabled = true;
            SetLayerRecursive(gameObject, ownerLayer);
        }
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        currentCharges.onChanged += (v) => UpdateUI();
    }

    // INPUTS

    public override void UseItem(bool inputDown, bool inputHeld, bool inputUp)
    {
        if (isInCooldown) return;
        if (!isInfinite && currentCharges.value <= 0) return;

        if (!throwOnRelease)
        {
            if (inputDown) AttemptUse();
            return;
        }

        if (inputDown && !isCharging)
        {
            StartCharging();
        }

        if (inputUp && isCharging)
        {
            ReleaseAndThrow();
        }
    }

    protected void AttemptUse()
    {
        if (isInCooldown) return;
        if(!isInfinite && currentCharges.value <= 0) {DepleteRoutine(); return;}

        StartCoroutine(CooldownRoutine());

        RequestUseServerRpc(cameraTransform.position, cameraTransform.forward);
    }

    [ServerRpc(requireOwnership: false)]
    protected void RequestUseServerRpc(Vector3 position, Vector3 direction)
    {
        // Ammo
        if(!isInfinite)
        {
            if(currentCharges.value <= 0) {DepleteRoutine(); return;}
            currentCharges.value --;
        }

        // Ejecutar la logica
        ExecuteUtilityLogic(position, direction);


        // Visuales
        PlayUsageEffectsObserverRpc();

        SpendChargesServerRpc();

    }


    // --- MÉTODOS VIRTUALES (Para los hijos) ---

    protected abstract void ExecuteUtilityLogic(Vector3 position, Vector3 direction);


    [ObserversRpc]
    protected virtual void PlayUsageEffectsObserverRpc()
    {
        if (useSound) AudioManager.Instance.PlaySound(useSound, transform.position, AudioType.SFX);
    }


    // RUTINAS

    protected IEnumerator CooldownRoutine()
    {
        isInCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isInCooldown = false;
    }

    protected void DepleteRoutine()
    {
        if(!isServer) return;

        if (weaponManager != null)
        {
            weaponManager.RemoveUtility(this.gameObject);
            weaponManager.StartUtilityCooldown();
        }
    }

    // Soporte

    protected void UpdateUI()
    {
        if (gameMainView != null)
        {
            // Poner aqui el metodo de las UI
        }
    }

    protected void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        if (childMeshes != null) foreach (var mesh in childMeshes) if(mesh) mesh.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }

    protected void StartCharging()
    {
        isCharging = true;
        chargeStartTime = Time.time; 
    }

   protected void ReleaseAndThrow()
    {
        float timeHeld = Time.time - chargeStartTime;

        if (timeHeld < chargeTime) 
        {
            isCharging = false;
            return; 
        }

        isCharging = false;
        
        RequestUseServerRpc(cameraTransform.position, cameraTransform.forward);
        
        StartCoroutine(CooldownRoutine());
    }

    [ObserversRpc(runLocally: true)]
    public void TakeGun(PlayerCharacter pc)
    {
        var wm = pc.GetComponent<WeaponManager>();
        if (isServer) wm.PickupItem(gameObject);
        else if (isOwner) wm.RequestPickupItemServerRpc(gameObject);
    }

    [ServerRpc]
    public void SpendChargesServerRpc()
    {
        if(isInfinite) return;

        currentCharges.value --;

        if(currentCharges.value <= 0)
            DepleteRoutine();
    }


}
