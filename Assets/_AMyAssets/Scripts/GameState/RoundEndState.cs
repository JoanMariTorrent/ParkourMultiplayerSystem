using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class RoundEndState : StateNode<List<PlayerHealth>>
{
    [SerializeField] private int _amountOfRound = 3;
    [SerializeField] StateNode _spawningState;
    [SerializeField] List<PlayerHealth> _players;

    private int _roundCount = 0;
    private WaitForSeconds _delay = new(3);


    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        foreach (var players in data)
        {
            if (players.owner.HasValue)
            {
                _players.Add(players);
            }
        }


        CheckForGameEnd();
    }

    private void CheckForGameEnd()
    {
        _roundCount++;
        Debug.Log("1");
        if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            return;
        Debug.Log("2");

        foreach (var player in _players)
        {
            Debug.Log($"{player}");
            
        }



            //var winner = scoreManager.GetWinner();
            //Debug.Log($"{winner} won this round!");
        


        if (_roundCount >= _amountOfRound)
        {
            machine.Next();
            return;
        }
        else
            StartCoroutine(DelayNextState());
    }

    private IEnumerator DelayNextState()
    {
        yield return _delay;
        machine.SetState(_spawningState);
    }
}
