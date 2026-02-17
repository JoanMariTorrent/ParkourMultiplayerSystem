using System.Collections.Generic;
using System.ComponentModel;
using PurrLobby;
using PurrNet;
using UnityEngine;

public class PlayersSpawnController : NetworkBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private List<GameObject> players = new List<GameObject>();

    void OnEnable()
    {
        lobbyManager.OnRoomJoined.AddListener(RefreshFromLobby);
        lobbyManager.OnRoomUpdated.AddListener(RefreshFromLobby);
        lobbyManager.OnRoomLeft.AddListener(ClearAllMeshes);
        
        ClearAllMeshes();
    }


    private void OnDisable()
    {
        // Limpieza de listeners
        lobbyManager.OnRoomJoined.RemoveListener(RefreshFromLobby);
        lobbyManager.OnRoomUpdated.RemoveListener(RefreshFromLobby);
        lobbyManager.OnRoomLeft.RemoveListener(ClearAllMeshes);
    }


    private void RefreshFromLobby(Lobby lobby)
    {
        if(!lobby.IsValid) return;

        int currentPlayers = lobby.Members.Count;
        
        for (int i = 0; i < players.Count; i++)
        {
            // Se activa si el índice es menor que la cantidad de miembros en la lobby
            players[i].SetActive(i < currentPlayers);
        }

        Debug.Log($"La cantidad de jugadores actuales en la lobby : {lobby.Name} es de : {lobby.Members.Count}");
    }

    private void ClearAllMeshes()
    {
        foreach (var mesh in players)
        {
            mesh.SetActive(false);
        }
    }

}
