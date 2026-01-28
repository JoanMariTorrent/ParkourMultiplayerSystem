using PurrNet.StateMachine;
using PurrNet; 
using UnityEngine;
using System.Collections.Generic;

public class GameEndStateDM : StateNode<List<PlayerHealth>>
{
    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        if(!asServer) return;


        foreach(var player in data)
        {
            Player p = player.GetComponent<Player>();
            p.canMove = false;
            
            player.SetImmunityRpc(true);
        }


        InstanceHandler.TryGetInstance(out ScoreManager scoreManager);
        
        if (scoreManager != null)
        {
            var podium = scoreManager.GetDeathmatchPodium(3);

            Debug.Log("--- FIN DE LA PARTIDA: RESULTADOS ---");

            for (int i = 0; i < podium.Count; i++)
            {
                var entry = podium[i];
                PlayerID playerID = entry.Key;
                var stats = entry.Value;

                Debug.Log($"Puesto #{i + 1}: Jugador {playerID} - Kills: {stats._kills} (Muertes: {stats._deaths})");
                
                // PONER INTERFAZ / CINEMATICA FINAL
            }
        }
    }
}