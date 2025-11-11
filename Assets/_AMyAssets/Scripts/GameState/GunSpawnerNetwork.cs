using PurrNet;
using Unity.VisualScripting;
using UnityEngine;

public class GunSpawnerNetwork : NetworkBehaviour
{
    private SpawningGunsState spawnGunStateInstance;

    void Awake() => InstanceHandler.RegisterInstance(this);



    public void RpcShowSlotMachine(PlayerID target, Player player)
    {
        Debug.Log($"<color=green>📺 Mostrando SlotMachine en cliente {target}</color>");
        Debug.Log($"<color=red> playerName: {player.gameObject.name} </color>");
    }
    
    
}
