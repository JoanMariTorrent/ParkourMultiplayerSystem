using System;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
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

    [Header("Health stats")]
    [SerializeField] private float startHealDelay;
    [SerializeField] private float delayBeetwenHeal;

    [Header("Weapon Logic")]
    private WeaponsDataManager _weaponsData;
    [SerializeField] private WeaponDatabase _weaponDatabase;
    [SerializeField] private UtilityDatabase _utilityDatabase;

    [Header("Componentes para Desactivar/Activar")]
    [SerializeField] private List<GameObject> othersPlayersVisuals;
    [SerializeField] private List<GameObject> selfPlayerVisuals;
    [SerializeField] private Collider colliderToDisable;
    [SerializeField] private Rigidbody _rb;
    public bool IsDead => _health.value <= 0;

    [Header("Audios")]
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] deathClips;

    public Action<PlayerID, string, string> OnDeath_Server;
    public PlayerID PlayerID => owner.Value;

    public int health => _health.value;
    public int maxHealth => _maxHealth;
    public float lastTimeTakenDamage { get; private set; } = -10f;
    private int _oldHealth;
    public float lastHitIntensity { get; private set; }
    private float nextHealTick;


    void Start()
    {
        InstanceHandler.TryGetInstance(out WeaponsDataManager wm);
        _weaponsData = wm;
    }

    void Update()
    {
        if(!isServer) return;
        if(Time.time - lastTimeTakenDamage > startHealDelay)
        {
            if(health < maxHealth && Time.time >= nextHealTick)
            {
                _health.value++;
                nextHealTick = Time.time + delayBeetwenHeal;
            }
        }
    }


    protected override void OnSpawned()
    {
        base.OnSpawned();
        _oldHealth = _health.value;

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
        if (_newHealth < _oldHealth)
        {
            int damageTaken = _oldHealth - _newHealth;
            lastTimeTakenDamage = Time.time;

            float baseIntensity = 0.4f;
            float damageScale = damageTaken / 25f;

            lastHitIntensity = Mathf.Clamp01(baseIntensity + damageScale);
        }
    
        _oldHealth = _newHealth;

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

        if (_amount < 0)
        {
            lastTimeTakenDamage = Time.time;
            nextHealTick = Time.time + startHealDelay + delayBeetwenHeal;
        }

        _health.value += _amount;

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
            string victimName = player.playerName;
            string killerName = "void";

            if (attackerID.HasValue)
            {
                if (attackerID.Value == null)
                {
                    killerName = "void";
                }
                else
                {
                    var attackerPlayer = PlayerRegistry.AllPlayers.Find(p => p.owner == attackerID.Value);
                    
                    if (attackerPlayer != null)
                        killerName = attackerPlayer.playerName;
                    else
                        killerName = "Unknown";
                }
            }

            if(string.IsNullOrWhiteSpace(victimName)) victimName = owner.Value.ToString();
            if(string.IsNullOrWhiteSpace(killerName)) killerName = attackerID.Value.ToString();

            if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                if(attackerID != null || attackerID.HasValue) scoreManager.Addkills(attackerID.Value);
                if(owner.HasValue)
                    scoreManager.AddDeath(owner.Value);
            }

            if (weaponManager != null)
            {
                weaponManager.DropAllWeaponsOnDeath();
                if(isOwner) weaponManager._currentItem = null;
            }

            
            DieVisualsObserversRpc();
            OnDeath_Server?.Invoke(owner.Value, victimName, killerName);
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

        if (selfPlayerVisuals.Count > 0) 
            foreach(var obj in selfPlayerVisuals)
                obj.SetActive(isOwner);

        if (isServer && giveGuns) CastToSpinFromServer();

    }

    private void CastToSpinFromServer()
    {
        if (_weaponDatabase == null || _utilityDatabase == null || player == null) 
        {
            Debug.LogError("Falta WeaponDatabase, UtilityDatabase o player en PlayerHealth");
            return;
        }

        // Filtrar armas principales

        var primFiltered = _weaponDatabase.weapons.FindAll(w => w.weaponType == WeaponScripteableType.Primary);
        if(primFiltered.Count == 0) primFiltered = _weaponDatabase.weapons;

        var primWinner = _weaponDatabase.GetRandomWeaponWeighted(primFiltered);
        int primID = _weaponDatabase.GetIdOfWeapon(primWinner);
        int[] primPool = primFiltered.ConvertAll(w => _weaponDatabase.GetIdOfWeapon(w)).ToArray();

        // Filtar armas secundarias, de momento esta en -1 porque asi no salen
        int secID = -1;
        int[] secPool = new int[0];

        // Filtrar utilidades
        var utilAll = _utilityDatabase.allUtilities;
        var utilWinner = _utilityDatabase.GetRandomUtilityWeighted(utilAll);
        int utilID = _utilityDatabase.GetIdOfUtility(utilWinner);
        int[] utilPool = utilAll.ConvertAll(u => _utilityDatabase.GetIdOfUtility(u)).ToArray();

        // Convertir los ganadores en un array
        int[] winners = {primID, secID, utilID};

        player.TargetStartSpin(owner.Value, winners, primPool, secPool, utilPool);
    }


    [ServerRpc]
    public void SetImmunityRpc(bool inmune)
    {
        _canTakeDamage.value = !inmune;
    }
    


    
}
