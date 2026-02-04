using System;
using System.Collections.Generic;
using PurrNet;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private SyncVar<int> _health = new(100);
    public SyncVar<bool> _canTakeDamage = new(true);
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _selfLayer, _otherLayer;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private Player player;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject deathVFX;

    [Header("Weapon Logic")]
    private WeaponsDataManager _weaponsData;
    [SerializeField] private WeaponDatabase _weaponDatabase;

    [Header("Componentes para Desactivar/Activar")]
    [SerializeField] private List<GameObject> othersPlayersVisuals;
    [SerializeField] private List<GameObject> selfPlayerVisuals;
    [SerializeField] private Collider colliderToDisable;
    [SerializeField] private Rigidbody _rb;
    public bool IsDead => _health.value <= 0;

    [Header("Audios")]
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] deathClips;

    public Action<PlayerID> OnDeath_Server;
    public PlayerID PlayerID => owner.Value;

    public int health => _health.value;


    void Start()
    {
        InstanceHandler.TryGetInstance(out WeaponsDataManager wm);
        _weaponsData = wm;
    }


    protected override void OnSpawned()
    {
        base.OnSpawned();

        var _actualLayer = isOwner ? _selfLayer : _otherLayer;
        SetLayerRecursive(gameObject, _actualLayer);


        if (isOwner)
        {
            _health.onChanged += OnHealthChanged;
            if(canvas != null && canvas.gameMainView != null)
                canvas.gameMainView.UpdateHealth(_health.value, _maxHealth);
            
        }
    }


    protected override void OnDestroy()
    { 
        base.OnDestroy();

        _health.onChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int _newHealth)
    {
        canvas.gameMainView.UpdateHealth(_newHealth, _maxHealth);
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
        if(!_canTakeDamage.value) return;
        if (playerCharacter.GodMode) return;

        _health.value += _amount;
        //Debug.Log(_amount);

        if(_health.value > 0)
        {
            if (damageClips.Length > 0 && isOwner)
            {
                AudioClip damageClip = damageClips[UnityEngine.Random.Range(0, damageClips.Length)];
                AudioManager.Instance.PlaySound2D(damageClip, AudioType.SFX , 1.2f, UnityEngine.Random.Range(0.95f, 1.05f));
            }
        }

        if (IsDead)
        {
            if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                if(attackerID != null || attackerID.HasValue) scoreManager.Addkills(attackerID.Value);
                if(owner.HasValue)
                    scoreManager.AddDeath(owner.Value);
            }

            if (weaponManager != null)
            {
                weaponManager.DropAllWeaponsOnDeath();
                if(isOwner) weaponManager._currentGun = null;
            }

            
            DieVisualsObserversRpc();
            OnDeath_Server?.Invoke(owner.Value);
        }


    }

    [ObserversRpc(runLocally: true)]
    public void DieVisualsObserversRpc()
    {

        if(isOwner)
        {
            AudioClip deathClip = deathClips[UnityEngine.Random.Range(0, deathClips.Length)];
            AudioManager.Instance.PlaySound(deathClip, transform.position, AudioType.SFX ,1.2f, UnityEngine.Random.Range(0.95f, 1.05f), parent: null);
            player.canMove = false;
            player.cameraBlocked = true;
        }

        if (deathVFX)
        {
            Vector3 spawnVFX = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            Instantiate(deathVFX, spawnVFX, Quaternion.identity);
        }

        if (colliderToDisable) colliderToDisable.enabled = false;

        foreach(var obj in othersPlayersVisuals)
        {
            if(obj != null) obj.SetActive(false);
        }
    }


    [ObserversRpc(runLocally: true)]
    public void ReviveObserversRpc(Vector3 spawnPos, Quaternion spawnRot, bool giveGuns = true)
    {
        if (isServer) _health.value = _maxHealth;

        if(isOwner)
        {
            player.canMove = false;
            player.cameraBlocked = false;
        }

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

        foreach(var obj in othersPlayersVisuals)
        {
            if(obj != null) obj.SetActive(true);
        }

        foreach(var obj in selfPlayerVisuals)
            obj.SetActive(isOwner);

        if (isServer && giveGuns) CastToSpinFromServer();

    }

    private void CastToSpinFromServer()
    {
        if (_weaponDatabase == null || player == null) 
        {
            Debug.LogError("Falta WeaponDatabase o player en PlayerHealth");
            return;
        }

        var candidates = _weaponDatabase.weapons;

        WeaponScripteableObject selectedWeapon = _weaponDatabase.GetRandomWeaponWeighted(candidates);

        int selectedID = _weaponDatabase.GetIdOfWeapon(selectedWeapon);

        int[] candidatesIDs = new int[candidates.Count];
        for(int i = 0; i < candidates.Count; i++)
        {
            candidatesIDs[i] = _weaponDatabase.GetIdOfWeapon(candidates[i]);
        }

        player.TargetStartSpin(owner.Value, selectedID, candidatesIDs);
    }


    [ServerRpc]
    public void SetImmunityRpc(bool inmune)
    {
        _canTakeDamage.value = !inmune;
    }


    
}
