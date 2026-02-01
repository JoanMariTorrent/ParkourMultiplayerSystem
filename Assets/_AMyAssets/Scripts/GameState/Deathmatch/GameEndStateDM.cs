using PurrNet.StateMachine;
using PurrNet; 
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameEndStateDM : StateNode<List<PlayerHealth>>
{
    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        if(!asServer) return;


        BlockPlayers(data);


        InstanceHandler.TryGetInstance(out ScoreManager scoreManager);
        
        if (scoreManager != null)
        {
            // Crear una lista ordenada segun el jugador y sus stats
            var sortedPlayers = data.Select(
                ph =>
                {
                    var p = ph.GetComponent<Player>();
                    var s = scoreManager.GetPlayerStats(p.owner.Value);
                    return new { Player = p, Stats = s };
                })
                .OrderByDescending(x => x.Stats._kills) // Mayor kills primero
                .ThenBy(x => x.Stats._deaths) // Luego por menor de muertes
                .ThenByDescending(x => x.Stats._damage) // Y por ultimo por mayor de daño
                .ToList();



            foreach(var p in data)
            {
                Player player = p.GetComponent<Player>();
                ShowFinalScreen(player);
                
                // pillar el endgameview del player de la lista de allviews
                EndGameView endGameView = player.canvas._allViews.OfType<EndGameView>().FirstOrDefault();

                if(endGameView != null)
                {
                    foreach(var item in sortedPlayers)
                    {
                        Player statsPlayer = item.Player;

                        ScoreManager.ScoreData stats = item.Stats;


                        string playerName = string.IsNullOrEmpty(statsPlayer.playerName)
                        ? statsPlayer.owner.Value.ToString() 
                        : statsPlayer.playerName;

                        //endGameView.AddPlayerToScore(playerName, stats._kills, stats._deaths, stats._damage);
                        AddPlayerToScoreObserverRPC(player, playerName, stats._kills, stats._deaths, stats._damage);
                    }
                }
                
            }

            // Logica para hacer un top 3
            /*var podium = scoreManager.GetDeathmatchPodium(3);

            Debug.Log("--- FIN DE LA PARTIDA: RESULTADOS ---");

            for (int i = 0; i < podium.Count; i++)
            {
                var entry = podium[i];
                PlayerID playerID = entry.Key;
                var stats = entry.Value;

                Debug.Log($"Puesto #{i + 1}: Jugador {playerID} - Kills: {stats._kills} (Muertes: {stats._deaths})");
                
                // PONER INTERFAZ / CINEMATICA FINAL
            }
            */
        }
    }

    [ObserversRpc(runLocally: true)] private void BlockPlayers(List<PlayerHealth> data)
    {
        foreach(var player in data)
        {
            Player p = player.GetComponent<Player>();
            p.canMove = false;
            p.cameraBlocked = true;
            
            player.SetImmunityRpc(true);
        }
    }

    [ObserversRpc(runLocally: true)] private void ShowFinalScreen(Player player)
    {
        player.canvas.ShowView<EndGameView>(true);        
    }

    [ObserversRpc (runLocally: true)] private void AddPlayerToScoreObserverRPC(Player player, string PlayerName, int kills, int deaths, int damage)
    {
        EndGameView endGameView = player.canvas._allViews.OfType<EndGameView>().FirstOrDefault();

        endGameView.AddPlayerToScore(PlayerName, kills, deaths, damage);
    }
}