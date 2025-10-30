using System.Collections;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;
using Steamworks;
using System.Security;
using UnityEngine.Rendering;
using Interfaces;

public enum WeaponID
{
    None,
    Pistol,
    RifleMalPorro,
    Grenade,
}

public enum WeaponType
{
    None,
    Primary,
    Secundary
}

public class Gun : NetworkBehaviour, ITakeGun
{
    [Header("Weapon Info")]
    public WeaponID weaponID;
    public WeaponType weaponType;
    public string displayName;
    [Space]
    [Header("Child meshes")]
    [SerializeField] private GameObject[] childMeshes;

    [Header("GunType")]
    [SerializeField] private bool _normalGun;
    [SerializeField] private bool _knife;
    [SerializeField] private bool _grenade;
    [SerializeField] private bool _automatic;
    [SerializeField] private bool _modificable;

    [Header("Stats")]
    [SerializeField] private int ammo;
    private int maxAmmo;
    [SerializeField] private float timeToReload = 3f;
    [SerializeField] private float _range = 20f;
    [SerializeField] private int _gunDamage = 10;
    [SerializeField] private float _fireRate = 0.5f;


    [Header("Recoil")]
    [SerializeField] private float _recoilStrenght = 1f;
    [SerializeField] private float _recoilDuration = 0.2f;
    [SerializeField] private AnimationCurve _recoilCurve;
    [SerializeField] private AnimationCurve _rotationCurve;
    [SerializeField] private float _rotationAmount = 25f;

    [Header("Grenades")]
    [SerializeField] private float _timeCharged;
    [SerializeField] private float _timeToCharge;
    [SerializeField] private float _maxTimeCharge;
    [SerializeField] private float _grenadeForce;
    bool grenadeCharged = false;
    public bool grenadeThrowed = false;

    [Header("References")]
    public Rigidbody rb;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private List<Renderer> _renderers = new();
    [SerializeField] private ParticleSystem _enviormentHit, _playerHitEffect;
    [SerializeField] private RecoilCamera recoilCamera;
    [SerializeField] private int ownerGunTag, otherPlayerGunTag;

    [Header("GunRecoil")]
    [Header("normal recoil")]
    public float recoilX;
    public float recoilY;
    public float recoilZ;
    [Space(0.2f)]
    [Header("aiming recoil")]
    public float aimRecoilX;
    public float aimRecoilY;
    public float aimRecoilZ;
    [Space(0.2f)]
    [Header("speed recoil")]
    public float returnSpeed;
    public float snappiness;
    [Space(1f)]



    [Header("Inspect")]
    [SerializeField] private bool scopeEquiped;
    [SerializeField] private MeshRenderer scopeMesh;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    private Coroutine _recoilCoroutine;
    private float _lastFireTime;
    private PlayerID _ownerID;

    private bool reloading;

    private WeaponManager weaponManager;





    private bool _inspecting = false;
    private float _inspectSpeed = 5f;
    [SerializeField] private Vector3 _inspectPositionOffset = new Vector3();
    [SerializeField] private Vector3 _inspectRotationEuler = new Vector3();

    public bool equipedGun = false;


    private void Start()
    {
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        if (!isOwner)
        {
            enabled = false;
            gameObject.layer = otherPlayerGunTag;
        }
        else
        {
            enabled = true;
            gameObject.layer = ownerGunTag;
            maxAmmo = ammo;
        }

    }




    public void Update()
    {
        if (!isOwner) return;
        if (!equipedGun) return;



        HandleShooting();
    }


    public void Setup(Transform cameraTransform, LayerMask hitLayer, RecoilCamera recoil, PlayerCharacter playerChar, WeaponManager wm)
    {
        equipedGun = true;
        _cameraTransform = cameraTransform;
        _hitLayer = hitLayer;
        recoilCamera = recoil;
        playerCharacter = playerChar;
        reloading = false;
        weaponManager = wm;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (isOwner)
        {
            gameObject.layer = 9;
            if (childMeshes != null)
            {
                foreach (var i in childMeshes)
                {
                    i.layer = 9;
                }
            }

        }

        else
        {
            gameObject.layer = 10;
            if (childMeshes != null)
            {
                foreach (var i in childMeshes)
                {
                    i.layer = 10;
                }
            }

        }
    }

