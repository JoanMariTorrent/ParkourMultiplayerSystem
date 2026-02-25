using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerSpawningState : StateNode
{
    [SerializeField] private PlayerHealth _playerPrefab;
    [SerializeField] private List<Transform> _spawnPoints = new();
    [SerializeField] private List<PlayerHealth> _spawnedPlayers = new();

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        DespawnPlayers();
        StartCoroutine(SpawnPlayersRoutine());
    }

    private IEnumerator SpawnPlayersRoutine()
    {
        _spawnedPlayers = new List<PlayerHealth>();

        List<Transform> _availablePoints = new List<Transform>(_spawnPoints);

        foreach (var _player in networkManager.players)
        {
            
            // 1. Añadir a la lista provisional los spawnpoints
            if (_availablePoints.Count == 0)
            {
                _availablePoints = new List<Transform>(_spawnPoints);
            }

            // 2. Elegimos un índice aleatorio de la lista provisional
            int _randomIndex = Random.Range(0, _availablePoints.Count);
            
            // 3. Obtenemos ese punto
            Transform _spawnPoint = _availablePoints[_randomIndex];

            // 4. Lo borramos de la lista provisional 
            _availablePoints.RemoveAt(_randomIndex);

            // 5. Instanciar el jugador 
            var _newPlayer = Instantiate(_playerPrefab, _spawnPoint.position, _spawnPoint.rotation);
            _newPlayer.GiveOwnership(_player);

            var character = _newPlayer.GetComponent<PlayerCharacter>();
            if (character != null)
            {
                yield return new WaitForEndOfFrame();
                
                character.TeleportTo(_spawnPoint.position, _spawnPoint.rotation);
                
                _newPlayer.ReviveObserversRpc(_spawnPoint.position, _spawnPoint.rotation, false);
            }
            else
            {
                _newPlayer.transform.position = _spawnPoint.position;
                _newPlayer.transform.rotation = _spawnPoint.rotation;
            }

            _spawnedPlayers.Add(_newPlayer);
        }

        foreach(var player in _spawnedPlayers)
        
        machine.Next(_spawnedPlayers);
    }

    private void DespawnPlayers()
    {
        var _allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (_allPlayers == null)
            return;
        foreach (var _player in _allPlayers)
        {
            if(_player.gameObject == null)
                continue;
            Destroy(_player.gameObject);
        }
    }

    public override void Exit(bool asServer)
    { 
        base.Exit(asServer);
    }
}