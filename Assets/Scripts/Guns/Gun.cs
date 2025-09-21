using System;
using System.Collections;
using NUnit.Framework;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;

public class Gun : StateNode
{
    [Header("Stats")]
    [SerializeField] private float _range = 20f;
    [SerializeField] private int _gunDamage = 10;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private bool _automatic;

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


    private float _lastFireTime;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Coroutine _recoilCoroutine;

    private PlayerID _ownerID;

    private void Awake()
    {
        ToggleVisuals(false);
    }

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
            ToggleVisuals(true);
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        ToggleVisuals(false);
    }


    private void Start()
    {
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
    }

    private void ToggleVisuals(bool toggle)
    {
        foreach (var renderer in _renderers)
        { 
            renderer.enabled = toggle;
        }
    }

    protected override void OnSpawned()
    { 
        base.OnSpawned();

        enabled = isOwner;
    }




    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);

        if (!isOwner) return;

        if (_automatic && !Input.GetKey(KeyCode.Mouse0) || !_automatic && !Input.GetKeyDown(KeyCode.Mouse0)) return;
        if (_lastFireTime + _fireRate > Time.unscaledTime) return;


        PlayShotEffect();
        _lastFireTime = Time.unscaledTime;


        if (!Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out var hit, _range, _hitLayer)) return;
        if (!hit.transform.TryGetComponent(out PlayerHealth _playerHealth)) 
        {
            EnviormentHit(hit.point, hit.normal);
            return;
        }

        PlayerHit(_playerHealth, _playerHealth.transform.InverseTransformPoint(hit.point), hit.normal);

        _playerHealth.ChangeHealth(-_gunDamage);
        if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            if (hit.transform.TryGetComponent(out PlayerHealth victim))
            {
                scoreManager.AddDamageServerRpc(victim.PlayerID, _gunDamage);
            }
        }
    }

    [ObserversRpc(runLocally: true)]
    private void PlayerHit(PlayerHealth player, Vector3 localposition, Vector3 normal)
    {
        if (_playerHitEffect && player && player.transform)
        {
            var effect = Instantiate(_playerHitEffect, player.transform.TransformPoint(localposition), Quaternion.LookRotation(normal));
            effect.Play();
        }
    }


    [ObserversRpc(runLocally: true)]
    private void EnviormentHit(Vector3 position, Vector3 normal)
    {
        if (_enviormentHit)
        {
            var effect = Instantiate(_enviormentHit, position, Quaternion.LookRotation(normal));
            effect.Play();
        }
    }

    

    [ObserversRpc(runLocally:true)]
    private void PlayShotEffect()
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
