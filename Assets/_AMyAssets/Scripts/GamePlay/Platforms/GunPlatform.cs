using UnityEngine;
using PurrNet;
using Random = UnityEngine.Random;

public class GunPlatform : NetworkBehaviour
{
    [SerializeField] private float timer = 0f;
    [SerializeField] private float timeNextGun = 10f;
    [Space]
    [SerializeField] private GameObject spawnGunPos;

    [Space] [SerializeField] private bool gunSpawned = false;

    [SerializeField] private int typeGun;
    [SerializeField] private GameObject[] spawnGun;
    [SerializeField] private GameObject spawnedGun;
    [SerializeField] private bool primaryWeapon;
    [Space]
    private WeaponsDataManager _weaponsData;
    [SerializeField] private PlayerCharacter player;
    
    [SerializeField]private bool playerInCollision;
    


    private void Start()
    {
        InstanceHandler.TryGetInstance(out WeaponsDataManager weaponsDataManager);
        if (weaponsDataManager == null) return;
        _weaponsData = weaponsDataManager;
    }


    private void Update()
    {
        if (playerInCollision && gunSpawned)
        {
            if (player == null) return;
            if (player._requestedInteract)
            {
                var weaponManager = player.GetComponent<WeaponManager>();
                if (weaponManager == null) return;

                
                weaponManager.NewWeapon(spawnGun[0].gameObject, primaryWeapon, false, false);

                RestartAll();
            }
        }

        if (!gunSpawned)
        {
            timer += Time.deltaTime;
        
            if (timer < timeNextGun) return;
            timer = 0;
            SpawnGun();
        }

        
    }
    [ServerRpc(requireOwnership: false)]
    private void SpawnGun()
    {
        typeGun = 1; //Random.Range(1, 3);
        spawnGun = _weaponsData.GetRandomWeapons(1, typeGun);
        var gunScript = spawnGun[0].GetComponent<Gun>();
        if (gunScript)
        {
            gunScript.equipedGun = false;
        }

        if (typeGun == 1)
            primaryWeapon = true;
        else if (typeGun == 2)
            primaryWeapon = false;
        
        if (spawnGun == null)
        {
            Debug.LogError("GunPlatform is trying to spawn some gun, but this gun is null");
            return;
        }

        gunSpawned = true;
        spawnedGun = Instantiate(spawnGun[0], spawnGunPos.transform.position, spawnGunPos.transform.rotation);
        spawnedGun.gameObject.SetActive(true);
        spawnedGun.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        
        var playercoll = other.GetComponent<PlayerCharacter>();
        if (playercoll == null) return;
        Debug.Log(playercoll);

        
        playerInCollision = true;
        player = playercoll;
    }

    private void OnTriggerExit(Collider other)
    {
        var playercoll = other.GetComponent<PlayerCharacter>();
        if (playercoll == null) return;
        
        playerInCollision = false;
        player = null;
    }

    [ServerRpc(requireOwnership:false)]
    private void RestartAll()
    {
        Destroy(spawnedGun);
        gunSpawned = false;
        typeGun = 0;
        spawnGun = null;
        primaryWeapon = false;

        RestartAll_ClientRpc(); // mandar actualización a clientes
    }

    [ObserversRpc]
    private void RestartAll_ClientRpc()
    {
        // ocultar en los clientes
        if (spawnedGun != null)
            Destroy(spawnedGun);
    }
}
