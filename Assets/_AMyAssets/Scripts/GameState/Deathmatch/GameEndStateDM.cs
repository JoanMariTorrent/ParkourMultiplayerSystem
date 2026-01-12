using PurrNet.StateMachine;
using PurrNet; 
using UnityEngine;

public class GameEndStateDM : StateNode 
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if(!asServer) return;

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