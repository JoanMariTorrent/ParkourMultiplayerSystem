using System.Collections;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class Gun : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _range = 20f;
    [SerializeField] private int _gunDamage = 10;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private bool _automatic;
    [SerializeField] private bool _knife;
    [SerializeField] private bool _modificable;

    [Header("Recoil")]
    [SerializeField] private float _recoilStrenght = 1f;
    [SerializeField] private float _recoilDuration = 0.2f;
    [SerializeField] private AnimationCurve _recoilCurve;
    [SerializeField] private AnimationCurve _rotationCurve;
    [SerializeField] private float _rotationAmount = 25f;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private List<Renderer> _renderers = new();
    [SerializeField] private ParticleSystem _enviormentHit, _playerHitEffect;
    [SerializeField] private RecoilCamera recoilCamera;

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



    private bool _inspecting = false;
    private float _inspectSpeed = 5f; // Ajusta la velocidad
    [SerializeField] private Vector3 _inspectPositionOffset = new Vector3();
    [SerializeField] private Vector3 _inspectRotationEuler = new Vector3();


    private void Start()
    {
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
        enabled = isOwner;
        
    }

    




    public void Update()
    {

        HandleShooting();
        HandleMods();
        
    }


    public void Setup(Transform cameraTransform, LayerMask hitLayer, RecoilCamera recoil)
    {
        _cameraTransform = cameraTransform;
        _hitLayer = hitLayer;
        recoilCamera = recoil;
    }

    private void HandleMods()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            _inspecting = !_inspecting;
        }

        if (_inspecting)
        {
            // Interpola suavemente a la pose de inspecci�n
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(_inspectRotationEuler), Time.deltaTime * _inspectSpeed);

            transform.localPosition = Vector3.Lerp(transform.localPosition, _originalPosition + _inspectPositionOffset, Time.deltaTime * _inspectSpeed);

            
            if (Input.GetKeyDown(KeyCode.F))
            {
                scopeEquiped = !scopeEquiped;
                scopeMesh.enabled = scopeEquiped;
                
            }
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _originalRotation, Time.deltaTime * _inspectSpeed);

            transform.localPosition = Vector3.Lerp( transform.localPosition, _originalPosition, Time.deltaTime * _inspectSpeed);
        }
    }











    private void HandleShooting()
    {
        if (_knife)
        {

        }
        else
        {
            // Si es automatica y no mantiene el click o no es automatica y no pulsa el click, se sale de la funcion
            if (_automatic && !Input.GetKey(KeyCode.Mouse0) || !_automatic && !Input.GetKeyDown(KeyCode.Mouse0)) return;

            // si el ultimo disparo mas el cooldown de disparo sumado, es mas grande que el tiempo que llevas sin disparar antes de darle al click se sale de la funcion
            if (_lastFireTime + _fireRate > Time.unscaledTime) return;

            _lastFireTime = Time.unscaledTime;

            ShootServerRpc();
        }
    }


    [ObserversRpc]
    private void ShootServerRpc()
    {
        if (!isServer) return;

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _cameraTransform.forward;


        if (recoilCamera != null)
        {
            recoilCamera.RecoilFire();
        }
        //Lanza un raycast, si no le da a nada, return
        if (!Physics.Raycast(origin, direction, out var hit, _range, _hitLayer, QueryTriggerInteraction.Ignore))
        {
            PlayShotEffectObserversRpc();
            return;
        }

        //Mira si la colision que ha tocado el raycast contiene un PlayerHealth, si lo tiene hace todo el sistema de quitarle vida, VFX, a�adirle da�o al ScoreManager...

        if (hit.transform.TryGetComponent(out PlayerHealth victim))
        {
            victim.ChangeHealth(-_gunDamage);
            PlayShotEffectObserversRpc();
            PlayerHitObserversRpc(victim, victim.transform.InverseTransformPoint(hit.point), hit.normal);
            if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                scoreManager.AddDamageServerRpc(victim.PlayerID, _gunDamage);
            }
        }

        // Si no tiene el script HealthManager, se hace el VFX de mapa
        else
        {
            PlayShotEffectObserversRpc();
            EnviormentHitObserversRpc(hit.point, hit.normal);

        }
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

    

    [ObserversRpc(runLocally:false)]
    private void PlayShotEffectObserversRpc()
    {
        if(_muzzleFlash)
            _muzzleFlash.Play();
        if (_recoilCoroutine != null)
            StopCoroutine(_recoilCoroutine);

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
}
