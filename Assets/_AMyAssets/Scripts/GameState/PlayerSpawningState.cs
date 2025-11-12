using NUnit.Framework;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerSpawningState : StateNode
{
    [SerializeField] private PlayerHealth _playerPrefab;
    [SerializeField] private List<Transform> _spawnPoints = new();


    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        DespawnPlayers();

        var _spawnedPlayers = SpawnPlayers();
        machine.Next(_spawnedPlayers);
    }

    

    private List<PlayerHealth> SpawnPlayers()
    {
        var _spawnedPlayers = new List<PlayerHealth>();

        int _currentSpawnIndex = 0;

        foreach (var _player in networkManager.players)
        {
            var _spawnPoint = _spawnPoints[_currentSpawnIndex];

            var _newPlayer = Instantiate(_playerPrefab, _spawnPoint.position, _spawnPoint.rotation);
            _newPlayer.GiveOwnership(_player);

            _spawnedPlayers.Add(_newPlayer);
            _currentSpawnIndex++;
            //_newPlayer.GetComponent<Player>().canMove = false;

            if (_currentSpawnIndex >= _spawnPoints.Count)
                _currentSpawnIndex = 0;
        }

        return _spawnedPlayers;
    }

    private void DespawnPlayers()
    {
        var _allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (_allPlayers == null)
            return;
        foreach (var _player in _allPlayers)
        {
            var player = _player.GetComponent<Player>();
            Destroy(player.canvas.gameObject); 
            Destroy(_player.gameObject);
        }
    }




    public override void Exit(bool asServer)
    { 
        base.Exit(asServer);
    }




}
