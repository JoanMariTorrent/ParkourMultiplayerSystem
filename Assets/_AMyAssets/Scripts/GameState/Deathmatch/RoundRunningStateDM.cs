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

    private List<PlayerHealth> _activePlayers = new(); 
    private bool gameEnded = false;

    [HideInInspector] public float timerReference => timer;


    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<RoundRunningStateDM>();
    }

    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        if (!asServer) return;

        // 1. Guardamos la lista de jugadores que vienen del SpawningState
        _activePlayers = data;
        _activePlayers.RemoveAll(x => x == null);

        timer = matchDuration;
        gameEnded = false;

        // 2. Nos suscribimos al evento de muerte
        foreach (var player in _activePlayers)
        { 
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

        var playerScript = _activePlayers.Find(p => p.owner == deadPlayerID);
        Debug.Log("HOLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        
        if (playerScript != null)
        {
            StartCoroutine(RespawnRoutine(playerScript));
        }
    }

    private IEnumerator RespawnRoutine(PlayerHealth player)
    {
        // 1. Esperamos el tiempo de respawn 
        yield return new WaitForSeconds(respawnDelay);

        if (gameEnded) yield break;

        // 2. Elegimos un punto de spawn aleatorio
        Transform spawnPoint = transform; 
        if (_spawnPoints.Count > 0)
            spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Count)];

        // 3. REVIVIR Y TELETRANSPORTAR
        player.ReviveObserversRpc(spawnPoint.position, spawnPoint.rotation);

        Debug.Log($"<color=green>Jugador {player.owner} reciclado y respawneado en {spawnPoint.position}.</color>");
    }

    private void EndRound()
    {
        gameEnded = true;
        machine.Next(_activePlayers);
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        
        if (asServer && _activePlayers != null)
        {
            foreach (var player in _activePlayers)
            {
                if (player != null) player.OnDeath_Server -= OnPlayerDeath;
            }
        }
    }
}