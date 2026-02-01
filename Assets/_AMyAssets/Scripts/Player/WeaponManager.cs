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
    
    public SyncList <GameObject> _ownedWeapons = new(ownerAuth: false); 
    //public SyncList <GameObject> mySync = new(ownerAuth: true);
    [SerializeField] private GameObject weaponInstance = null;
    [SerializeField] private PlayerCharacter playerChar;
    [SerializeField] private Player player;
    [Space][Header("Audios")]
    [SerializeField] private AudioClip[] takeGunSound;

    protected override void OnSpawned()
    {
        GetPlayerScript();
        if(isServer) EnsureWeaponSlots();
    }

    void Update()
    {
        lastGun = playerChar._lastGunEquiped;
    }

    // --- RPCs y LOGICA SERVER ---

    [ServerRpc(requireOwnership: true)] 
    public void RequestPickupGunServerRpc(GameObject gunObject, bool isPrimary, bool isUtility)
    {
        NewWeapon(gunObject, isPrimary, isUtility, true);
    }

    public void NewWeapon(GameObject weaponPrefab, bool primary, bool utility, bool groundGun)
    {
        if (!isServer) return;

        EnsureWeaponSlots();
        Gun gunScript = weaponPrefab.GetComponent<Gun>();
        if(gunScript == null) return;

        WeaponID newWeaponID = gunScript.weaponID;
        bool havePrimary = _ownedWeapons[0] || _ownedWeapons[1];
        bool haveSecondary = _ownedWeapons[2] || _ownedWeapons[3];
        bool shouldDelete = false;

        if (primary && !utility)
        {
            if (havePrimary)
            {
                if ((_ownedWeapons[0] != null && _ownedWeapons[1] != null) || HasWeaponOfType(newWeaponID))
                    shouldDelete = true;
            }
        }
        else if (!primary && !utility)
        {
            if (haveSecondary)
            {
                if ((_ownedWeapons[2] != null && _ownedWeapons[3] != null) || HasWeaponOfType(newWeaponID))
                    shouldDelete = true;
            }
        }
        
        EquipWeapon(weaponPrefab, shouldDelete, primary, groundGun);
        PlayEquipSoundObserversRpc();
    }

    private void EquipWeapon(GameObject weaponPrefab, bool deleteWeapon, bool primaryWeapon, bool groundGun)
    {
        int targetIndex = -1;
        GameObject finalWeaponObject = null;

        if (deleteWeapon)
        {
            Gun gunScript = weaponPrefab.GetComponent<Gun>();
            targetIndex = IndexHasWeaponOfType(gunScript.weaponID);

            if (targetIndex == -1)
            {
                if (primaryWeapon)
                {
                    targetIndex = (_currentGun != null && _currentGun.weaponType == WeaponType.Primary) 
                                     ? IndexHasWeaponOfType(_currentGun.weaponID) 
                                     : 0;
                    if (targetIndex == -1) targetIndex = 0;
                }
                else
                {
                    targetIndex = (_currentGun != null && _currentGun.weaponType == WeaponType.Secundary) 
                                     ? IndexHasWeaponOfType(_currentGun.weaponID) 
                                     : 2;
                    if (targetIndex == -1) targetIndex = 2;
                }
            }

            if (targetIndex >= 0 && _ownedWeapons[targetIndex] != null)
            {
                DropWeaponAtIndex(targetIndex);
            }
        }
        else
        {
            targetIndex = GetWeaponIndex(primaryWeapon);
        }

        if (groundGun)
        {
            AddGunFromGround(weaponPrefab); 
            finalWeaponObject = weaponPrefab;
        }
        else
        {
            InstantiateGun(weaponPrefab);
            finalWeaponObject = weaponInstance;
        }

        

        if (targetIndex >= 0)
        {
            while (_ownedWeapons.Count <= targetIndex) _ownedWeapons.Add(null);
            _ownedWeapons[targetIndex] = finalWeaponObject;
            SwitchWeapon(targetIndex, finalWeaponObject);
        }
    }

    // --- SWITCH WEAPON ---

    [ObserversRpc(requireServer: false)]
    public void SwitchWeapon(int index, GameObject forcedWeapon = null) 
    {
        GameObject weaponToSwitch = forcedWeapon;

        if (weaponToSwitch == null)
        {
            if (index >= 0 && index < _ownedWeapons.Count)
                weaponToSwitch = _ownedWeapons[index];
        }

        if (weaponToSwitch == null) return;
        if (_currentGun != null && weaponToSwitch == _currentGun.gameObject) return;

        for (int i = 0; i < _ownedWeapons.Count; i++)
        {
            if (_ownedWeapons[i] != null) _ownedWeapons[i].SetActive(false);
        }

        weaponToSwitch.SetActive(true);
        _currentGun = weaponToSwitch.GetComponent<Gun>();
        
        if (_currentGun.rb != null)
        {
            _currentGun.rb.isKinematic = true;
            _currentGun.rb.useGravity = false;
        }
        Collider col = _currentGun.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        _currentGun.GiveOwnership(owner.Value);
        
        weaponToSwitch.transform.SetParent(_handTransform);
        weaponToSwitch.transform.localPosition = Vector3.zero;
        weaponToSwitch.transform.localRotation = Quaternion.identity;

        if(player == null) GetPlayerScript();
        
        _currentGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);

        Debug.Log($"Cambio de arma a {_currentGun.name} en el slot {index}");
    }

    public void SwitchWeapon(int index) { SwitchWeapon(index, null); }

    // --- RESTO DE FUNCIONES ---

    private void InstantiateGun(GameObject weaponPrefab)
    {
        if (_currentGun != null) _currentGun.gameObject.SetActive(false);
        weaponInstance = Instantiate(weaponPrefab, _handTransform);
        weaponInstance.SetActive(true);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;
        Gun newGun = weaponInstance.GetComponent<Gun>();
        if (newGun == null) return;
        newGun.GiveOwnership(owner.Value);
        if (player == null) GetPlayerScript();
        newGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);
    }

    public void AddGunFromGround(GameObject weaponObject)
    {
        Gun gunScript = weaponObject.GetComponent<Gun>();
        if (gunScript == null) return;
        
        gunScript.transform.SetParent(_handTransform);
        gunScript.transform.localPosition = Vector3.zero;
        gunScript.transform.localRotation = Quaternion.identity;
        
        gunScript.rb.isKinematic = true;
        gunScript.rb.useGravity = false;
        
        Collider col = gunScript.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        gunScript.GiveOwnership(owner.Value);
        if(player == null) GetPlayerScript();
        gunScript.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this);
        gunScript.gameObject.SetActive(true);
    }

    [ObserversRpc(runLocally: true)]
    private void DropWeaponAtIndex(int index)
    {
        if (index < 0 || index >= _ownedWeapons.Count) return;
        GameObject weaponObj = _ownedWeapons[index];
        if (weaponObj == null) return;

        Gun gunScript = weaponObj.GetComponent<Gun>();
        if (gunScript == null) return;

        gunScript.SetDown();
        weaponObj.transform.SetParent(null);
        weaponObj.SetActive(true);
        
        gunScript.rb.isKinematic = false;
        gunScript.rb.useGravity = true;
        gunScript.rb.AddForce((transform.forward + Vector3.up).normalized * 3f, ForceMode.Impulse);

        gunScript.GiveOwnership(null);

        if(isServer) _ownedWeapons[index] = null;
        if (_currentGun == gunScript) _currentGun = null;
    }

    private void EquipUtility(GameObject utilityPrefab) 
    { 
        InstantiateGun(utilityPrefab); 
        if(isServer) _ownedWeapons[4] = weaponInstance; SwitchWeapon(4, weaponInstance); 
    }
    private bool HasWeaponOfType(WeaponID id) 
    { 
        foreach (var w in _ownedWeapons) 
        { 
            if (w && w.GetComponent<Gun>().weaponID == id) return true; 
        }
        return false; 
    }
    private int IndexHasWeaponOfType(WeaponID id) 
    { 
        for (int i = 0; i < _ownedWeapons.Count; i++) 
        { 
            if (_ownedWeapons[i] && _ownedWeapons[i].GetComponent<Gun>().weaponID == id) return i; 
        } 
        return -1; 
    }
    private int GetWeaponIndex(bool primaryWeapon) 
    { 
        int start = primaryWeapon ? 0 : 2; 
        int end = primaryWeapon ? 2 : 4; 
        for (int i = start; i < end; i++) 
        { 
            while (_ownedWeapons.Count <= i) 
                _ownedWeapons.Add(null); 
            if (_ownedWeapons[i] == null) return i; 
        } 
        return -1; 
    }
    private void EnsureWeaponSlots() 
    { 
        while (_ownedWeapons.Count < 5) 
            _ownedWeapons.Add(null); 
    }
    public void UtilityThrowed() 
    { 
        if(isServer) 
        { 
            _ownedWeapons.RemoveAt(4); 
            _ownedWeapons.Insert(4, null); 
        } 
        SwitchWeapon(0); 
    }
    public void DropGun() 
    { 
        if(isServer) 
            DoDropGunLogic(); 
        else 
            RequestDropGunServerRpc(); 
    }
    [ServerRpc(requireOwnership: true)] private void RequestDropGunServerRpc() { DoDropGunLogic(); }
    [ObserversRpc(runLocally: true)] private void DoDropGunLogic() 
    { 
        if (_currentGun == null) 
            return;
        _currentGun.equipedGun = false; 
        GameObject dropped = _currentGun.gameObject; 
        _currentGun.SetDown(); 
        dropped.transform.SetParent(null); 
        _currentGun.rb.isKinematic = false; 
        _currentGun.rb.useGravity = true; 
        _currentGun.rb.AddForce(_handTransform.transform.forward * 5f, ForceMode.Impulse); 
        _currentGun.GiveOwnership(null); 
        if (isServer) 
        { 
            int idx = _ownedWeapons.IndexOf(_currentGun.gameObject); 
            if (idx >= 0) 
                _ownedWeapons[idx] = null; 
        } 
        _currentGun = null; 
        int next = -1; 
        if (_ownedWeapons[0]) next = 0; 
        else if (_ownedWeapons[1]) next = 1; 
        else if (_ownedWeapons[2]) next = 2; 
        else if (_ownedWeapons[3]) next = 3; 
        if (next != -1) SwitchWeapon(next); 
    }
    [ObserversRpc(runLocally: true)] private void PlayEquipSoundObserversRpc() 
    { 
        if (takeGunSound.Length <= 0) return; 
        
        
        if(isOwner)
            AudioManager.Instance.PlaySound2D(takeGunSound[Random.Range(0, takeGunSound.Length)], pitch: Random.Range(0.98f, 1.02f));

        else
            AudioManager.Instance.PlaySound(takeGunSound[Random.Range(0, takeGunSound.Length)], transform.position , pitch: Random.Range(0.98f, 1.02f));
    }
    [ObserversRpc(runLocally: true)] public void DropAllWeaponsOnDeath() 
    { 
        foreach (var w in _ownedWeapons) 
        {  
            if (!w) 
                continue; 
            Gun g = w.GetComponent<Gun>(); 
            if (!g) 
                continue; 
            g.SetDown(); 
            w.transform.SetParent(null); 
            w.SetActive(true); 
            g.rb.isKinematic = false; 
            g.rb.useGravity = true; 
            g.rb.AddForce((Random.insideUnitSphere + Vector3.up).normalized * 4f, ForceMode.Impulse); 
            g.GiveOwnership(null); 
        } 
        _currentGun = null; 
        if (isServer) 
        for (int i = 0; i < _ownedWeapons.Count; i++) 
            _ownedWeapons[i] = null; 
    }
    private void GetPlayerScript() { player = GetComponent<Player>(); }
}