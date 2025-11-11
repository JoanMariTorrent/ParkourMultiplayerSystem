using PurrNet;
using PurrNet.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningGunsState : StateNode<List<PlayerHealth>>
{
    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private List<PlayerID> _players = new();


    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        if (data.Count <= 0)
            return;


        foreach (var player in data)
        {
            var getPlayer = player.GetComponent<Player>();
            if (getPlayer == null)
                continue;


            Debug.Log($"<color=purple>Enviando SlotMachine a jugador {getPlayer.owner.Value}</color>");
            RpcShowSlotMachine(getPlayer.owner.Value, getPlayer);
        }
        machine.Next(data);
    }


    [TargetRpc(requireServer: true, runLocally: true)]
    public void RpcShowSlotMachine(PlayerID target, Player player)
    {
        Debug.Log($"<color=green>📺 Mostrando SlotMachine en cliente {target}</color>");
        Debug.Log($"<color=red> playerName: {player.gameObject.name} </color>");
    }



    private IEnumerator GetGuns(Player player)
    {
        if (InstanceHandler.TryGetInstance(out SlotMachine slotMachine))
        {
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
            Debug.Log("<color=green>✅ Spin completado correctamente</color>");
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
        }

    }
}

