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
        
        if (winner == default)
        {
            Debug.LogError("GameEndState failed to get winner!");
            return;
        }

        foreach (var playerScript in PlayerRegistry.AllPlayers)
        {
            bool isThisPlayerTheWinner = (playerScript.owner.Value == winner);

            playerScript.TargetShowFinalScreen(playerScript.owner.Value, isThisPlayerTheWinner);
        }
    }
}
