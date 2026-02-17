using System.Collections.Generic;
using System.ComponentModel;
using PurrLobby;
using UnityEngine;

public class PlayersSpawnController : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<GameObject> players = new List<GameObject>();
    [SerializeField] private int playersInLobby;
    private Lobby lobby;

    void OnEnable()
    {
        lobbyManager.OnRoomJoined.AddListener(OnJoined);
        lobbyManager.OnRoomLeft.AddListener(OnLeft);

        foreach(var p in players)
            p.SetActive(false);
    }



    private void OnJoined(Lobby _lobby)
    {
        this.lobby = _lobby; 
        Debug.Log($"La cantidad de jugadores actuales en la lobby : {lobby.Name} es de : {lobby.Members.Count}");
        players[lobby.Members.Count - 1].SetActive(true);
        
    }

    private void OnLeft()
    {
        Debug.Log($"Lobby: {lobby.Name} Jugadores: {lobby.Members.Count}");
        players[lobby.Members.Count - 1].SetActive(false);
    }

}
