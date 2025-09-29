using NUnit.Framework;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

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
    public void EquipWeapon(GameObject weaponPrefab, bool deleteWeapon)
    {
        if (deleteWeapon)
        {
            Destroy(_currentGun.gameObject);
        }
        else if (!deleteWeapon)
        { 
            _currentGun.enabled = false;
        }


        var weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        _currentGun = weaponInstance.GetComponent<Gun>();
        if (_currentGun != null)
        {
            _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);
        }

        if (!_ownedWeapons.Contains(weaponPrefab))
            _ownedWeapons.Add(weaponPrefab);
    }

    public void NewWeapon(GameObject weaponPrefab, int index)
    {
        if (_currentGun != null)
        {
            Debug.Log("Existe un arma equipada");
            if (_ownedWeapons[0] != null && _ownedWeapons[1] != null) // Si tiene 2 principales
            {

                if (_ownedWeapons.Contains(weaponPrefab)) // si la arma que esta pillando ya la tiene
                {
                    if (index >= 0 && index < _ownedWeapons.Count && _ownedWeapons[index] == weaponPrefab) // si la arma que esta pillando es la misma que lleva actualmente
                    {
                        Debug.Log("Misma arma en mismo index");
                        Debug.Log($"arma que intentas añadir: {weaponPrefab} / arma actual: {_currentGun.name} / index en el arma actual: {_ownedWeapons[index]}");
                        EquipWeapon(weaponPrefab, true);
                    }
                    else // si el arma que esta pillando, la tiene pero no en la misma que lleva actualmente
                    {
                        Debug.Log("Misma arma en diferente index");
                        Debug.Log($"arma que intentas añadir: {weaponPrefab} / arma actual: {_currentGun.name} / index en el arma actual: {_ownedWeapons[index]}");
                    }

                }

                else if (!_ownedWeapons.Contains(weaponPrefab)) // si el arma que esta pillando no la tiene en general
                {
                    if (index >= 0 && index < _ownedWeapons.Count && _ownedWeapons[index] != weaponPrefab) // si el arma que esta pillando no la tiene, y esta en otro index
                    {
                        Debug.Log("Diferente arma en el mismo index");
                        Debug.Log($"arma que intentas añadir: {weaponPrefab} / arma actual: {_currentGun.name} / index en el arma actual: {_ownedWeapons[index]}");
                    }
                    else // si el arma que esta pillando no la tien y no esta en el mismo index
                    {
                        Debug.Log("Diferente arma y diferente index");
                        Debug.Log($"arma que intentas añadir: {weaponPrefab} / arma actual: {_currentGun.name} / index en el arma actual: {_ownedWeapons[index]}");
                    }
                }
            }

            if (_ownedWeapons[0] == null || _ownedWeapons[1] == null) // tiene un hueco libre en la arma principal
            { 

            }

            if (_ownedWeapons[2] != null && _ownedWeapons[3] != null) // si tiene 2 secundarias
            { 

            }
            
            if (_ownedWeapons[2] == null || _ownedWeapons[3] == null) // tiene un hueco libre en la arma secundaria
            { 

            }

        }
        else if (_currentGun == null)
        {
            Debug.Log("No hay ningun arma equipada");
        }
    }


    public void SwitchWeapon(int index)
    {
        if (index >= 0 && index < _ownedWeapons.Count)
        {
           // EquipWeapon(_ownedWeapons[index]);
        }
    }
}
