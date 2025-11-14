using System.Collections;
using PurrNet.StateMachine;
using UnityEngine;

public class WaitForPlayersState : StateNode
{
    [SerializeField] private int _minPlayers = 2;

    private void Awake()
    {
        Debug.Log("Starting machine");
    }
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        while (networkManager.players.Count < _minPlayers)
            yield return null;
        machine.Next();

    }
}
