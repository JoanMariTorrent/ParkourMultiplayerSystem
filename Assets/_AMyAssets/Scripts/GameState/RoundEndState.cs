using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using PurrNet;


public class RoundEndState : StateNode<List<PlayerID>>
{
    [SerializeField] private int _amountOfRound = 3;
    [SerializeField] StateNode _spawningState;
    [SerializeField] SyncDictionary<PlayerID, int> _playersWins = new();

    private int _roundCount = 0;
    private WaitForSeconds _delay = new(3);


    public override void Enter(List<PlayerID> winner, bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        foreach(var winners in winner)
            CheckForGameEnd(winners);
    }

    private void CheckForGameEnd(PlayerID winners)
    {
        _roundCount++;
        if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            return;

        if (!_playersWins.ContainsKey(winners))
        {
            _playersWins[winners] = 0; // Inicializa la clave si no existía
        }

        _playersWins[winners] += 1;
        Debug.Log($"{winners} won this round, and he has {_playersWins[winners]}!");


        foreach (var kvp in _playersWins)
        {
            PlayerID playerID = kvp.Key;
            int wins = kvp.Value;

            if (wins > _amountOfRound)
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
