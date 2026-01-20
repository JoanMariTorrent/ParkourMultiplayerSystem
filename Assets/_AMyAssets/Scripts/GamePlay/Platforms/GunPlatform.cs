using System;
using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System.Linq; 

public class GunPlatform : NetworkBehaviour
{
    [SerializeField] private float timer = 0f;
    [SerializeField] private float timeNextGun = 10f;
    [Space]
    [SerializeField] private GameObject spawnGunPos;

    [Space] 
    [SerializeField] private bool gunSpawned = false;
    
    [SerializeField] private WeaponID currentSpawnedID = WeaponID.None;
    
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

        // Lógica de Spawneo (Solo Server)
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
        int typeGun = 1; // 1 = Primary, 2 = Secondary
        
        // 1. El servidor elige el arma aleatoria
        GameObject[] randomSelection = _weaponsData.GetRandomWeapons(1, typeGun);
        
        if (randomSelection == null || randomSelection.Length == 0) return;

        GameObject chosenPrefab = randomSelection[0];
        Gun gunScript = chosenPrefab.GetComponent<Gun>();
        
        if (gunScript == null) return;

        // 2. Extraemos su ID único
        WeaponID idToSpawn = gunScript.weaponID;

        // 3. Se lo mandamos a todos
        SpawnGunObserversRpc(idToSpawn, typeGun);
    }

    [ObserversRpc]
    private void SpawnGunObserversRpc(WeaponID weaponID, int typeGun)
    {
        // Guardamos el ID para saber qué tenemos
        currentSpawnedID = weaponID;
        
        // BUSCAMOS EL PREFAB EXACTO USANDO EL ID
        GameObject gunPrefab = FindPrefabByID(weaponID, typeGun);

        if (gunPrefab == null) return;

        if (typeGun == 1) primaryWeapon = true;
        else if (typeGun == 2) primaryWeapon = false;

        if (spawnedGunVisual != null) Destroy(spawnedGunVisual);
        
        spawnedGunVisual = Instantiate(gunPrefab, spawnGunPos.transform.position, spawnGunPos.transform.rotation);
        
        var gunScript = spawnedGunVisual.GetComponent<Gun>();
        if (gunScript) 
        {
            gunScript.enabled = false; 
            gunScript.equipedGun = false;
            // Desactiva físicas si hace falta
             var rb = gunScript.GetComponent<Rigidbody>();
             if(rb) rb.isKinematic = true;
        }
        
        spawnedGunVisual.gameObject.SetActive(true);
        spawnedGunVisual.transform.localScale = Vector3.one;
        
        gunSpawned = true;
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

        // No generamos una aleatoria. Buscamos el prefab que coincide con el ID que está visualmente spawneda.
        int typeGun = primaryWeapon ? 1 : 2;
        GameObject gunToGive = FindPrefabByID(currentSpawnedID, typeGun);
        
        if (gunToGive != null)
        {
            weaponManager.NewWeapon(gunToGive, primaryWeapon, false, false);
            RestartAllObserversRpc();
        }
    }

    //
    private GameObject FindPrefabByID(WeaponID id, int typeGun)
    {
        GameObject[] allPossible = _weaponsData.GetRandomWeapons(50, typeGun); 
        
        foreach(var prefab in allPossible)
        {
            if(prefab == null) continue;
            var g = prefab.GetComponent<Gun>();
            if(g != null && g.weaponID == id)
            {
                return prefab;
            }
        }
        
        Debug.LogError($"No se encontró el prefab con ID {id} en el manager!");
        return null;
    }

    [ObserversRpc]
    private void RestartAllObserversRpc()
    {
        if (spawnedGunVisual != null) Destroy(spawnedGunVisual);
        gunSpawned = false;
        currentSpawnedID = WeaponID.None; // Reset ID
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