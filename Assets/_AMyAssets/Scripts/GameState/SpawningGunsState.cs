using PurrNet;
using PurrNet.StateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SpawningGunsState : StateNode<List<PlayerHealth>>
{
    [SerializeField] private int playerCount;
    [SerializeField] private int playerEndedSpinCount;

    [SerializeField] private List<SlotMachine> PlayersSlotMachines = new();
    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private List<PlayerID> _players = new();


    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);

        if (!asServer)
            return;
        if (data.Count <= 0)
            return;


        foreach (var player in data)
        {
            var getPlayer = player.GetComponent<Player>();
            if (getPlayer == null)
                continue;
            playerCount++;

            //RpcShowSlotMachine(getPlayer.owner.Value, getPlayer);
            StartCoroutine(GetGuns(getPlayer, data));
        }

        TryGoNextState(data);
        
    }


    [TargetRpc]
    public void RpcShowSlotMachine(PlayerID target, Player player)
    {
        Debug.Log($"<color=green>📺 Mostrando SlotMachine en cliente {target}</color>");
        Debug.Log($"<color=red> playerName: {player.gameObject.name} </color>");
    }

    private void TryGoNextState(List<PlayerHealth> data)
    {
        if(playerEndedSpinCount == playerCount)
        {
            Debug.Log($"<color=purple> All players have finished their spin!</color>");
            machine.Next(data);
        }
        else if(playerEndedSpinCount != playerCount)
        {
            Debug.Log("<color=orange> Wait for other playes to finish his spins!</color>");
        }
    }



    private IEnumerator GetGuns(Player player, List<PlayerHealth> data)
    {
        yield return null;
        if (player == null)
        {
            Debug.Log("<color=red> No se ha encontrado el player</color>");
        }

        if (player.canvas == null)
        {
            Debug.Log("<color=red> No se ha encontrado el canvas</color>");
        }

        if (player.canvas._allViews == null)
        {
            Debug.Log("<color=red> No se ha encontrado _allViews en el canvas</color>");
        }

        Debug.Log("asdasdasd");


        var slotMachine = player.canvas._allViews.OfType<SlotMachine>().FirstOrDefault();
        Debug.Log($"<color=yellow> slot machine: {slotMachine} of player: {player} </color>");
        if (slotMachine == null) yield break;
        PlayersSlotMachines.Add(slotMachine);


        var weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogAssertionFormat("weaponManager is null!");
            yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        slotMachine.GetComponent<CanvasGroup>().alpha = 1f;
        slotMachine.gameObject.SetActive(true);

        yield return slotMachine.Spin();
        yield return new WaitForSeconds(1f);
        slotMachine.GetComponent<CanvasGroup>().alpha = 0f;

        if (slotMachine.finalWeapon.weaponType == WeaponScripteableType.Primary)
            weaponManager.NewWeapon(slotMachine.finalWeapon.gunPrefab.gameObject, true, false, false);

        else if (slotMachine.finalWeapon.weaponType == WeaponScripteableType.Secondary)
            weaponManager.NewWeapon(slotMachine.finalWeapon.gunPrefab, false, false, false);

        else if (slotMachine.finalWeapon.weaponType == WeaponScripteableType.Utility)
            weaponManager.NewWeapon(slotMachine.finalWeapon.gunPrefab, false, true, false);


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        weaponManager.SwitchWeapon(0);
        slotMachine.gameObject.SetActive(false);
        player.GetComponent<Player>().canMove = true;

        Debug.Log($"<color=green>✅ Player {player.gameObject} has ended his spin!</color>");
        playerEndedSpinCount++;

        TryGoNextState(data);

    }
}

