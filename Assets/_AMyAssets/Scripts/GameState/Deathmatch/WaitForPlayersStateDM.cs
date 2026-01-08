using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;

public class WaitForPlayersStateDM : StateNode
{
    [SerializeField] private int _minPlayers = 2;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;


        Debug.Log($"Cantidad de jugadores guardado en memoria: <color=green> {MatchData.PlayerCount} </color>");

        if (MatchData.PlayerCount != 0)
            _minPlayers = MatchData.PlayerCount;
        else
            _minPlayers = 1;
        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        while (networkManager.players.Count < _minPlayers)
            yield return null;
        machine.Next();

    }
}
