using NUnit.Framework;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using Steamworks;

public class WeaponManager : NetworkBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private RecoilCamera recoil;

    public Gun _currentGun;
    [SerializeField] private SyncList<GameObject> _ownedWeapons = new();

    private void Awake()
    {
        enabled = isOwner;
    }
    private void EquipWeapon(GameObject weaponPrefab, bool deleteWeapon, bool primaryWeapon)
    {
        if (deleteWeapon)
        {
            int currentIndex = _ownedWeapons.IndexOf(_currentGun.gameObject);

            Destroy(_currentGun.gameObject);

            InstantiateGun(weaponPrefab);

            if (currentIndex >= 0)
            {
                _ownedWeapons[currentIndex] = weaponPrefab;
                Debug.Log($"Arma reemplazada en el slot {currentIndex}");
            }
            else
            {
                int indexWeapon = GetWeaponIndex(weaponPrefab, primaryWeapon);
                _ownedWeapons[indexWeapon] = weaponPrefab;
            }
        }

        else if (!deleteWeapon)
        {
            if(_currentGun != null) 
                _currentGun.enabled = false;
            
            if(primaryWeapon)
            {
                int indexWeapon = GetWeaponIndex(weaponPrefab, true);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");
            }
            else if (!primaryWeapon)
            {
                int indexWeapon = GetWeaponIndex(weaponPrefab, false);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");
            }


        }
    }
    
    [ServerRpc]
    public void NewWeapon(GameObject weaponPrefab, bool primary)
    {
        if(primary && _currentGun != null)
        {
            if (_ownedWeapons[0] != null && _ownedWeapons[1] != null) // Si tiene 2 principales
            {
                EquipWeapon(weaponPrefab, true, true); // como tiene 2 armas ya, tendra que destruirla 100%
            }

            if (_ownedWeapons[0] == null || _ownedWeapons[1] == null) // tiene un hueco libre en la arma principal
            {

                if (_ownedWeapons.Contains(weaponPrefab)) // si la arma que esta pillando ya la tiene
                {
                    EquipWeapon(weaponPrefab, true, true);

                }

                else if (!_ownedWeapons.Contains(weaponPrefab)) // si el arma que esta pillando no la tiene en general
                {
                    EquipWeapon(weaponPrefab, false, true);
                }
            }
        }
        else if (!primary && _currentGun != null)
        {

            if (_ownedWeapons[2] != null && _ownedWeapons[3] != null) // si tiene 2 secundarias
            {
                EquipWeapon(weaponPrefab, true, false); // como tiene 2 armas ya, tendra que destruirla 100%
            }
            
            if (_ownedWeapons[2] == null || _ownedWeapons[3] == null) // tiene un hueco libre en la arma secundaria
            {
                if (_ownedWeapons.Contains(weaponPrefab)) // si la arma que esta pillando ya la tiene
                {
                    EquipWeapon(weaponPrefab, true, false);

                }

                else if (!_ownedWeapons.Contains(weaponPrefab)) // si el arma que esta pillando no la tiene en general
                {
                    EquipWeapon(weaponPrefab, false, false);
                }
            }
        }

        if(_currentGun == null)
        {
            EquipWeapon(weaponPrefab, false, primary);
        }

    }


    private void InstantiateGun(GameObject weaponPrefab)
    {
        var weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        _currentGun = weaponInstance.GetComponent<Gun>();
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);
    }

    public void SwitchWeapon(int index)
    {
        if (index >= 0 && index < _ownedWeapons.Count)
        {
           // EquipWeapon(_ownedWeapons[index]);
        }
    }

    private int GetWeaponIndex(GameObject weaponPrefab, bool primaryWeapon)
    {
        int startIndex = primaryWeapon ? 0 : 2;
        int endIndex = primaryWeapon ? 2 : 4;

        for (int i = startIndex; i < endIndex; i++)
        {
            if (i >= _ownedWeapons.Count || _ownedWeapons[i] == null)
            {
                while (_ownedWeapons.Count <= i)
                    _ownedWeapons.Add(null);
                Debug.Log($"Arma añadida en el slot {i}");
                return i;
            }
        }

        return -1;
       
    }

   
}
