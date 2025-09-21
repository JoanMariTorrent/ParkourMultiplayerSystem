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

        if (!asServer)
            return;
        Debug.Log("2");
        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        Debug.Log("3");
        while (networkManager.players.Count < _minPlayers)
            yield return null;
        Debug.Log("4");
        machine.Next();

    }
}
