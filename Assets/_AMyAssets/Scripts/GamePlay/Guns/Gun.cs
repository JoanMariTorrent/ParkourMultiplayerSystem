using System.Collections;
using System.Linq;
using Interfaces;
using PurrNet;
using UnityEngine;

public enum WeaponID { None, PistolaSimple, RifleMalPorro, Ojo, LanzaCigarros, FlameThrower, Railgun }
public enum WeaponType { None, Primary, Secundary } 
public enum AimType {Normal, Aiming, Sniper}

public class Gun : EquippableItem, ITakeGun
{
    [Header("Base Info")]
    public WeaponID weaponID;
    public WeaponType weaponType;
    public AimType aimType;
    public string displayName;
    public Transform leftHandGrip;
    public Transform rightHandGrip;
    public WeaponAnimationData animData;

    [Header("Stats")]
    [SerializeField] protected SyncVar<int> _ammo = new SyncVar<int>(30);
    [SerializeField] protected SyncVar<int> _reloadsAmmo = new SyncVar<int>(90);
    [SerializeField] protected int maxAmmo = 30;
    
    [SerializeField] protected bool _automatic; 
    [SerializeField] protected float _fireRate = 0.5f;
    [SerializeField] protected float timeToReload = 3f;
    [SerializeField] protected int _gunDamage = 10;

    [Header("Aiming system")]
    public bool canAim = true;
    public AnimationCurve aimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float timeToAim = 0.15f;
    
    public float aimingFOV = 50f;
    public float gunAimingFOV = 50f;
    public bool isAiming;

    [Header("Visuals & Audio")]
    [SerializeField] protected Transform _cameraTransform;
    [SerializeField] protected Transform shootTransform; 
    [SerializeField] protected ParticleSystem _muzzleFlash;
    [SerializeField] protected BulletTracer tracerPrefab;
    [SerializeField] protected AudioClip shootSound;
    [SerializeField] protected AudioClip hitMarker;
    [SerializeField] protected float minPitch = 0.8f, maxPitch = 1.2f;
    [SerializeField] protected GameObject[] childMeshes;

    [Header("Recoil System")]
    [SerializeField] protected RecoilCamera recoilCamera;
    [SerializeField] protected float _recoilStrenght = 1f;
    [SerializeField] protected float _recoilDuration = 0.2f;
    [SerializeField] protected AnimationCurve _recoilCurve;
    [SerializeField] protected AnimationCurve _rotationCurve;
    [SerializeField] protected float _rotationAmount = 25f;

    [Header("Layers")]
    [SerializeField] protected int ownerGunTag = 7;
    [SerializeField] protected int otherPlayerGunTag = 8;

    [Header("Spread System")]
    [SerializeField] protected float spreadX = 0.05f; 
    [SerializeField] protected float spreadY = 0.05f;
    [SerializeField] protected float movingSpreadX = 0.05f;
    [SerializeField] protected float movingSpreadY = 0.05f;

    [Header("Camera Recoil Stats")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    [Space]
    public float movingRecoilX;
    public float movingRecoilY;
    public float movingRecoilZ;
    [Space]
    public float aimRecoilX;
    public float aimRecoilY;
    public float aimRecoilZ;
    [Space]
    public float snappiness = 10f;
    public float returnSpeed = 20f;

    [Header("World settings")]
    public Vector3 initPos = Vector3.zero;
    [SerializeField] private Vector3 equippedScale = Vector3.one; 
    [SerializeField] private Vector3 droppedScale = new Vector3(1, 1, 1);
    
    [Header("Extras")]
    public bool IsReloading => reloading;

    

    // Estado Interno
    public PlayerCharacter playerCharacter;
    protected Player player;
    protected PlayerAnimationHandler animHandler;
    public Animator gunAnimHandler;
    protected WeaponManager weaponManager;
    protected GameMainView gameMainView;
    public Rigidbody rb;
    
    protected float _lastFireTime;
    protected bool reloading;
    public bool equipedGun = false;
    
    protected Vector3 _originalPosition;
    protected Quaternion _originalRotation;
    protected Coroutine _recoilCoroutine;
    protected Coroutine _reloadCoroutine;

    // --- SETUP ---

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        _originalPosition = initPos;
        _originalRotation = transform.localRotation;

        int targetLayer = isOwner ? ownerGunTag : otherPlayerGunTag;
        SetLayerRecursive(gameObject, targetLayer);
    }

