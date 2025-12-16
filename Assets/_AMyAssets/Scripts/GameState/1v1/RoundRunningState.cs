using NUnit.Framework;
using PurrNet;
using PurrNet.StateMachine;
using PurrNet.Transports;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RoundRunningState : StateNode<List<PlayerHealth>>
{

    [SerializeField] private StateNode _spawningState;
    private List<PlayerID> _players = new();



    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        Debug.Log("ASDJIBKASKBASDKBASDASHBDAJHSBDJHASBDJHASBDJHBAHJDBJHASBDJHBASJHDBJHASBD");

        if (!asServer)
            return;

        _players.Clear();

        foreach (var player in data)
        {
            if (player.owner.HasValue)
            {
                _players.Add(player.owner.Value);
            }
            
            player.OnDeath_Server += OnPlayerDeath;
        }
    }

    private void OnPlayerDeath(PlayerID _deadPlayer)
    {
        _players.Remove(_deadPlayer);

        if (_players.Count == 1)
        {
            machine.Next(_players);
        }
        else if (_players.Count <= 0)
        {
            Debug.LogWarning("Han muerto todos los jugadores a la vez");
            machine.SetState(_spawningState);
        }
    }
}
