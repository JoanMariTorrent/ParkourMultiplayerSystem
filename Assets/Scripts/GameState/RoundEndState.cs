using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using UnityEngine.SceneManagement;

public class RoundEndState : StateNode<PlayerID>
{
    [SerializeField] private int _amountOfRound = 3;
    [SerializeField] StateNode _spawningState;

    private int _roundCount = 0;
    private WaitForSeconds _delay = new(3);


    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;



        


        CheckForGameEnd();
    }

    private void CheckForGameEnd()
    {
        _roundCount++;
        if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            var winner = scoreManager.GetWinner();
            Debug.Log($"{winner} won this round!");
        }


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