    void OnDisable()
    {
        if(!isOwner) return;
        if(gunAnimHandler != null) gunAnimHandler.enabled = false;
        if(animHandler != null) animHandler.UnRegisterWeaponAnimator();
        if(_reloadCoroutine != null) 
        {
            Debug.Log("CANCELANDO RELOAD");
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
    }

    public virtual void Setup(Transform cam, LayerMask mask, RecoilCamera rec, PlayerCharacter pc, Player p, WeaponManager wm, PlayerAnimationHandler _animHandler)
    {
        transform.localScale = equippedScale;

        this.enabled = true;
        this.equipedGun = true;
        this._cameraTransform = cam;
        this.recoilCamera = rec;
        this.playerCharacter = pc;
        this.player = p;
        this.weaponManager = wm;
        this.reloading = false;
        this.animHandler = _animHandler;
        if(gunAnimHandler != null) animHandler.RegisterWeaponAnimator(gunAnimHandler, animData);
        if(gunAnimHandler != null) gunAnimHandler.enabled = true;
        

        // Reset de posición
        transform.localPosition = initPos;
        transform.localRotation = Quaternion.identity;
        _originalPosition = initPos;
        _originalRotation = transform.localRotation;

        // --- ARREGLO DE LA UI ---
        if (p != null && p.isOwner && p.canvas != null)
        {
            gameMainView = p.canvas.gameMainView;
            UpdateAmmoUI(); 
        }
        else
        {
            gameMainView = null; 
        }

        
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        int targetLayer = (p != null && p.isOwner) ? ownerGunTag : otherPlayerGunTag;
        SetLayerRecursive(gameObject, targetLayer);
    }

    public void SetDown()
    {
        transform.localScale = droppedScale;
        equipedGun = false;
        reloading = false;

        gameMainView = null;

        if(gunAnimHandler != null) gunAnimHandler.enabled = false;
        if(gunAnimHandler != null) animHandler.UnRegisterWeaponAnimator();

        transform.SetParent(null);
        var col = GetComponent<Collider>();
        if (col) col.enabled = true;
        enabled = false;
        SetLayerRecursive(gameObject, 12); 
    }

    // --- UPDATE & INPUT ---

    protected virtual void Update()
    {
        if (!equipedGun) return;
    
        // 1. LÓGICA PARA TODOS (Dueño y Observadores)
        if (_recoilCoroutine == null)
        {
            transform.localPosition = _originalPosition;
            transform.localRotation = _originalRotation;

            if(transform.localScale != equippedScale)
                transform.localScale = equippedScale;
        }
    
        // 2. LÓGICA SOLO PARA EL DUEÑO
        if (isOwner && !reloading)
        {
            HandleInput();
    
            if(Input.GetKeyDown(KeyCode.J))
                _ammo.value += 20;
        }
    }

    /// <summary>
    /// Calcula la dirección del disparo aplicando dispersión si no se está apuntando.
    /// </summary>
    protected Vector3 GetShootingDirection()
    {
        Vector3 targetDir = _cameraTransform.forward;

        if (!isAiming)
        {
            bool isMoving = false;
            if(playerCharacter != null)
            {
                Vector3 velocity = playerCharacter._state.Velocity;

                velocity.y = 0;

                isMoving = velocity.magnitude > 0.4f;
            }


            float randomX;
            float randomY;
            if(!isMoving)
            {
                randomX = Random.Range(-spreadX, spreadX);
                randomY = Random.Range(-spreadY, spreadY);
            }
            else
            {
                randomX = Random.Range(-movingSpreadX, movingSpreadX);
                randomY = Random.Range(-movingSpreadY, movingSpreadY);
            }
            

            targetDir += _cameraTransform.right * randomX + _cameraTransform.up * randomY;
            
            targetDir.Normalize();
        }

        return targetDir;
    }


    protected virtual void HandleInput()
    {
        if (playerCharacter == null) return;
        bool wantsShoot = _automatic ? playerCharacter._requestedShoot : playerCharacter._requestedShootThisFrame;

        if (wantsShoot)
        {
            if (Time.unscaledTime >= _lastFireTime + _fireRate)
            {
                AttemptShoot();
            }
        }
    }

    protected void AttemptShoot()
    {
        if (_ammo.value <= 0)
        {
            Reload();
            return;
        }

        _lastFireTime = Time.unscaledTime;

        if (animHandler != null && gunAnimHandler != null)
        {
            animHandler.TriggerShoot();
        }

        // Rollback collider
        double tick = InstanceHandler.NetworkManager.tickModule.rollbackTick;

        Vector3 finalDirection = GetShootingDirection();

        RequestShootServerRpc(_cameraTransform.position, finalDirection, tick);
    }

    // --- RED Y DISPARO ---

    // AQUI DEBERIA DE IR EL ServerRPC PERO SI LO PONGO EL JUGADOR 2 NO LE PUEDE DISPARAR AL JUGADOR 1, ASI QUE DE MOMENTO LO QUITO PERO SE TIENE QUE ARREGLAR.
    private void RequestShootServerRpc(Vector3 pos, Vector3 dir, double tick)
    {
        changeAmmo();
        PlayEffectsObserversRpc(); 
        ExecuteShootingLogic(pos, dir, tick); 
    }

    [ServerRpc(requireOwnership: false)] 
    private void changeAmmo()
    {
        if (_ammo.value <= 0) return;

        _ammo.value--;
    }

    protected virtual void ExecuteShootingLogic(Vector3 position, Vector3 direction, double tick) { }

    // --- EFECTOS ---

    [ObserversRpc(runLocally: true)]
    protected void PlayEffectsObserversRpc()
    {
        if (shootSound) AudioManager.Instance.PlaySound(shootSound, transform.position, AudioType.SFX ,0.2f, pitch: Random.Range(minPitch, maxPitch));
        if (_muzzleFlash) _muzzleFlash.Play();
        if (isOwner && recoilCamera) recoilCamera.RecoilFire();

        if (gameObject.activeInHierarchy && equipedGun)
        {
            if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
            _recoilCoroutine = StartCoroutine(PlayRecoil());
        }
    }

    protected IEnumerator PlayRecoil()
    {
        float elapsed = 0f;
        while (elapsed < _recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _recoilDuration;
            
            float rVal = _recoilCurve.Evaluate(t) * _recoilStrenght;
            Vector3 posOffset = Vector3.back * rVal;
            
            float rotVal = _rotationCurve.Evaluate(t) * _rotationAmount;
            Vector3 rotOffset = new Vector3(rotVal, 0, 0);

            transform.localPosition = _originalPosition + posOffset;
            transform.localRotation = _originalRotation * Quaternion.Euler(rotOffset);
            yield return null;
        }
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }

    // --- RECARGA ---

    [ObserversRpc(runLocally: true)]
    public void Reload()
    {
        if (_reloadsAmmo.value > 0 && _ammo.value < maxAmmo)
        {
            reloading = true;
            if(_reloadCoroutine == null) _reloadCoroutine = StartCoroutine(ReloadCoroutine());
        }
    }

    IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(timeToReload);
        FinishReloadServerRpc();
        _reloadCoroutine = null;
    }

