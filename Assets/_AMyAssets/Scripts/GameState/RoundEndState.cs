using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using PurrNet;


public class RoundEndState : StateNode<List<PlayerID>>
{
    [SerializeField] StateNode _spawningState;
    [SerializeField] private int _amountOfRound = 5;

    private WaitForSeconds _delay = new(3);


    public override void Enter(List<PlayerID> winner, bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        if (winner == null)
        {
            Debug.Log("winner null");
            machine.Next();
        }

        foreach (var winners in winner)
                CheckForGameEnd(winners);
    }

    private void CheckForGameEnd(PlayerID winners)
    {
        if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            return;

        scoreManager.AddWins(winners, 1);


        foreach (var kvp in scoreManager._playersWins)
        {
            PlayerID playerID = kvp.Key;
            int wins = kvp.Value;

            if (wins >= _amountOfRound)
            {
                machine.Next();
                return;
            }
            else continue;
        }
        StartCoroutine(DelayNextState());
    }

    private IEnumerator DelayNextState()
    {
        yield return _delay;
        machine.SetState(_spawningState);
    }
}
