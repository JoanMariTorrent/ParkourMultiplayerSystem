using NUnit.Framework;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using Steamworks;
using TMPro;

public class WeaponManager : NetworkBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private RecoilCamera recoil;

    public Gun _currentGun;
    [SerializeField] private SyncList<GameObject> _ownedWeapons = new();
    [SerializeField] private GameObject weaponInstance = null;

    private void Awake()
    {
        enabled = isOwner;
    }
    private void EquipWeapon(GameObject weaponPrefab, bool deleteWeapon, bool primaryWeapon)
    {
        if (deleteWeapon) // Si el arma hay que borrarla
        {
            // Busca el indice del arma que hay que borrar y lo elimina
            int currentIndex = _ownedWeapons.IndexOf(_currentGun.gameObject);
            Destroy(_currentGun.gameObject);

            // Instancia la nueva arma
            InstantiateGun(weaponPrefab);

            if (currentIndex >= 0) // Si el indice "existe" se guarda la nueva arma en el array de armas obtenidas
            {
                _ownedWeapons[currentIndex] = weaponPrefab;
                Debug.Log($"Arma reemplazada en el slot {currentIndex}");
            }
            else // Si el indice "no existe" 
            {
                int indexWeapon = GetWeaponIndex(primaryWeapon);
                _ownedWeapons[indexWeapon] = weaponPrefab;
            }
        }

        else if (!deleteWeapon) // Si no hay que borrar el arma
        {
            if (primaryWeapon) // Arma principal
            {
                // Se busca el indice mas bajo, y se guarda el arma nueva en el indice buscado
                int indexWeapon = GetWeaponIndex(true);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");

                // Se instancia la nueva arma
                InstantiateGun(weaponPrefab);
            }
            else if (!primaryWeapon) // Arma secundaria
            {
                // Se busca el indice mas bajo, y se guarda el arma nueva en el indice buscado
                int indexWeapon = GetWeaponIndex(false);
                _ownedWeapons[indexWeapon] = weaponPrefab;
                Debug.Log($"{_ownedWeapons[indexWeapon].name} {indexWeapon}");

                // Se instancia la nueva arma
                InstantiateGun(weaponPrefab);
            }


        }
    }
    

    public void NewWeapon(GameObject weaponPrefab, bool primary)
    {
        // Se generan los todos los espacios del array
        EnsureWeaponSlots();

        // Dos bools para ver si es arma principal o secundaria la que se intenta agregar
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

        if (!havePrimary || !haveSecondary) // Si no tiene ningun arma, tanto principal como secundaria, se le pasa false en destruir y asi instancia una nueva
        {
            EquipWeapon(weaponPrefab, false, primary);
        }

    }


    private void EnsureWeaponSlots() // Genera todos los slots y los pone en null, para tenerlos creados
    {
        while (_ownedWeapons.Count < 4)
            _ownedWeapons.Add(null);
    }


    private void InstantiateGun(GameObject weaponPrefab) // Instancia armas nuevas, tanto para cuando hay que eliminar una porque no hay hueco y para cuando no hay armas
    {
        if (_currentGun != null)
            _currentGun.gameObject.SetActive(false);

        // Instancia el arma y la setea en la posicion y rotacion correcta
        weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        // Guarda el script de la nueva arma como el script actual
        _currentGun = weaponInstance.GetComponent<Gun>();

        // Setea la camara, la hitlayer y el recoil del arma en su script
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);

        // Actualiza el slot con la instancia en la escena
        int index = _ownedWeapons.IndexOf(weaponPrefab);
        if (index >= 0)
            _ownedWeapons[index] = weaponInstance;

        SwitchWeapon(index);
    }
    
    [ObserversRpc]
    public void SwitchWeapon(int index) // FALTA ARREGLAR QUE AL CAMBIAR EL ARMA, SE OCULTE LA ANTERIOR Y SE ACTIVE LA NUEVA
    {
        if (index < 0 || index >= _ownedWeapons.Count)
            return;

        GameObject weaponToSwitch = _ownedWeapons[index];

        if (weaponToSwitch == null)
            return;

        // ocultar todas las armas
        for (int i = 0; i < _ownedWeapons.Count; i++)
        {
            if (_ownedWeapons[i] != null)
            {
                _ownedWeapons[i].SetActive(false); 
            }
        }


        // Activar el arma
        weaponToSwitch.SetActive(true);
        _currentGun = weaponToSwitch.GetComponent<Gun>();

        // Setea la posicion y rotacion de la nueva arma
        weaponToSwitch.transform.localPosition = Vector3.zero;
        weaponToSwitch.transform.localRotation = Quaternion.identity;

        // reconfigurar camara y recoil
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil);

        Debug.Log($"Cambio de arma a {_currentGun.name} en el slot {index}");
    }

    private int GetWeaponIndex(bool primaryWeapon) // Un int para devolver el hueco libre dentro del array _ownedWeapons dependiendo de si es principal o secundaria 
    {
        // Setear inicio y final del loop
        int startIndex = primaryWeapon ? 0 : 2;
        int endIndex = primaryWeapon ? 2 : 4;

        // loop para encontrar el indice libre mas bajo
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
