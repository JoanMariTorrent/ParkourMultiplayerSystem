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
                _currentGun.gameObject.SetActive(false);

            if (primaryWeapon)
            {
                int indexWeapon = GetWeaponIndex(weaponPrefab, true);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");
                InstantiateGun(weaponPrefab);
            }
            else if (!primaryWeapon)
            {
                int indexWeapon = GetWeaponIndex(weaponPrefab, false);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");
                InstantiateGun(weaponPrefab);
            }


        }
    }
    
    [ServerRpc]
    public void NewWeapon(GameObject weaponPrefab, bool primary)
    {
        EnsureWeaponSlots();
        bool havePrimary = _ownedWeapons[0] || _ownedWeapons[1];
        bool haveSecondary = _ownedWeapons[2] || _ownedWeapons[3];

        if (primary && havePrimary)
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
        else if (!primary && haveSecondary)
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
                    EquipWeapon(weaponPrefab, true, false);
                }
            }
        }

        if (!havePrimary || !haveSecondary)
        {
            EquipWeapon(weaponPrefab, false, primary);
            Debug.Log($"weapon manager: {primary}");
        }

    }


    private void EnsureWeaponSlots()
{
    while (_ownedWeapons.Count < 4)
        _ownedWeapons.Add(null);
}


    private void InstantiateGun(GameObject weaponPrefab)
    {
        if (_currentGun != null)
            _currentGun.gameObject.SetActive(false);
        
        var weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;


        _currentGun = weaponInstance.GetComponent<Gun>();
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);
        
    }
    
    

    public void SwitchWeapon(int index) // FALTA ARREGLAR QUE AL CAMBIAR EL ARMA, SE OCULTE LA ANTERIOR Y SE ACTIVE LA NUEVA
    {
        if (index < 0 || index >= _ownedWeapons.Count)
            return;

        GameObject weaponToSwitch = _ownedWeapons[index];

        if (weaponToSwitch == null)
            return;



        // ocultar arma actual
        for (int i = 0; i < _ownedWeapons.Count; i++)
        {
            if (_ownedWeapons[i] != null)
                _ownedWeapons[i].SetActive(i == index); // solo el índice seleccionado se activa
        }

        // Activar el arma
        _currentGun = weaponToSwitch.GetComponent<Gun>();
        _currentGun.gameObject.SetActive(true);

        _currentGun.gameObject.transform.localPosition = Vector3.zero;
        _currentGun.gameObject.transform.localRotation = Quaternion.identity;

        // reconfigurar camara y recoil
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);

        Debug.LogAssertion(weaponToSwitch);
        //Debug.Log($"Cambio de arma a {_currentGun.name} en el slot {index}");
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
