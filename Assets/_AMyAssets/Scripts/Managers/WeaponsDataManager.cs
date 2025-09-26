using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class WeaponsDataManager : NetworkBehaviour
{
    public GameObject[] _primaryWeaponData;
    public GameObject[] _secondaryWeaponData;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        base.OnDestroy();
        InstanceHandler.UnregisterInstance<WeaponsDataManager>();
    }



    public GameObject[] GetRandomWeapons(int loop, bool primaryWeapon)
    {
        GameObject[] weapons = new GameObject[loop];

        for (int i = 0; i < loop; i++)
        {
            if (primaryWeapon)
            {
                int index = Random.Range(0, _primaryWeaponData.Length);
                weapons[i] = _primaryWeaponData[index];
            }
            else
            {
                int index = Random.Range(0, _secondaryWeaponData.Length);
                weapons[i] = _secondaryWeaponData[index];
            }
        }

        return weapons;
    }

}
