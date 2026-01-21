using System.Collections;
using System.Linq;
using Interfaces;
using PurrNet;
using UnityEngine;

public enum WeaponID { None, PistolaSimple, RifleMalPorro, Ojo, LanzaCigarros, FlameThrower, Railgun }
public enum WeaponType { None, Primary, Secundary } 

public class Gun : NetworkBehaviour, ITakeGun
{
    [Header("Base Info")]
    public WeaponID weaponID;
    public WeaponType weaponType;
    public string displayName;

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
    public float aimingFOV = 50f;
    public float gunAimingFOV = 100f;

    [Header("Visuals & Audio")]
    [SerializeField] protected Transform _cameraTransform;
    [SerializeField] protected Transform shootTransform; 
    [SerializeField] protected ParticleSystem _muzzleFlash;
    [SerializeField] protected AudioClip shootSound;
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
    [SerializeField] protected int ownerGunTag = 9;
    [SerializeField] protected int otherPlayerGunTag = 10;

    [Header("Camera Recoil Stats")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    [Space]
    public float aimRecoilX;
    public float aimRecoilY;
    public float aimRecoilZ;
    [Space]
    public float snappiness = 10f;
    public float returnSpeed = 20f;

    

    // Estado Interno
    protected PlayerCharacter playerCharacter;
    protected Player player;
    protected WeaponManager weaponManager;
    protected GameMainView gameMainView;
    public Rigidbody rb;
    
    protected float _lastFireTime;
    protected bool reloading;
    public bool equipedGun = false;
    
    protected Vector3 _originalPosition;
    protected Quaternion _originalRotation;
    protected Coroutine _recoilCoroutine;

    // --- SETUP ---

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;

        if (!isOwner)
        {
            enabled = false;
            SetLayerRecursive(gameObject, otherPlayerGunTag);
        }
        else
        {
            enabled = true;
            SetLayerRecursive(gameObject, ownerGunTag);
        }
    }

    public virtual void Setup(Transform cam, LayerMask mask, RecoilCamera rec, PlayerCharacter pc, Player p, WeaponManager wm)
    {
        this.enabled = true;
        this.equipedGun = true;
        this._cameraTransform = cam;
        this.recoilCamera = rec;
        this.playerCharacter = pc;
        this.player = p;
        this.weaponManager = wm;
        this.reloading = false;

        // Reset de posición
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;

        // --- ARREGLO DE LA UI ---
        if (p != null && p.isOwner && p.canvas != null)
        {
            gameMainView = p.canvas.GetComponentInChildren<GameMainView>();
            UpdateAmmoUI(); 
        }
        
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        int targetLayer = (p != null && p.isOwner) ? ownerGunTag : otherPlayerGunTag;
        SetLayerRecursive(gameObject, targetLayer);
    }

    // --- UPDATE & INPUT ---

    protected virtual void Update()
    {
        if (!isOwner || !equipedGun || reloading) return;
        HandleInput();
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
        RequestShootServerRpc(_cameraTransform.position, _cameraTransform.forward);
    }

    // --- RED Y DISPARO ---

    [ServerRpc] // AQUI DEBERIA DE IR EL ServerRPC PERO SI LO PONGO EL JUGADOR 2 NO LE PUEDE DISPARAR AL JUGADOR 1, ASI QUE DE MOMENTO LO QUITO PERO SE TIENE QUE ARREGLAR.
    private void RequestShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (_ammo.value <= 0) return;

        _ammo.value--;
        PlayEffectsObserversRpc(); 
        ExecuteShootingLogic(pos, dir); 
    }

    protected virtual void ExecuteShootingLogic(Vector3 position, Vector3 direction) { }

    // --- EFECTOS ---

    [ObserversRpc(runLocally: true)]
    protected void PlayEffectsObserversRpc()
    {
        if (shootSound) AudioManager.Instance.PlaySound(shootSound, transform.position, 0.2f, pitch: Random.Range(minPitch, maxPitch));
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
            StartCoroutine(ReloadCoroutine());
        }
    }

    IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(timeToReload);
        FinishReloadServerRpc();
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
        bool isPrimary = weaponType == WeaponType.Primary;
        if (isServer) wm.NewWeapon(gameObject, isPrimary, false, true);
        else if (isOwner) wm.RequestPickupGunServerRpc(gameObject, isPrimary, false);
    }

    public void SetDown()
    {
        equipedGun = false;
        reloading = false;
        transform.SetParent(null);
        var col = GetComponent<Collider>();
        if (col) col.enabled = true;
        enabled = false;
        SetLayerRecursive(gameObject, 12); 
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
}