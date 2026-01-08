using PurrNet;
using PurrNet.StateMachine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RoundRunningStateDM : StateNode<List<PlayerHealth>>
{
    [SerializeField] private float matchDuration;
    [SerializeField] private float timer;
    private List<PlayerID> _players = new();

    public override void Enter(List<PlayerHealth> data, bool asServer)
    {
        base.Enter(data, asServer);
        if (!asServer) return;
        _players.Clear();
        timer = matchDuration;

        foreach (var player in data)
        { 
            if(player.owner.HasValue) _players.Add(player.owner.Value);

            player.OnDeath_Server += OnPlayerDeath;
        }
        
    }

    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);
        if(!asServer) return;

        timer -= Time.deltaTime;

        if (timer <= 0) 
        {
            Debug.Log("asdasdasdasd FINISHED aihsdbiahbdihad");
        }
    }

    private void OnPlayerDeath(PlayerID _deadPlayer)
    { 

    }
}
