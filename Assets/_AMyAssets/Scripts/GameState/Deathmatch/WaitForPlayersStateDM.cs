using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;

public class WaitForPlayersStateDM : StateNode
{
    [SerializeField] private int _minPlayers = 2;
    [SerializeField] private bool testingStart = false;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        if(testingStart == false)
        {
            if (MatchData.PlayerCount != 0)
            _minPlayers = MatchData.PlayerCount;
            else
                _minPlayers = 1;
        }
        
        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        while (networkManager.players.Count < _minPlayers)
            yield return null;

        yield return new WaitForSeconds(1.5f);
        machine.Next();

    }
}
