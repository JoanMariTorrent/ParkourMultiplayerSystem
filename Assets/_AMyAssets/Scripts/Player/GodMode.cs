using PurrNet;
using UnityEngine;
using Steamworks;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class GodMode : NetworkBehaviour
{

    private const ulong DEV_STEAM_ID = 76561198355953706;

    [SerializeField] private bool isGodMode = false;
    [SerializeField] private CinemachineCamera playerCamera;

    private PlayerCharacter player;
    private Rigidbody rb;

    void Start()
    {
        if (isOwner)
        {
            player = GetComponent<PlayerCharacter>();
            rb = GetComponent<Rigidbody>();
            if (player == null)
                Debug.LogWarning("GodMode: no se encontró PlayerCharacter en el mismo GameObject.");
        }
    }

    public void Update()
    {
        if (!isOwner) return;
        ulong mySteamID = SteamUser.GetSteamID().m_SteamID;
        if (mySteamID != DEV_STEAM_ID) return;

        if (Input.GetKeyDown(KeyCode.F10))
        {
            isGodMode = !isGodMode;
            player.ToggleGodMode(isGodMode);
        }

        if (isGodMode)
        {
            player.HandleFreeFly();
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }

    }


    public void Teleport(Vector3 position)
    {
        player.SetPosition(position);
    }

    
    
    
}
