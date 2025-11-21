using PurrNet;
using UnityEngine;
using Unity.Cinemachine;


public class WeaponManager : NetworkBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private RecoilCamera recoil;

    public Gun _currentGun;
    [SerializeField] private LastGunEquiped lastGun;
    public SyncList <GameObject> _ownedWeapons = new(ownerAuth: true);
    public SyncList <GameObject> mySync = new(ownerAuth: true);
    [SerializeField] private GameObject weaponInstance = null;
    [SerializeField] private PlayerCharacter playerChar;
    [SerializeField] private Player player;

    void Awake()
    {
        if(!_ownedWeapons.ownerAuth && isOwner)
        {
            _ownedWeapons = new SyncList<GameObject>(true);
        }
    }



    protected override void OnSpawned()
    {
        GetPlayerScript();
    }

    void Update()
    {
        lastGun = playerChar._lastGunEquiped;
    }

    private void EquipWeapon(GameObject weaponPrefab, bool deleteWeapon, bool primaryWeapon, bool groundGun)
    {
        if (deleteWeapon) // Si el arma hay que borrarla
        {
            // Busca el indice del arma que hay que borrar
            Gun gunScript = weaponPrefab.GetComponent<Gun>();
            int currentIndex = IndexHasWeaponOfType(gunScript.weaponID);



            
            if (currentIndex >= 0 && _ownedWeapons[currentIndex] != null) // Si encuentro un arma del mismo tipo, la destruye
            {
                //SwitchWeapon(currentIndex);
                //DropGun();
                Destroy(_ownedWeapons[currentIndex]);
                _ownedWeapons[currentIndex] = null;
                    
            }
            else
            {
                if (primaryWeapon)
                {
                    if (_ownedWeapons[0] != null && _ownedWeapons[1] != null)
                    {
                        Debug.LogAssertionFormat("asdasdasd");
                        Destroy(_ownedWeapons[playerChar.gunToSwitchIndex]);
                        _ownedWeapons[playerChar.gunToSwitchIndex] = null;
                    }
                }
                else if (!primaryWeapon)
                {
                    if (_ownedWeapons[2] != null && _ownedWeapons[3] != null)
                    {
                        Destroy(_ownedWeapons[playerChar.gunToSwitchIndex]);
                        _ownedWeapons[playerChar.gunToSwitchIndex] = null;
                    }
                 }
            }
            
            if (groundGun)
            {
                AddGunFromGround(weaponPrefab);
            }

            else if (!groundGun)
            {
                // Instancia la nueva arma
                InstantiateGun(weaponPrefab);
                Debug.Log($"<color=orange>🧱 Instanciado objeto en escena: {weaponInstance.name}</color>");
            }

            if (currentIndex >= 0) // Si el indice "existe" se guarda la nueva arma en el array de armas obtenidas
            {
                _ownedWeapons[currentIndex] = weaponInstance;
            }

            else // Si el indice "no existe" 
            {
                int indexWeapon = GetWeaponIndex(primaryWeapon);
                _ownedWeapons[indexWeapon] = weaponInstance;
            }
        }

        else if (!deleteWeapon) // Si no hay que borrar el arma
        {
            if (groundGun)
            {
                AddGunFromGround(weaponPrefab);
            }
            else
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
    }

    private void EquipUtility(GameObject utilityPrefab)
    {
        _ownedWeapons[4] = utilityPrefab;
        InstantiateGun(utilityPrefab);
    }


    public void NewWeapon(GameObject weaponPrefab, bool primary, bool utility, bool groundGun)
    {
        // Se generan los todos los espacios del array
        EnsureWeaponSlots();

        Gun gunScript = weaponPrefab.GetComponent<Gun>();
        WeaponID newWeaponID = gunScript.weaponID;

        // Dos bools para ver si es arma principal o secundaria la que se intenta agregar
        bool havePrimary = _ownedWeapons[0] || _ownedWeapons[1];
        bool haveSecondary = _ownedWeapons[2] || _ownedWeapons[3];

        if (primary && havePrimary && !utility)
        {
            if (_ownedWeapons[0] != null && _ownedWeapons[1] != null) // Si tiene 2 principales
            {
                if (groundGun) EquipWeapon(weaponPrefab, true, true, true);
                else EquipWeapon(weaponPrefab, true, true, false); // como tiene 2 armas ya, tendra que destruirla 100%
                Debug.LogWarning("Borrar armas, tienes 2");
            }

            else if (_ownedWeapons[0] == null || _ownedWeapons[1] == null) // tiene un hueco libre en la arma principal
            {
                if (HasWeaponOfType(newWeaponID)) // si la arma que esta pillando ya la tiene
                {
                    if (groundGun) EquipWeapon(weaponPrefab, true, true, true);
                    else EquipWeapon(weaponPrefab, true, true, false);
                    Debug.LogWarning("Borrar arma porque ya la tienes");
                }

                else  // si el arma que esta pillando no la tiene en general
                {
                    if (groundGun) EquipWeapon(weaponPrefab, false, true, true);
                    else EquipWeapon(weaponPrefab, false, true, false);
                    Debug.LogWarning("Añadiendo arma nueva");
                }
            }
        }
        else if (!primary && haveSecondary && !utility)
        {

            if (_ownedWeapons[2] != null && _ownedWeapons[3] != null) // si tiene 2 secundarias
            {
                if (groundGun) EquipWeapon(weaponPrefab, true, false, true);
                else EquipWeapon(weaponPrefab, true, false, false); // como tiene 2 armas ya, tendra que destruirla 100%
            }

            else if (_ownedWeapons[2] == null || _ownedWeapons[3] == null) // tiene un hueco libre en la arma secundaria
            {
                if (HasWeaponOfType(newWeaponID)) // si la arma que esta pillando ya la tiene
                {
                    if (groundGun) EquipWeapon(weaponPrefab, true, false, true);
                    else EquipWeapon(weaponPrefab, true, false, false);
                }

                else // si el arma que esta pillando no la tiene en general
                {
                    if (groundGun) EquipWeapon(weaponPrefab, false, false, true);
                    else EquipWeapon(weaponPrefab, false, false, false);
                }
            }
        }

        else if (utility)
        {
            EquipUtility(weaponPrefab);
        }

        if ((primary && !havePrimary) || (!primary && !haveSecondary)) // Si no tiene ningun arma, tanto principal como secundaria, se le pasa false en destruir y asi instancia una nueva
        {
            if (groundGun) EquipWeapon(weaponPrefab, false, primary, true);
            else EquipWeapon(weaponPrefab, false, primary, false);
        }

    }

    private bool HasWeaponOfType(WeaponID id)
    {
        foreach (var weapon in _ownedWeapons)
        {
            if (weapon == null) continue;
            Gun g = weapon.GetComponent<Gun>();
            if (g != null && g.weaponID == id)
                return true;
        }
        return false;
    }

    private int IndexHasWeaponOfType(WeaponID id)
    {
        for (int i = 0; i < _ownedWeapons.Count; i++)
        {
            var weapon = _ownedWeapons[i];
            if (weapon == null) continue;

            Gun g = weapon.GetComponent<Gun>();
            if (g != null && g.weaponID == id)
                return i;
        }

        return -1;
    }


    private void EnsureWeaponSlots() // Genera todos los slots y los pone en null, para tenerlos creados
    {
        while (_ownedWeapons.Count < 5)
            _ownedWeapons.Add(null);
    }


    private void InstantiateGun(GameObject weaponPrefab)
    {
        if (_currentGun != null)
            _currentGun.gameObject.SetActive(false);


        if(_handTransform == null) { Debug.Log("HAND TRANSFORM ES NUUUUUUUUUUUUUUUUUUUULLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL"); }

        // Instanciar el arma
        weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.SetActive(true);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;
        Debug.Log($"<color=white> Posicion de la nueva arma: {weaponInstance.transform.localPosition} </color>");

        // Obtener script
        _currentGun = weaponInstance.GetComponent<Gun>();

        if (_currentGun == null)
        {
            Debug.LogError("❌ El prefab no tiene componente Gun!");
            return;
        }

        // Configurar arma
        _currentGun.GiveOwnership(owner.Value);
        if (player == null)
            GetPlayerScript();

        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);

        // Buscar índice correcto
        Gun prefabGun = weaponPrefab.GetComponent<Gun>();
        int index = IndexHasWeaponOfType(prefabGun.weaponID);

        if (index < 0)
        {
            bool isPrimary = prefabGun.weaponType == WeaponType.Primary;
            index = GetWeaponIndex(isPrimary);
            if (index < 0)
                index = isPrimary ? 0 : 2;
        }

        // Asegurar tamaño
        while (_ownedWeapons.Count <= index)
            _ownedWeapons.Add(null);

        // Asignar
        _ownedWeapons[index] = weaponInstance;
        Debug.Log($"<color=blue>✅ Instanciada '{_currentGun.name}' en slot {index} y el arma actual es {_currentGun.name} </color>");
        
        SwitchWeapon(index);

        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;
    }



    public void AddGunFromGround(GameObject weaponObject)
    {
        Gun gunScript = weaponObject.GetComponent<Gun>();
        if (gunScript == null) return;

        _currentGun = gunScript;

        _currentGun.transform.SetParent(_handTransform);
        _currentGun.transform.localPosition = Vector3.zero;
        _currentGun.transform.localRotation = Quaternion.identity;

        _currentGun.rb.isKinematic = true;
        _currentGun.rb.useGravity = false;
        Collider col = _currentGun.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        _currentGun.GiveOwnership(owner.Value);

        if(player ==  null)
            GetPlayerScript();
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);

        int indexWeapon = GetWeaponIndex(_currentGun.weaponType == WeaponType.Primary);
        _ownedWeapons[indexWeapon] = weaponObject;


        _currentGun.gameObject.SetActive(true);

        SwitchWeapon(indexWeapon);
    }

    [ObserversRpc(requireServer: false)]
    public void SwitchWeapon(int index) // FALTA ARREGLAR QUE AL CAMBIAR EL ARMA, SE OCULTE LA ANTERIOR Y SE ACTIVE LA NUEVA
    {
        if (index < 0 || index >= _ownedWeapons.Count)
            return;

        GameObject weaponToSwitch = _ownedWeapons[index];

        if (weaponToSwitch == null)
            return;


        if (_currentGun != null && weaponToSwitch == _currentGun.gameObject)
        {
            Debug.Log("<color=yellow>⚠️ Ya tienes equipada esta arma, no se cambia.</color>");
            return;
        }

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
        // Asignar el ownership al jugador que posee esta arma
        _currentGun.GiveOwnership(owner.Value);


        // Setea la posicion y rotacion de la nueva arma
        weaponToSwitch.transform.SetParent(_handTransform);
        weaponToSwitch.transform.localPosition = Vector3.zero;
        weaponToSwitch.transform.localRotation = Quaternion.identity;

        // reconfigurar camara y recoil
        if(player == null)
            GetPlayerScript();
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);

        weaponToSwitch.SetActive(true);

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

    public void UtilityThrowed()
    {
        _ownedWeapons.RemoveAt(4);
        SwitchWeapon(0);
    }

    public void DropGun()
    {
        DoDropGunLogic();
    }

    [ObserversRpc(runLocally: false)]
    private void DoDropGunLogic()
    {
        if (_currentGun == null)
        {
            Debug.LogWarning("<color=red>❌ No hay arma equipada para dropear.</color>");
            return;
        }

        _currentGun.equipedGun = false;

        GameObject droppedGun = _currentGun.gameObject;

        // Activar fisicas
        _currentGun.rb.isKinematic = false;
        _currentGun.rb.useGravity = true;
        _currentGun.rb.AddForce(_handTransform.transform.forward * 5f, ForceMode.Impulse);

        // Quitar Ownership
        _currentGun.GiveOwnership(null);

        // Quitarla del inventario
        int currentIndex = _ownedWeapons.IndexOf(_currentGun.gameObject);
        if (currentIndex >= 0)
        {
            _ownedWeapons[currentIndex] = null;
            Debug.Log($"<color=orange>🪂 Dropeada '{droppedGun.name}' del slot {currentIndex}</color>");
        }
        else
        {
            Debug.LogWarning("<color=red>⚠️ DropGun: No se encontró el arma en _ownedWeapons</color>");
        }

        // Estado del arma fisica en el suelo
        _currentGun.SetDown();

        // No cambiamos de arma todavia, dejamos el _currentGun en null temporalmente
        Gun prevGun = _currentGun;
        _currentGun = null;


        int _case = -1;

        if (playerChar._lastGunEquiped == LastGunEquiped.Primary && _case == -1)
        {
            if (_ownedWeapons[0] != null || _ownedWeapons[1] != null)
            {
                _case = _ownedWeapons[0] != null ? 0 : 1;
                playerChar._lastGunEquiped = LastGunEquiped.Primary;
            }
            else if ((_ownedWeapons[2] != null || _ownedWeapons[3] != null) && _case == -1)
            {
                _case = _ownedWeapons[2] != null ? 2 : 3;
                playerChar._lastGunEquiped = LastGunEquiped.Secondary;
            }
            else
                Debug.Log("Cambiar a cuchillo (principal)");
        }
        else if (playerChar._lastGunEquiped == LastGunEquiped.Secondary && _case == -1)
        {
            if (_ownedWeapons[2] != null || _ownedWeapons[3] != null)
            {
                _case = _ownedWeapons[2] != null ? 2 : 3;
                playerChar._lastGunEquiped = LastGunEquiped.Secondary;
            }
            else if ((_ownedWeapons[0] != null || _ownedWeapons[1] != null) && _case == -1)
            {
                _case = _ownedWeapons[0] != null ? 0 : 1;
                playerChar._lastGunEquiped = LastGunEquiped.Primary;
            }
            else
                Debug.Log("Cambiar a cuchillo (secundaria)");
        }

        if (_case != -1)
        {
            SwitchWeapon(_case);
            Debug.Log(_case);
            Debug.LogWarning(_ownedWeapons[_case]);
        }
        else if (_case == -1)
            Debug.LogWarning("_case es -1");



    }

    private void GetPlayerScript()
    {
        player = GetComponent<Player>();
    }

}