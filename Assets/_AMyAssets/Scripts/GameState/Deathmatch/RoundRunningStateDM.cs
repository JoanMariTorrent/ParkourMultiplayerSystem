using PurrNet;
using PurrNet.StateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [HideInInspector] public float timerReference;


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
        if (timer > 0) 
        {
            timer -= Time.deltaTime;
            TimerToObservers(timer);
        }
        
        // 4. Fin de la ronda
        if (timer <= 0 && !gameEnded)
        {
            EndRound();
        }
    }

    private void OnPlayerDeath(PlayerID deadPlayerID, string victimName, string killerName)
    { 
        if (gameEnded) return;

        var playerScript = _activePlayers.Find(p => p.owner == deadPlayerID);
        Debug.Log("HOLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        
        if (playerScript != null)
        {
            StartCoroutine(RespawnRoutine(playerScript));
        }

        foreach(var player in _activePlayers)
        {
            CallToSpawnEnemyDeathUI(victimName, killerName, player);
        }
    }

    private IEnumerator RespawnRoutine(PlayerHealth player)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (gameEnded || player == null) yield break;
    
        // Si la lista está vacía por error, usamos la posición del script como emergencia
        Vector3 targetPos = transform.position;
        Quaternion targetRot = transform.rotation;
    
        if (_spawnPoints != null && _spawnPoints.Count > 0)
        {
            // Limpiamos nulos por si acaso algún spawnpoint se destruyó
            var validSpawns = _spawnPoints.Where(s => s != null).ToList();
            if (validSpawns.Count > 0)
            {
                Transform selected = validSpawns[Random.Range(0, validSpawns.Count)];
                targetPos = selected.position;
                targetRot = selected.rotation;
            }
        }
        else 
        {
            Debug.LogError($"[RoundRunningStateDM] ¡No hay spawnpoints asignados en {gameObject.name}!");
        }
    
        player.ReviveObserversRpc(targetPos, targetRot);
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

    [ObserversRpc] public void TimerToObservers(float _timer)
    {
        timerReference = _timer;
    }


    [ObserversRpc(runLocally: true)] public void CallToSpawnEnemyDeathUI(string victimName, string killerName, PlayerHealth player)
    {
        Player playerScript = player.GetComponent<Player>();
        GameMainView GMV = playerScript.canvas._allViews.OfType<GameMainView>().FirstOrDefault();
        GMV.SpawnEnemyDeathUI(victimName, killerName);
        
    }
}