    [ServerRpc]
    void FinishReloadServerRpc()
    {
        int needed = maxAmmo - _ammo.value;
        int available = _reloadsAmmo.value;
        int toAdd = Mathf.Min(needed, available);
        _ammo.value += toAdd;
        _reloadsAmmo.value -= toAdd;
        FinishReloadObserversRpc();
    }

    [ObserversRpc]
    void FinishReloadObserversRpc() { reloading = false; UpdateAmmoUI(); }

    // --- UTILIDADES ---

    [ObserversRpc(runLocally: true)]
    public void TakeGun(PlayerCharacter pc)
    {
        var wm = pc.GetComponent<WeaponManager>();
        if (isServer) wm.PickupItem(gameObject);
        else if (isOwner) wm.RequestPickupItemServerRpc(gameObject);
    }

    protected void UpdateAmmoUI()
    {
        if (gameMainView != null) 
        {
            gameMainView.UpdateAmmo(_ammo.value, _reloadsAmmo.value);
        }
    }

    protected void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        if (childMeshes != null) foreach (var mesh in childMeshes) if(mesh) mesh.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }
    
    protected override void OnSpawned() 
    { 
        base.OnSpawned(); 
        _ammo.onChanged += (v) => UpdateAmmoUI(); 
        _reloadsAmmo.onChanged += (v) => UpdateAmmoUI(); 
    }


    [ServerRpc] public void GetAmmo(int ammo)
    {
        _ammo.value += ammo;
    }


    /// <summary>
    /// Genera la bala visual desde el cañón hasta el punto objetivo.
    /// Úsala en tus scripts hijos (Hitscan, Sniper) dentro de un ObserverRpc.
    /// </summary>
    protected void SpawnTracer(Vector3 hitPosition)
    {
        if (tracerPrefab == null || shootTransform == null) return;

        var tracerObj = Instantiate(tracerPrefab, shootTransform.position, Quaternion.identity);
        tracerObj.Init(shootTransform.position, hitPosition);
    }

    protected void HitMarker(bool lastHit)
    {
        if(player == null) return;
        player.canvas.gameMainView.HitMarker(lastHit);

        if(lastHit)
            player.canvas.gameMainView.RequestKillAnimation();
    }


    

}