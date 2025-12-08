using System;
using UnityEngine;
using PurrNet;
using System.Collections.Generic;
// using Random = UnityEngine.Random; // Descomenta si usas random real

public class GunPlatform : NetworkBehaviour
{
    [Header("Timers")]
    [SerializeField] private float timer = 0f;
    [SerializeField] private float timeNextGun = 10f;

    [Header("Referencias")]
    [SerializeField] private GameObject spawnGunPos;
    [SerializeField] private GameObject[] prefabsGun; // Tus prefabs de armas

    [Header("Estado (Sync no necesario si usamos RPCs de estado)")]
    private bool gunSpawned = false;
    private int currentGunIndex = 0;
    private bool isPrimary = true;
    
    // Referencia visual local (no network object, solo el modelo visual)
    private GameObject visualGunInstance;

    private WeaponsDataManager _weaponsData;
    
    // Control local para saber si YO estoy dentro
    private bool isLocalPlayerInside = false;
    private PlayerCharacter localPlayerCharacter;

    private void Start()
    {
        InstanceHandler.TryGetInstance(out WeaponsDataManager weaponsDataManager);
        if (weaponsDataManager != null) _weaponsData = weaponsDataManager;
    }

    private void Update()
    {
        // 1. LÓGICA DEL SERVIDOR (Temporizador)
        if (isServer)
        {
            if (!gunSpawned)
            {
                timer += Time.deltaTime;
                if (timer >= timeNextGun)
                {
                    timer = 0;
                    ServerSpawnGunLogic();
                }
            }
        }

        // 2. LÓGICA DEL CLIENTE (Input)
        // Solo si soy un cliente, estoy dentro del trigger y el arma está lista
        if (isLocalPlayerInside && gunSpawned)
        {
            // Verificamos si el jugador local pulsó interactuar
            // Asumo que localPlayerCharacter tiene referencia al Input o verificamos el flag
            if (localPlayerCharacter != null && localPlayerCharacter._requestedInteract)
            {
                // IMPORTANTE: Reseteamos el input para no spamear
                localPlayerCharacter._requestedInteract = false; 
                
                // Pedimos al servidor coger el arma
                RequestPickup_ServerRpc(localPlayerCharacter.GetComponent<PlayerID>()); 
            }
        }
    }

    // ----------------------------------------------------------------
    // LÓGICA DE SPAWN (SERVIDOR -> TODOS)
    // ----------------------------------------------------------------

    private void ServerSpawnGunLogic()
    {
        // 1. Elegir arma (Lógica Server)
        int typeGun = 1; // Tu lógica de random aquí
        var selectedWeaponList = _weaponsData.GetRandomWeapons(1, typeGun);
        
        // Asumimos que spawnGun[0] es el prefab correcto. 
        // Necesitamos saber qué índice es en tu array 'prefabsGun' para decirle a los clientes cuál instanciar.
        // O si 'GetRandomWeapons' devuelve el prefab directo, asegúrate de que los clientes puedan saber cuál es.
        // Para simplificar este ejemplo, usaremos un índice ficticio o el nombre.
        
        gunSpawned = true;
        isPrimary = (typeGun == 1);
        
        // 2. Avisar a todos los clientes (incluido el host) que muestren el arma
        // Pasamos el índice o identificador del arma para que sepan qué modelo mostrar
        SpawnGun_ObserversRpc(typeGun); 
    }

    [ObserversRpc]
    private void SpawnGun_ObserversRpc(int weaponType)
    {
        gunSpawned = true; // Actualizamos estado en cliente
        
        // Instanciamos VISUALMENTE el arma (sin lógica, solo modelo)
        if (visualGunInstance != null) Destroy(visualGunInstance);

        // Aquí deberías buscar el prefab correcto basado en weaponType o ID
        // Por ahora uso tu lógica original simplificada:
        GameObject prefabVisual = _weaponsData.GetRandomWeapons(1, weaponType)[0]; 

        visualGunInstance = Instantiate(prefabVisual, spawnGunPos.transform.position, spawnGunPos.transform.rotation);
        visualGunInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Desactivamos scripts del arma visual para que no dispare ni haga nada
        var gunScript = visualGunInstance.GetComponent<Gun>();
        if (gunScript) gunScript.enabled = false; 
    }

    // ----------------------------------------------------------------
    // LÓGICA DE INTERACCIÓN (CLIENTE -> SERVIDOR)
    // ----------------------------------------------------------------

    // requireOwnership: false es VITAL. Permite que el Jugador 2 llame a este RPC 
    // aunque la plataforma pertenezca al Server.
    [ServerRpc(requireOwnership: false)]
    private void RequestPickup_ServerRpc(PlayerID playerWhoAsked)
    {
        if (!gunSpawned) return; // Validación de seguridad

        // Buscamos el objeto del jugador en el servidor
        // (Asumiendo que tienes una forma de encontrar al jugador por ID o NetID)
        // Si no tienes sistema de PlayerID, puedes pasar el NetworkId del jugador.
        
        // Forma simplificada: Si usas PlayerRegistry o similar:
        Player playerScript = GetPlayerFromID(playerWhoAsked); 
        
        if (playerScript != null)
        {
            var weaponManager = playerScript.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                // LÓGICA REAL DE DAR ARMA (Solo ocurre en el server)
                // Necesitas el prefab REAL aquí, no el visual
                GameObject realPrefab = _weaponsData.GetRandomWeapons(1, isPrimary ? 1 : 2)[0]; 
                
                weaponManager.NewWeapon(realPrefab, isPrimary, false, false);
                
                // Reiniciar plataforma
                RestartAll_ObserversRpc();
            }
        }
    }

    // Función auxiliar para encontrar al jugador (implementa según tu juego)
    private Player GetPlayerFromID(PlayerID id)
    {
        foreach(var p in PlayerRegistry.AllPlayers) // Asumiendo que tienes esto de tu otro script
        {
            if (p.owner == id) return p; // O la lógica que uses para identificar
        }
        return null;
    }

    [ObserversRpc]
    private void RestartAll_ObserversRpc()
    {
        if (visualGunInstance != null) Destroy(visualGunInstance);
        gunSpawned = false;
        timer = 0;
    }

    // ----------------------------------------------------------------
    // DETECCIÓN DE TRIGGERS (LOCAL)
    // ----------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el objeto que entró es MI jugador local
        var playerChar = other.GetComponent<PlayerCharacter>();
        if (playerChar != null)
        {
            // Comprobamos si este character pertenece a este cliente
            var netIdentity = playerChar.GetComponent<NetworkIdentity>(); 
            if (netIdentity != null && netIdentity.isOwner)
            {
                isLocalPlayerInside = true;
                localPlayerCharacter = playerChar;
                // Debug.Log("Yo (Cliente) entré en la plataforma");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var playerChar = other.GetComponent<PlayerCharacter>();
        if (playerChar != null)
        {
            var netIdentity = playerChar.GetComponent<NetworkIdentity>();
            if (netIdentity != null && netIdentity.isOwner)
            {
                isLocalPlayerInside = false;
                localPlayerCharacter = null;
            }
        }
    }
}