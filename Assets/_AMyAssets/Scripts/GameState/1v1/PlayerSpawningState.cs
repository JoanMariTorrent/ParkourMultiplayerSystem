using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Necesario para Corrutinas

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

        // Iniciamos el proceso de spawn (ahora lo hacemos como corrutina para asegurar tiempos)
        StartCoroutine(SpawnPlayersRoutine());
    }

    private IEnumerator SpawnPlayersRoutine()
    {
        var _spawnedPlayers = new List<PlayerHealth>();
        int _currentSpawnIndex = 0;

        foreach (var _player in networkManager.players)
        {
            var _spawnPoint = _spawnPoints[_currentSpawnIndex];

            // 1. Instanciamos
            var _newPlayer = Instantiate(_playerPrefab, _spawnPoint.position, _spawnPoint.rotation);
            
            // 2. Damos propiedad
            _newPlayer.GiveOwnership(_player);

            // 3. Forzamos la posición usando el sistema KCC para evitar el bug del (0,0,0)
            // Buscamos el componente de movimiento
            var character = _newPlayer.GetComponent<PlayerCharacter>();
            if (character != null)
            {
                // Esperamos un frame para que el motor se inicialice si es necesario
                yield return new WaitForEndOfFrame();
                
                // Forzamos la posición en el Servidor (el KCC server-side)
                character.TeleportTo(_spawnPoint.position, _spawnPoint.rotation);
                
                // IMPORTANTE: Avisamos al Cliente dueño de que se teletransporte ahí también
                // Usamos el ReviveObserversRpc que ya creaste, ya que hace exactamente eso: Teleportar KCC + Resetear vida
                _newPlayer.ReviveObserversRpc(_spawnPoint.position, _spawnPoint.rotation, false);
            }
            else
            {
                // Fallback si no tiene KCC
                _newPlayer.transform.position = _spawnPoint.position;
                _newPlayer.transform.rotation = _spawnPoint.rotation;
            }

            _spawnedPlayers.Add(_newPlayer);
            _currentSpawnIndex++;

            if (_currentSpawnIndex >= _spawnPoints.Count)
                _currentSpawnIndex = 0;
        }

        foreach(var player in _spawnedPlayers)
            Debug.Log($"<color=purple> player spawned: {player.owner}</color>");
            
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