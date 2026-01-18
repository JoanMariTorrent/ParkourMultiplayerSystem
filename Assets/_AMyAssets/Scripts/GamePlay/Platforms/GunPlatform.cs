using System;
using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class GunPlatform : NetworkBehaviour
{
    [SerializeField] private float timer = 0f;
    [SerializeField] private float timeNextGun = 10f;
    [Space]
    [SerializeField] private GameObject spawnGunPos;

    [Space] 
    [SerializeField] private bool gunSpawned = false;
    [SerializeField] private int currentGunIndex = 0; 
    [SerializeField] private GameObject spawnedGunVisual; 
    [SerializeField] private bool primaryWeapon;
    
    [Space]
    private WeaponsDataManager _weaponsData;
    private PlayerCharacter localPlayerInTrigger;

    private void Start()
    {
        InstanceHandler.TryGetInstance(out WeaponsDataManager weaponsDataManager);
        if (weaponsDataManager == null) return;
        _weaponsData = weaponsDataManager;
    }

    private void Update()
    {
        if (gunSpawned && localPlayerInTrigger != null && localPlayerInTrigger.isOwner)
        {
            if (localPlayerInTrigger._requestedInteract)
            {
                RequestPickUpServerRpc(localPlayerInTrigger.owner.Value);
                localPlayerInTrigger._requestedInteract = false; 
            }
        }

        if (isServer)
        {
            if (!gunSpawned)
            {
                timer += Time.deltaTime;
                if (timer >= timeNextGun)
                {
                    timer = 0;
                    ServerDecideAndSpawnGun();
                }
            }
        }
    }

    private void ServerDecideAndSpawnGun()
    {
        int typeGun = 1; 
        
        GameObject[] selectedGunArray = _weaponsData.GetRandomWeapons(1, typeGun);
        
        if (selectedGunArray == null || selectedGunArray.Length == 0) return;

        SpawnGunObserversRpc(typeGun);
    }

    [ObserversRpc]
    private void SpawnGunObserversRpc(int typeGun)
    {
        GameObject[] possibleGuns = _weaponsData.GetRandomWeapons(1, typeGun);
        if (possibleGuns == null || possibleGuns.Length == 0) return;
        
        GameObject gunPrefab = possibleGuns[0];

        if (typeGun == 1) primaryWeapon = true;
        else if (typeGun == 2) primaryWeapon = false;

        if (spawnedGunVisual != null) Destroy(spawnedGunVisual);
        
        spawnedGunVisual = Instantiate(gunPrefab, spawnGunPos.transform.position, spawnGunPos.transform.rotation);
        
        var gunScript = spawnedGunVisual.GetComponent<Gun>();
        if (gunScript) 
        {
            gunScript.enabled = false; 
            gunScript.equipedGun = false;
        }
        
        spawnedGunVisual.gameObject.SetActive(true);
        spawnedGunVisual.transform.localScale = Vector3.one;
        
        gunSpawned = true;
        currentGunIndex = typeGun; 
    }

    [ServerRpc(requireOwnership: false)] 
    private void RequestPickUpServerRpc(PlayerID playerID)
    {
        if (!gunSpawned) return; 

        Player playerScript = null;
        
        foreach (var p in FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p.owner.Value == playerID)
            {
                playerScript = p;
                break;
            }
        }

        if (playerScript == null) return;

        WeaponManager weaponManager = playerScript.GetComponent<WeaponManager>();
        if (weaponManager == null) return;

        GameObject[] gunToGive = _weaponsData.GetRandomWeapons(1, currentGunIndex);
        
        weaponManager.NewWeapon(gunToGive[0], primaryWeapon, false, false);

        RestartAllObserversRpc();
    }

    [ObserversRpc]
    private void RestartAllObserversRpc()
    {
        if (spawnedGunVisual != null) Destroy(spawnedGunVisual);
        gunSpawned = false;
        timer = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        var playerChar = other.GetComponent<PlayerCharacter>();
        if (playerChar == null) return;

        if (playerChar.isOwner)
        {
            localPlayerInTrigger = playerChar;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var playerChar = other.GetComponent<PlayerCharacter>();
        if (playerChar == null) return;

        if (playerChar == localPlayerInTrigger)
        {
            localPlayerInTrigger = null;
        }
    }
}