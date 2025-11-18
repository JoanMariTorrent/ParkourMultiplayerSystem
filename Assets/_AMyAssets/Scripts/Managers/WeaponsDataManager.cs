using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class WeaponsDataManager : NetworkBehaviour
{
    public List<WeaponScripteableObject> primaryWeapons;
    public List<WeaponScripteableObject> secondaryWeapons;

    public GameObject[] _primaryWeaponData;
    public GameObject[] _secondaryWeaponData;
    public GameObject[] _utilityData;

    public GameObject[] _allGuns;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        base.OnDestroy();
        InstanceHandler.UnregisterInstance<WeaponsDataManager>();
    }



    public GameObject[] GetRandomWeapons(int loop, int Type)
    {
        GameObject[] weapons = new GameObject[loop];

        for (int i = 0; i < loop; i++)
        {
            if (Type == 1)
            {
                int index = Random.Range(0, primaryWeapons.Count);
                weapons[i] = primaryWeapons[index].gunPrefab;
            }
            else if (Type == 2)
            {
                int index = Random.Range(0, _secondaryWeaponData.Length);
                weapons[i] = _secondaryWeaponData[index];
            }
            else if (Type == 3)
            {
                int index = Random.Range(0, _utilityData.Length);
                weapons[i] = _utilityData[index];
            }
            else if (Type == 4)
            {
                int index = Random.Range(0, _allGuns.Length);
                weapons[i] = _allGuns[index];
            }
        }

        return weapons;
    }

}
