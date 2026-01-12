using PurrNet;
using PurrNet.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundRunningStateDM : StateNode<List<PlayerHealth>>
{
    [Header("Ajustes de Partida")]
    [SerializeField] private float matchDuration = 120f;
    [SerializeField] private float timer;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Referencias")]
    [SerializeField] private List<Transform> _spawnPoints = new();

    // Lista de jugadores activos (para gestionar eventos y respawns)
    private List<PlayerHealth> _activePlayers = new(); 
    private bool gameEnded = false;

    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        if (!asServer) return;

        // 1. Guardamos la lista de jugadores que vienen del SpawningState
        _activePlayers = data;
        _activePlayers.RemoveAll(x => x == null); // Limpieza de seguridad

        timer = matchDuration;
        gameEnded = false;

        // 2. Nos suscribimos al evento de muerte
        foreach (var player in _activePlayers)
        { 
            // Como usamos "Reciclaje" (no destruimos el objeto), 
            // nos suscribimos una vez aquí y sirve para toda la ronda.
            player.OnDeath_Server += OnPlayerDeath;
        }
    }

    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);
        if(!asServer) return;
        
        // 3. Lógica del tiempo
        if (timer > 0) timer -= Time.deltaTime;
        
        // 4. Fin de la ronda
        if (timer <= 0 && !gameEnded)
        {
            EndRound();
        }
    }

    private void OnPlayerDeath(PlayerID deadPlayerID)
    { 
        if (gameEnded) return;

        // Buscamos el script PlayerHealth correspondiente al ID del muerto
        var playerScript = _activePlayers.Find(p => p.owner == deadPlayerID);
        
        if (playerScript != null)
        {
            // Iniciamos la rutina de Respawn
            StartCoroutine(RespawnRoutine(playerScript));
        }
    }

    private IEnumerator RespawnRoutine(PlayerHealth player)
    {
        // 1. Esperamos el tiempo de respawn (mientras el jugador ve la killcam o pantalla negra)
        yield return new WaitForSeconds(respawnDelay);

        if (gameEnded) yield break;

        // 2. Elegimos un punto de spawn aleatorio
        Transform spawnPoint = transform; // Fallback por seguridad
        if (_spawnPoints.Count > 0)
            spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Count)];

        // 3. REVIVIR Y TELETRANSPORTAR
        player.ReviveObserversRpc(spawnPoint.position, spawnPoint.rotation);

        Debug.Log($"<color=green>Jugador {player.owner} reciclado y respawneado en {spawnPoint.position}.</color>");
    }

    private void EndRound()
    {
        gameEnded = true;
        machine.Next();
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        
        // Limpieza: Nos desuscribimos de los eventos al terminar la ronda
        if (asServer && _activePlayers != null)
        {
            foreach (var player in _activePlayers)
            {
                if (player != null) player.OnDeath_Server -= OnPlayerDeath;
            }
        }
    }
}