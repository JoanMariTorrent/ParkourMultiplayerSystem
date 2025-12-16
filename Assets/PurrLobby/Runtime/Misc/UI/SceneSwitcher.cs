using PurrLobby;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MatchData
{
    public static List<LobbyUser> Players = new List<LobbyUser>();

    public static int PlayerCount = 0;
}

namespace PurrLobby
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [PurrScene, SerializeField] private string nextScene;

        public void SwitchScene()
        {
            MatchData.Players = new List<LobbyUser>(lobbyManager.CurrentLobby.Members);
            MatchData.PlayerCount = lobbyManager.CurrentLobby.Members.Count;

            lobbyManager.SetLobbyStarted();
            SceneManager.LoadSceneAsync(nextScene);
        }
    }
}
