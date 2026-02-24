using PurrNet;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class WeaponManager : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private float utilityCooldown;
    [Header("References")]
    [SerializeField] private UtilityDatabase utilityDatabase;
    [SerializeField] private Transform _handTransform;
    [SerializeField] private Transform _handTransformWithoutAnim;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private RecoilCamera recoil;
    [SerializeField] private PlayerAnimationHandler animHandler;

    public EquippableItem _currentItem;
    public Gun _currentGun 
{
    get 
    {
        return _currentItem as Gun;
    }
}

    [SerializeField] private LastGunEquiped lastGun;
    
    public SyncList <GameObject> _ownedWeapons = new(ownerAuth: false); 
    [SerializeField] private GameObject weaponInstance = null;
    [SerializeField] private PlayerCharacter playerChar;
    [SerializeField] private Player player;
    [Space][Header("Audios")]
    [SerializeField] private AudioClip[] takeGunSound;
    [SerializeField] private IKFollower leftIKFollower;
    [SerializeField] private IKFollower rightIKFollower;


    private Coroutine utilityCoroutine;


    protected override void OnSpawned()
    {
        GetPlayerScript();
        if(isServer) EnsureWeaponSlots();
    }

    void Update()
    {
        lastGun = playerChar._lastGunEquiped;

        if (isOwner && _currentItem != null)
        {
            bool down = playerChar._requestedShootThisFrame;
            bool held = playerChar._requestedShoot;
            bool up = playerChar._stopShooting;

            _currentItem.UseItem(down, held, up);
        }
    }

    // --- RPCs y LOGICA SERVER ---
    public void RequestPickupItemServerRpc(GameObject itemObject)
    {
        PickupItem(itemObject);
    }

    public void PickupItem(GameObject itemObject)
    {
        if (!isServer || itemObject == null) return;
    
        EquippableItem item = itemObject.GetComponent<EquippableItem>();
        if (item == null) return;
    
        int targetIndex = -1;
        bool shouldDeleteOld = false;
    
        if (item is Gun gun)
        {
            bool isPrimary = gun.weaponType == WeaponType.Primary;
            WeaponID newWeaponID = gun.weaponID;
    
            // Si tengo un Sniper y pillo otro Sniper, suelto el viejo esté donde esté.
            int duplicateIndex = IndexHasWeaponOfType(newWeaponID);
    
            if (duplicateIndex != -1)
            {
                targetIndex = duplicateIndex;
                shouldDeleteOld = true;
            }
            else
            {
                // 2. HAY HUECO LIBRE?
                int emptySlot = GetWeaponIndex(isPrimary);
    
                if (emptySlot != -1)
                {
                    targetIndex = emptySlot;
                    shouldDeleteOld = false;
                }
                else
                {
                    // 3. TODO LLENO: INTERCAMBIO FORZADO
                    int heldIndex = _ownedWeapons.IndexOf(_currentItem?.gameObject);
                    
                    // Si lo que tengo en la mano es del mismo tipo (ej: Rifle pilla Sniper)
                    if (_currentItem is Gun heldGun && heldGun.weaponType == gun.weaponType)
                    {
                        targetIndex = heldIndex;
                    }
                    else
                    {
                        // Si tengo en la mano algo de otro tipo (ej: Pistola pilla Sniper)
                        targetIndex = isPrimary ? 0 : 2;
                    }
                    shouldDeleteOld = true;
                }
            }
        }
        else if (item is Utility utility)
        {
            targetIndex = 4;
            if (_ownedWeapons.Count > 4 && _ownedWeapons[4] != null) shouldDeleteOld = true;
            if(utilityCoroutine != null)
            {
                StopCoroutine(utilityCoroutine);
                utilityCoroutine = null;
            }
        }
    
        // --- EJECUCIÓN ---
    
        // Tirar el arma vieja si el slot está ocupado o si era un duplicado
        if (shouldDeleteOld && _ownedWeapons.Count > targetIndex && _ownedWeapons[targetIndex] != null)
        {
            DropWeaponAtIndex(targetIndex);
        }
    
        // Asegurar tamaño y asignar
        while (_ownedWeapons.Count <= targetIndex) _ownedWeapons.Add(null);
        _ownedWeapons[targetIndex] = itemObject;
    
        // Setup físico y de red
        EquipItemFromGround(item); 
        
        // CAMBIO DE ARMA: Siempre cambiamos al arma recién recogida
        SwitchWeapon(targetIndex, itemObject);
    
        PlayEquipSoundObserversRpc();
    }

    public void NewWeapon(GameObject weaponPrefab, bool primary, bool utility, bool groundGun)
    {
        if (!isServer) return;
        GameObject finalObject = groundGun ? weaponPrefab : Instantiate(weaponPrefab);
        
        PickupItem(finalObject);
    }

    public void AddUtility(GameObject utilityPrefab, bool equipNewUtility)
    {
        if (!isServer) return;
        EnsureWeaponSlots();

        GameObject instance = Instantiate(utilityPrefab, _handTransformWithoutAnim);
        
        

        Utility utilScript = instance.GetComponent<Utility>();
        if (utilScript != null)
        {
            utilScript.GiveOwnership(owner.Value);

            utilScript.SetUp(_playerCamera.transform, playerChar, player, this, animHandler);

            instance.transform.localPosition = utilScript.initPos;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);
        }

        if (_ownedWeapons.Count <= 4) _ownedWeapons.Add(null);
        _ownedWeapons[4] = instance;

        if(equipNewUtility) SwitchWeapon(4, instance);
        else
        {
            PrepareItemPhysics(instance);
            instance.SetActive(false);
        }
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
                Gun currentGunScript = _currentItem as Gun;

                if (primaryWeapon)
                {
                    targetIndex = (currentGunScript != null && currentGunScript.weaponType == WeaponType.Primary) 
                                     ? IndexHasWeaponOfType(currentGunScript.weaponID) 
                                     : 0;
                    if (targetIndex == -1) targetIndex = 0;
                }
                else
                {
                    targetIndex = (currentGunScript != null && currentGunScript.weaponType == WeaponType.Secundary) 
                                     ? IndexHasWeaponOfType(currentGunScript.weaponID) 
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
            PickupItem(weaponPrefab); 
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

    // --- SWITCH WEAPON (POLIMÓRFICO) ---

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
        if (_currentItem != null && weaponToSwitch == _currentItem.gameObject) return;

        for (int i = 0; i < _ownedWeapons.Count; i++)
        {
            if (_ownedWeapons[i] != null) _ownedWeapons[i].SetActive(false);
        }

        weaponToSwitch.SetActive(true);
        
        _currentItem = weaponToSwitch.GetComponent<EquippableItem>();
        
        Rigidbody rb = weaponToSwitch.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        Collider col = weaponToSwitch.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_currentItem != null) _currentItem.GiveOwnership(owner.Value);
        
        if (_currentItem is Gun gunScript)
        {
            if(gunScript.gunAnimHandler != null) weaponToSwitch.transform.SetParent(_handTransform);
            else weaponToSwitch.transform.SetParent(_handTransformWithoutAnim);
            
            // Setup de Arma
            if(player == null) GetPlayerScript();
            gunScript.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this, animHandler);

            if(leftIKFollower != null) leftIKFollower.SetTarget(gunScript.leftHandGrip);
            if(rightIKFollower != null) rightIKFollower.SetTarget(gunScript.rightHandGrip);

            weaponToSwitch.transform.localPosition = gunScript.initPos;

        }
        else if (_currentItem is Utility utilScript)
        {
            weaponToSwitch.transform.SetParent(_handTransformWithoutAnim);

            if(player == null) GetPlayerScript();

            utilScript.SetUp(_playerCamera.transform, playerChar, player, this, animHandler);
            weaponToSwitch.transform.localPosition = utilScript.initPos;
        }
        else
        {
            weaponToSwitch.transform.SetParent(_handTransformWithoutAnim);
            weaponToSwitch.transform.localPosition = Vector3.zero;
        }

        
        weaponToSwitch.transform.localRotation = Quaternion.identity;

        // Llamamos al OnEquip del padre
        if (_currentItem != null) _currentItem.OnEquip();
    }

    [ObserversRpc(requireServer: false)]
    private void PrepareItemPhysics(GameObject obj)
    {
        if (obj == null) return;

        // Físicas: Kinematic y sin gravedad para que no se caiga del jugador
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Colisión: Desactivada para que no empuje al jugador ni a otros objetos
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Ownership: Aseguramos que el dueño del objeto sea el jugador
        var item = obj.GetComponent<EquippableItem>();
        if (item != null) {
            item.GiveOwnership(owner.Value);
            item.OnUnequip();
            }

        
    }

    public void SwitchWeapon(int index) { SwitchWeapon(index, null); }

    // --- RESTO DE FUNCIONES ---

    private void InstantiateGun(GameObject weaponPrefab)
    {
        if (_currentItem != null) _currentItem.gameObject.SetActive(false);

        // Instanciamos
        Gun gunCheck = weaponPrefab.GetComponent<Gun>(); 
        if(gunCheck != null && gunCheck.gunAnimHandler != null) weaponInstance = Instantiate(weaponPrefab, _handTransform);
        else weaponInstance = Instantiate(weaponPrefab, _handTransformWithoutAnim);
        
        weaponInstance.SetActive(true);
        //weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        // Obtenemos el item
        EquippableItem newItem = weaponInstance.GetComponent<EquippableItem>();
        if (newItem == null) return;
        
        newItem.GiveOwnership(owner.Value);
        
        // Si es arma, setup
        if (newItem is Gun newGun)
        {
             if (player == null) GetPlayerScript();
             newGun.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this, animHandler);
        }
    }

    private void EquipItemFromGround(EquippableItem item)
    {
        GameObject itemObj = item.gameObject;
        
        // 1. Configuración de Físicas (Común para todos)
        itemObj.SetActive(true);
        
        Rigidbody rb = itemObj.GetComponent<Rigidbody>();
        if (rb) 
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Collider col = itemObj.GetComponent<Collider>();
        if (col) col.enabled = false;

        // 2. Parenting Inteligente
        // Decidimos dónde va según si tiene animaciones o no
        Transform parentTarget = _handTransformWithoutAnim;

        if (item is Gun g && g.gunAnimHandler != null) 
        {
            parentTarget = _handTransform;
        }
        // Si tienes utilidades con animaciones en el futuro, añade aquí el 'else if'

        itemObj.transform.SetParent(parentTarget);
        itemObj.transform.localRotation = Quaternion.identity;

        // 3. Ownership (Común)
        item.GiveOwnership(owner.Value);

        // 4. Setup Específico (Aquí es donde separamos la lógica única)
        if (item is Gun gunScript)
        {
            if(player == null) GetPlayerScript();
            gunScript.Setup(_playerCamera.transform, _hitLayer, recoil, playerChar, player, this, animHandler);
            itemObj.transform.localPosition = gunScript.initPos;
        }
        else if (item is Utility utilScript)
        {
            // Usamos el Setup de la utilidad
            utilScript.SetUp(_playerCamera.transform, playerChar, player, this, animHandler);
            itemObj.transform.localPosition = utilScript.initPos;
        }
    }
 
    [ObserversRpc(runLocally: true)]
    private void DropWeaponAtIndex(int index)
    {
        if (index < 0 || index >= _ownedWeapons.Count) return;
        GameObject weaponObj = _ownedWeapons[index];
        if (weaponObj == null) return;

        EquippableItem itemScript = weaponObj.GetComponent<EquippableItem>();
        if (itemScript == null) return;

        // Desequipar genérico
        itemScript.OnUnequip();
        
        // Desequipar específico de arma
        if (itemScript is Gun g) g.SetDown();

        weaponObj.transform.SetParent(null);
        weaponObj.SetActive(true);
        
        Rigidbody rb = weaponObj.GetComponent<Rigidbody>();
        if(rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce((transform.forward + Vector3.up).normalized * 3f, ForceMode.Impulse);
        }

        itemScript.GiveOwnership(null);

        if(isServer) _ownedWeapons[index] = null;
        if (_currentItem == itemScript) _currentItem = null;
    }

    private void EquipUtility(GameObject utilityPrefab) 
    { 
        InstantiateGun(utilityPrefab); 
        if(isServer) _ownedWeapons[4] = weaponInstance; 
        SwitchWeapon(4, weaponInstance); 
    }

    private bool HasWeaponOfType(WeaponID id) 
    { 
        foreach (var w in _ownedWeapons) 
        { 
            if (w == null) continue;
            // Solo chequeamos Guns
            Gun g = w.GetComponent<Gun>();
            if (g && g.weaponID == id) return true; 
        }
        return false; 
    }

    private int IndexHasWeaponOfType(WeaponID id) 
    { 
        for (int i = 0; i < _ownedWeapons.Count; i++) 
        { 
             if (_ownedWeapons[i] == null) continue;
             Gun g = _ownedWeapons[i].GetComponent<Gun>();
             if (g && g.weaponID == id) return i; 
        } 
        return -1; 
    }

    private int GetWeaponIndex(bool primaryWeapon) 
    { 
        int start = primaryWeapon ? 0 : 2; 
        int end = primaryWeapon ? 2 : 4; 

        for (int i = start; i < end; i++) 
        { 
            // Si el slot no existe o está vacío
            if (i >= _ownedWeapons.Count || _ownedWeapons[i] == null) return i; 
        } 
        return -1; // Lleno
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
        if (_currentItem == null || _currentItem is Utility) return;
        
        if(isServer) 
            DoDropGunLogic(); 
        else 
            RequestDropGunServerRpc(); 
    }

    [ServerRpc(requireOwnership: true)] private void RequestDropGunServerRpc() { DoDropGunLogic(); }

    [ObserversRpc(runLocally: true)] private void DoDropGunLogic() 
    { 
        if (_currentItem == null) return; // Antes _currentGun

        _currentItem.isEquipped = false; 
        GameObject dropped = _currentItem.gameObject; 

        if (_currentItem is Gun g) g.SetDown();
        else if (_currentItem is Utility u)
        {
            u.SetDown();
            StartUtilityCooldown();
        }

        dropped.transform.SetParent(null); 

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if(rb)
        {
            rb.isKinematic = false; 
            rb.useGravity = true; 
            rb.AddForce(_handTransform.transform.forward * 5f, ForceMode.Impulse); 
        }

        _currentItem.GiveOwnership(null); 
        
        if (isServer) 
        { 
            int idx = _ownedWeapons.IndexOf(dropped); 
            if (idx >= 0) _ownedWeapons[idx] = null; 
        } 
        
        _currentItem = null; 
        
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
            AudioManager.Instance.PlaySound2D(takeGunSound[Random.Range(0, takeGunSound.Length)], type: AudioType.SFX, volume: 0.3f ,pitch: Random.Range(0.98f, 1.02f));
        else
            AudioManager.Instance.PlaySound(takeGunSound[Random.Range(0, takeGunSound.Length)], transform.position , type: AudioType.SFX, volume: 0.3f, pitch: Random.Range(0.98f, 1.02f));
    }

    [ObserversRpc(runLocally: true)] public void DropAllWeaponsOnDeath() 
    { 
        foreach (var w in _ownedWeapons) 
        {  
            if (!w) continue; 
            
            // Usamos item genérico
            EquippableItem item = w.GetComponent<EquippableItem>();
            if (!item) continue; 
            
            if (item is Gun g) g.SetDown();
            else if (item is Utility u)
            {
                u.OnUnequip();
                continue;
            }

            w.transform.SetParent(null); 
            w.SetActive(true); 
            
            Rigidbody rb = w.GetComponent<Rigidbody>();
            if(rb)
            {
                rb.isKinematic = false; 
                rb.useGravity = true; 
                rb.AddForce((Random.insideUnitSphere + Vector3.up).normalized * 4f, ForceMode.Impulse); 
            }
            item.GiveOwnership(null); 
        } 
        _currentItem = null; 
        if (isServer) 
        for (int i = 0; i < _ownedWeapons.Count; i++) 
            _ownedWeapons[i] = null; 
    }

    public void RemoveUtility(GameObject utilityObj)
    {
        // Solo el servidor puede modificar la SyncList y Despawnear
        if (!isServer) return;

        // 1. Buscamos en qué hueco está (normalmente el 4)
        int index = _ownedWeapons.IndexOf(utilityObj);

        if (index != -1)
        {
            _ownedWeapons[index] = null;
            Destroy(utilityObj);

            int next = -1; 
            if (_ownedWeapons[0]) next = 0; 
            else if (_ownedWeapons[1]) next = 1; 
            else if (_ownedWeapons[2]) next = 2; 
            else if (_ownedWeapons[3]) next = 3; 
            if (next != -1) SwitchWeapon(next);
        }
    }

    private void GetPlayerScript() { player = GetComponent<Player>(); }

    [ServerRpc(requireOwnership: false)]
    public void StartUtilityCooldown()
    {
        if(utilityCoroutine == null) utilityCoroutine = StartCoroutine(UtilityCooldown());
    }

    
    private IEnumerator UtilityCooldown()
    {
        yield return new WaitForSeconds(utilityCooldown);

        var newUtility = GetRandomUtility();
        
        Debug.Log($"<color=green>NUEVA UTILIDAD:{newUtility} </color>");
        AddUtility(newUtility.utilityPrefab, false);

        utilityCoroutine = null;
    }


    private UtilityScriptableObject GetRandomUtility()
    {
        var all = utilityDatabase.allUtilities;
        var selected = utilityDatabase.GetRandomUtilityWeighted(all);

        return selected;
    }
}