    public void SetDown()
    {
        equipedGun = false;
        _cameraTransform = null;
        recoilCamera = null;
        playerCharacter = null;
        reloading = false;
        gameObject.layer = 12;
        gameObject.transform.SetParent(null);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }



    public void HandleShooting()
    {
        if (!isOwner) return;
        if (reloading) return;

        if (_knife)
        {

        }
        else if (_normalGun)
        {
            // Si es automatica y no mantiene el click o no es automatica y no pulsa el click, se sale de la funcion
            if (_automatic && !playerCharacter._requestedShoot || !_automatic && !playerCharacter._requestedShootThisFrame) return;

            // si el ultimo disparo mas el cooldown de disparo sumado, es mas grande que el tiempo que llevas sin disparar antes de darle al click se sale de la funcion
            if (_lastFireTime + _fireRate > Time.unscaledTime) return;

            if (ammo <= 0)
            {
                Reload();
                return;
            }

            ammo--;

            _lastFireTime = Time.unscaledTime;

            ShootServerRpc(_cameraTransform.position, _cameraTransform.forward);
        }

        else if (_grenade)
        {
            if (playerCharacter._requestedShoot && !grenadeThrowed)
            {
                Debug.Log("Cargando granada");
                _timeCharged += Time.deltaTime;

                if (_timeCharged >= _timeToCharge && _timeCharged < _maxTimeCharge)
                {
                    grenadeCharged = true;
                }
                if (_timeCharged >= _maxTimeCharge)
                {
                    Debug.Log("BOOM!");
                    _timeCharged = 0;
                    grenadeCharged = false;
                    return;
                }

            }
            if (playerCharacter._requestedShoot && grenadeCharged && !grenadeThrowed)
            {
                grenadeCharged = false;
                Debug.Log("Throwing grenade");

                Rigidbody rbGrenade = GetComponent<Rigidbody>();
                if (rbGrenade == null)
                {
                    Debug.LogError("rbGrenade is null!");
                    return;
                }
                RpcThrowGrenade(_cameraTransform.forward, _grenadeForce);

            }
        }

    }

    private void ShootServerRpc(Vector3 origin, Vector3 direction)
    {
        Debug.Log("ShootServerRpc");
        if (recoilCamera != null)
            recoilCamera.RecoilFire();

        //Lanza un raycast, si no le da a nada, return
        if (!Physics.Raycast(origin, direction, out var hit, _range, _hitLayer))
        {
            PlayShotEffectObserversRpc();
            return;
        }

        //Mira si la colision que ha tocado el raycast contiene un PlayerHealth, si lo tiene hace todo el sistema de quitarle vida, VFX, a�adirle da�o al ScoreManager...

        if (hit.transform.TryGetComponent(out PlayerHealth victim))
        {
            ApplyDamageServerRpc(victim, _gunDamage);
            PlayShotEffectObserversRpc();
            PlayerHitObserversRpc(victim, victim.transform.InverseTransformPoint(hit.point), hit.normal);
        }

        // Si no tiene el script HealthManager, se hace el VFX de mapa
        else
        {
            PlayShotEffectObserversRpc();
            EnviormentHitObserversRpc(hit.point, hit.normal);

        }

        Debug.Log(hit.transform.name);
    }


