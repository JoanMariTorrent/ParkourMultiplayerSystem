using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private SyncVar<int> _health = new(100);
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _selfLayer, _otherLayer;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject deathVFX;

    [Header("Componentes para Desactivar/Activar")]
    [SerializeField] private List<GameObject> visualObjectsList;
    [SerializeField] private Collider colliderToDisable;
    [SerializeField] private Rigidbody _rb;
    public bool IsDead => _health.value <= 0;

    [Header("Audios")]
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] deathClips;

    public Action<PlayerID> OnDeath_Server;
    public PlayerID PlayerID => owner.Value;

    public int health => _health.value;


    protected override void OnSpawned()
    {
        base.OnSpawned();

        var _actualLayer = isOwner ? _selfLayer : _otherLayer;
        SetLayerRecursive(gameObject, _actualLayer);


        if (isOwner)
        {
            _health.onChanged += OnHealthChanged;
            if(canvas != null && canvas.gameMainView != null)
                canvas.gameMainView.UpdateHealth(_health.value);
            
        }
    }


    protected override void OnDestroy()
    { 
        base.OnDestroy();

        _health.onChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int _newHealth)
    {
        canvas.gameMainView.UpdateHealth(_newHealth);
    }

    private void SetLayerRecursive(GameObject _obj, int _layer)
    {
        _obj.layer = _layer;

        foreach (Transform child in _obj.transform)
        { 
            SetLayerRecursive(child.gameObject, _layer);
        }
    }


    [ServerRpc(requireOwnership:false)]
    public void ChangeHealth(int _amount, PlayerID? attackerID = null)
    {
        if (IsDead) return;
        if (playerCharacter.GodMode) return;

        _health.value += _amount;
        //Debug.Log(_amount);

        if(_health.value > 0)
        {
            if (damageClips.Length > 0)
            {
                AudioClip damageClip = damageClips[UnityEngine.Random.Range(0, damageClips.Length)];
                AudioManager.Instance.PlaySound(damageClip, transform.position, 1.2f, UnityEngine.Random.Range(0.95f, 1.05f));
            }
        }

        if (IsDead)
        {
            AudioClip deathClip = deathClips[UnityEngine.Random.Range(0, deathClips.Length)];
            AudioManager.Instance.PlaySound(deathClip, transform.position, 1.2f, UnityEngine.Random.Range(0.95f, 1.05f));

            if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                if(attackerID != null || attackerID.HasValue) scoreManager.Addkills(attackerID.Value);
                if(owner.HasValue)
                scoreManager.AddDeath(owner.Value);
            }

            if (weaponManager != null)
            {
                weaponManager.DropAllWeaponsOnDeath();
            }

            
            DieVisualsObserversRpc();
            OnDeath_Server?.Invoke(owner.Value);
        }


    }

    [ObserversRpc(runLocally: true)]
    public void DieVisualsObserversRpc()
    {
        if (deathVFX)
        {
            Vector3 spawnVFX = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            Instantiate(deathVFX, spawnVFX, Quaternion.identity);
        }

        if (colliderToDisable) colliderToDisable.enabled = false;

        foreach(var obj in visualObjectsList)
        {
            if(obj != null) obj.SetActive(false);
        }
    }


    [ObserversRpc(runLocally: true)]
    public void ReviveObserversRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (isServer) _health.value = _maxHealth;

        if (playerCharacter != null)
        {
            playerCharacter.TeleportTo(spawnPos, spawnRot);
        }
        else
        {
            transform.position = spawnPos;
            transform.rotation = spawnRot;
        }

        if (colliderToDisable) colliderToDisable.enabled = true;

        foreach(var obj in visualObjectsList)
        {
            if(obj != null) obj.SetActive(true);
        }

    }
}
