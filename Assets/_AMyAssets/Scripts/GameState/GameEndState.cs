using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class GameEndState : StateNode
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

        if (!InstanceHandler.TryGetInstance(out EndGameView endGameView))
        {
            Debug.LogError("GameEndState failed to get end game view!");
            return;
        }

        if (!InstanceHandler.TryGetInstance(out Canvas gameViewManager))
        {
            Debug.LogError("GameEndState failed to get Canvas script!");
            return;
        }

        //asdasd
        endGameView.SetWinner(winner);
        gameViewManager.ShowView<EndGameView>();
        Debug.Log($"Game has now ended with {winner} as our champion!");
    }
}
