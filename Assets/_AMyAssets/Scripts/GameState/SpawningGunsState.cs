using NUnit.Framework;
using PurrNet;
using PurrNet.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class SpawningGunsState : StateNode<List<PlayerHealth>>
{
    private List<PlayerID> _players = new();


    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);

        if (!asServer)
            return;
        if (data.Count <= 0)
            return;


        GetGuns(data);
        machine.Next(data);
    }


    private void GetGuns(List<PlayerHealth> data)
    {
        if (InstanceHandler.TryGetInstance(out WeaponsDataManager _weaponDataManager))
        {
            foreach (var player in data)
            {
                var weaponManager = player.GetComponent<WeaponManager>();
                if (!weaponManager) continue;


                GameObject[] _primary = _weaponDataManager.GetRandomWeapons(1, 1);
                GameObject[] _secondary = _weaponDataManager.GetRandomWeapons(1, 2);
                GameObject[] _utility = _weaponDataManager.GetRandomWeapons(1, 3);

                weaponManager.NewWeapon(_primary[0].gameObject, true, false, false);
                weaponManager.NewWeapon(_secondary[0].gameObject, false, false, false);
                weaponManager.NewWeapon(_utility[0].gameObject, false, true, false);
                weaponManager.SwitchWeapon(0);


                //Debug.Log($"{_primary[0].gameObject} and {_secondary[0].gameObject}");
            }
        }
    }
}

