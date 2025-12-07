using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class GameEndState : StateNode<List<PlayerID>>
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            Debug.LogError("GameEndState failed to get scoremanager!");
            return;
        }

        var winner = scoreManager.GetWinner();
        List<PlayerID> losers = scoreManager.GetLosers();
        
        
        if (winner == default)
        {
            Debug.LogError("GameEndState failed to get winner!");
            return;
        }

        var winnerPlayerScript = PlayerRegistry.AllPlayers.FirstOrDefault(p => p.owner == winner);

        if(winnerPlayerScript != null)
        {
            Debug.Log($"Game has now ended with {winner} as our champion!");
            winnerPlayerScript.TargetShowFinalScreen(winner, true);
        }

        else
        {
            Debug.LogError("¡Tenemos una ID de ganador pero no encontramos su script Player!");
        }
    }
}
