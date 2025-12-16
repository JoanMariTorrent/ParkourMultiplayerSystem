using PurrNet;
using Unity.VisualScripting;
using UnityEngine;

public class GunSpawnerNetwork : NetworkBehaviour
{
    private SpawningGunsState spawnGunStateInstance;

    void Awake() => InstanceHandler.RegisterInstance(this);


    
    
    
}