    [ServerRpc]
    private void ApplyDamageServerRpc(PlayerHealth victim, int gunDamage)
    {
        Debug.Log("ApplyDamageServerRpc");
        victim.ChangeHealth(-gunDamage);
        if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            scoreManager.AddDamageServerRpc(victim.PlayerID, gunDamage);
        }

    }


    private PlayerHealth FindPlayerByID(PlayerID id)
    {
        Debug.Log("FindPlayerByID");
        foreach (var player in FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player.PlayerID == id)
                return player;
        }
        return null;
    }

    [ObserversRpc(runLocally: false)]
    private void PlayerHitObserversRpc(PlayerHealth player, Vector3 localposition, Vector3 normal)
    {
        if (_playerHitEffect && player && player.transform)
        {
            var effect = Instantiate(_playerHitEffect, player.transform.TransformPoint(localposition), Quaternion.LookRotation(normal));
            effect.Play();
        }
    }


    [ObserversRpc(runLocally: false)]
    private void EnviormentHitObserversRpc(Vector3 position, Vector3 normal)
    {
        if (_enviormentHit)
        {
            var effect = Instantiate(_enviormentHit, position, Quaternion.LookRotation(normal));
            effect.Play();
        }
    }



    [ObserversRpc(runLocally: false)]
    private void PlayShotEffectObserversRpc()
    {
        if (_muzzleFlash)
            _muzzleFlash.Play();
        if (_recoilCoroutine != null)
            StopCoroutine(_recoilCoroutine);

        if (!gameObject.activeInHierarchy)
            return;

        if (gameObject.layer == otherPlayerGunTag)
            return;

        _recoilCoroutine = StartCoroutine(PlayRecoil());
    }

    private IEnumerator PlayRecoil()
    {
        float elapsed = 0f;

        while (elapsed < _recoilDuration)
        {
            elapsed += Time.deltaTime;
            float curveTime = elapsed / _recoilDuration;

            //position recoil
            float recoilValue = _recoilCurve.Evaluate(curveTime);
            Vector3 recoilOffset = Vector3.back * (recoilValue * _recoilStrenght);
            transform.localPosition = _originalPosition + recoilOffset;

            //rotation recoil
            float rotationValue = _rotationCurve.Evaluate(curveTime);
            Vector3 rotationOffset = new Vector3(rotationValue * _rotationAmount, 0f, 0f);
            transform.localRotation = _originalRotation * Quaternion.Euler(rotationOffset);

            yield return null;
        }

        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }

    [ObserversRpc(runLocally: false)]
    private void RpcThrowGrenade(Vector3 forward, float grenadeForce)
    {
        grenadeThrowed = true;

        // Desemparentar la granada
        transform.parent = null;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody de la granada no encontrado!");
            return;
        }

        // Aplicar fuerza
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 20);
        rb.AddForce(forward * grenadeForce);

        // Notificar al WeaponManager localmente
        WeaponManager wm = GetComponentInParent<WeaponManager>();
        wm?.UtilityThrowed();

        // Inicia la coroutine local para explosión y destrucción
        StartCoroutine(GrenadeCoroutine());
    }

    private IEnumerator GrenadeCoroutine()
    {
        yield return new WaitForSeconds(0.8f);

        // Activa física inactiva localmente
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Debug.Log("BOOM!!!!!");

        yield return new WaitForSeconds(3);

        // Destruye la granada localmente
        Destroy(gameObject);
    }

    [ObserversRpc(runLocally: false)]
    public void Reload()
    {
        if (ammo < maxAmmo)
        {
            reloading = true;
            StartCoroutine(CoroutineReload());
        }
    }

    private IEnumerator CoroutineReload()
    {
        yield return new WaitForSeconds(timeToReload);
        ReloadFinished();
    }

    [ObserversRpc(runLocally: false)]
    public void ReloadFinished()
    {
        ammo = maxAmmo;
        reloading = false;
    }

    public void TakeGun()
    {
        if (weaponManager == null)
        {
            Debug.LogAssertionFormat("Trying to take some gun, but the weaponManager is null!");
            return;
        }

        if (weaponType == WeaponType.Primary)
        {
            weaponManager.NewWeapon(gameObject, true, false, true);
        }
        else if (weaponType == WeaponType.Secundary)
        {
            weaponManager.NewWeapon(gameObject, false, false, true);
        }
    }


}
