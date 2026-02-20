using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;

public class TutorialSpawningState : StateNode
{
    [SerializeField] private PlayerHealth _playerPrefab;
    [SerializeField] private Transform _tutorialSpawnPoint;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer) return;

        DespawnPlayers();

        var newPlayer = Instantiate(_playerPrefab, _tutorialSpawnPoint.position, _tutorialSpawnPoint.rotation);

        newPlayer.GiveOwnership(networkManager.localPlayer);

        newPlayer.ReviveObserversRpc(_tutorialSpawnPoint.position, _tutorialSpawnPoint.rotation, false);
        newPlayer.GetComponent<Player>().tutorialMode = true;

        var spawnedList = new List<PlayerHealth> { newPlayer };
        machine.Next(spawnedList);
    }

    private void DespawnPlayers()
    {
        var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p != null) Destroy(p.gameObject);
        }
    }
}