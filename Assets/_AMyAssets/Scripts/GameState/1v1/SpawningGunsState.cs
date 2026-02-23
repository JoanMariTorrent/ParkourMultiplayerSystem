using PurrNet;
using PurrNet.StateMachine;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum randomType
{
    Primary,
    Secondary,
    Utility,
    All
}


[Serializable]
public partial struct WeaponSpinRound
{
    public int winnerID;
    public int[] poolIDs;
}


// La spin se activa desde el script del Player, ya que el canvas se spawnea solo si isOwner es true,
// es decir, que se instancia en local, y este script solo lo ejecuta el servidor, asi que lo que hay
// que hacer, es que el servidor active una funcion en cada jugar, y esa funcion sea el spin
public class SpawningGunsState : StateNode<List<PlayerHealth>>
{

    public static SpawningGunsState SpawningGunsStateActiveInstance;
    [SerializeField] private List<Player> normalPlayers = new();
    [SerializeField] private List<SlotMachine> PlayersSlotMachines = new();
    [SerializeField] private int playerEndedSpinCount;
    [SerializeField] private int totalPlayers = 0;
    [SerializeField] private List<WeaponScripteableObject> weapons;
    [SerializeField] private List<WeaponScripteableObject> filteredWeapons = new List<WeaponScripteableObject>();
    [SerializeField] private WeaponDatabase weaponDataBase;
    [SerializeField] private UtilityDatabase utilityDatabase;
    [SerializeField] private WeaponScripteableObject selectedWeapon;
    [SerializeField] private randomType randomType;
    [SerializeField] private bool giveSecondary = false;
    private List<PlayerHealth> _playersDataCache = new List<PlayerHealth>();
    private List<PlayerID> _players = new();
    


    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        SpawningGunsStateActiveInstance = this;
    }

    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);

        SpawningGunsStateActiveInstance = this;

        if (!asServer)
            return;
        if (data.Count <= 0)
            return;



        // Reset de variables
        normalPlayers.Clear();
        PlayersSlotMachines.Clear();
        filteredWeapons.Clear();
        _players.Clear();
        _playersDataCache.Clear();

        playerEndedSpinCount = 0;
        totalPlayers = 0;

        _playersDataCache = data;
        totalPlayers = data.Count;

        foreach (var player in data)
        {
            var getPlayer = player.GetComponent<Player>();
            if (getPlayer == null)
                continue;
            normalPlayers.Add(getPlayer);
            _players.Add(player.owner.Value);
        }

        Debug.Log($"[Enter] Players count: {normalPlayers.Count}");

        ServerShowSlot();
        TryGoNextState(data);

    }

    [ObserversRpc]
    private void ServerShowSlot()
    {
        foreach (var player in normalPlayers)
        {
            var r1 = CreateWeaponRound(randomType.Primary, weaponDataBase);
            var r3 = CreateUtilityRound(randomType.Utility, utilityDatabase);
    
            int[] winners;
            int[] p2_data;
    
            if (giveSecondary) 
            {
                var r2 = CreateWeaponRound(randomType.Secondary, weaponDataBase);
                winners = new int[] { r1.winnerID, r2.winnerID, r3.winnerID };
                p2_data = r2.poolIDs;
            } 
            else 
            {
                // Mandamos -1 para "apagar" la columna central
                winners = new int[] { r1.winnerID, -1, r3.winnerID };
                p2_data = new int[0]; 
            }
    
            player.TargetStartSpin(player.owner.Value, winners, r1.poolIDs, p2_data, r3.poolIDs);
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void OnPlayerFinishedSpin(PlayerID playerID)
    {

        Debug.Log(normalPlayers.Count);

        playerEndedSpinCount ++;
        
        Debug.Log($"<color=green> Player {playerID} finished his spin ({playerEndedSpinCount} / {totalPlayers})");
        TryGoNextState(_playersDataCache);
    }

    private void TryGoNextState(List<PlayerHealth> data)
    {
        if (totalPlayers == 0 && data.Count > 0) return;
        if (totalPlayers == 0 && data.Count == 0) {machine.Next(data); return;}

        if (playerEndedSpinCount == normalPlayers.Count)
        {
            Debug.Log($"<color=purple> All players have finished their spin!</color>");
            machine.Next(data);
        }
        else if (playerEndedSpinCount != normalPlayers.Count)
        {
            Debug.Log("<color=orange> Wait for other playes to finish his spins!</color>");
        }
    }



    // Elegir un arma segun sus posibilidades
    private WeaponScripteableObject ChooseWeaponByChance(List<WeaponScripteableObject> weaponList)
    {
        float totalChance = 0f;
        foreach (var w in weaponList)
            totalChance += w.dropChance;

        float r = UnityEngine.Random.Range(0f, totalChance);
        float accum = 0f;

        foreach (var w in weaponList)
        {
            accum += w.dropChance;
            if (r <= accum)
                return w;
        }

        return weaponList[weaponList.Count - 1];
    }

    private List<WeaponScripteableObject> filteredWeaponsList()
    {
        foreach (var w in weapons)
        {
            switch (randomType)
            {
                case randomType.Primary:
                    if (w.weaponType == WeaponScripteableType.Primary)
                        filteredWeapons.Add(w);
                    break;
                case randomType.Secondary:
                    if (w.weaponType == WeaponScripteableType.Secondary)
                        filteredWeapons.Add(w);
                    break;
                case randomType.Utility:
                    if (w.weaponType == WeaponScripteableType.Utility)
                        filteredWeapons.Add(w);
                    break;
            }
        }

        // Si no hay ninguna, usar todas
        if (filteredWeapons.Count == 0)
            filteredWeapons = new List<WeaponScripteableObject>(weapons);

        return filteredWeapons;
    }



    private WeaponSpinRound CreateWeaponRound(randomType type, WeaponDatabase db)
    {
        var filtered = db.weapons.FindAll(w => w.weaponType == (WeaponScripteableType)type);
        if (filtered.Count == 0) filtered = db.weapons;

        var selected = db.GetRandomWeaponWeighted(filtered);

        return new WeaponSpinRound {
            winnerID = db.GetIdOfWeapon(selected),
            poolIDs = filtered.ConvertAll(w => db.GetIdOfWeapon(w)).ToArray()
        };
    }

    private WeaponSpinRound CreateUtilityRound(randomType type, UtilityDatabase db)
    {
        var all = db.allUtilities;

        var selected = db.GetRandomUtilityWeighted(all);

        return new WeaponSpinRound {
            winnerID = db.GetIdOfUtility(selected),
            poolIDs = all.ConvertAll(u => db.GetIdOfUtility(u)).ToArray()
        };
    }

    private List<WeaponScripteableObject> GetFilteredList(randomType type)
    {
        List<WeaponScripteableObject> list = new List<WeaponScripteableObject>();

        foreach(var w in weapons)
        {
            if  ((type == randomType.Primary && w.weaponType == WeaponScripteableType.Primary ) ||
                (type == randomType.Secondary && w.weaponType == WeaponScripteableType.Secondary ) ||
                (type == randomType.Utility && w.weaponType == WeaponScripteableType.Utility))
            {
                list.Add(w);
            }
        }
        return list.Count > 0 ? list : new List<WeaponScripteableObject>(weapons);
    }

}

