using System;
using PurrNet;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> _scores = new();
    

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        _scores.onChanged += OnScoresChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        InstanceHandler.UnregisterInstance<ScoreManager>();
        _scores.onChanged -= OnScoresChanged;
    }

    private void OnScoresChanged(SyncDictionaryChange<PlayerID, ScoreData> change)
    {
        if (InstanceHandler.TryGetInstance(out ScoreboardView scorebardView))
        {
            scorebardView.SetData(_scores.ToDictionary());
        }
    }

    public void Addkills(PlayerID _playerID)
    {
        CheckForDictionaryEntry(_playerID);

        var _scoreData = _scores[_playerID];
        _scoreData._kills++;
        _scores[_playerID] = _scoreData;
    }

    public void AddDeath(PlayerID _playerID)
    {
        CheckForDictionaryEntry(_playerID);

        var _scoreData = _scores[_playerID];
        _scoreData._deaths++;
        _scores[_playerID] = _scoreData;
    }




    [ServerRpc]
    public void AddDamageServerRpc(PlayerID victimID, int amount, RPCInfo info = default)
    {
        // El atacante REAL es el que llamó al ServerRpc:
        var attackerID = info.sender; // PlayerID del cliente que mandó el RPC

        // Registrar al atacante
        CheckForDictionaryEntry(attackerID);
        var attackerScore = _scores[attackerID];
        attackerScore._damage += amount;
        _scores[attackerID] = attackerScore;

        // Registrar daño a la víctima
        CheckForDictionaryEntry(victimID);
        // aquí podrías restarle vida también si quieres
    }


    public PlayerID GetWinner()
    {
        PlayerID winner = default;
        var highestKills = 0;

        foreach (var score in _scores)
        {
            if (score.Value._kills > highestKills)
            { 
                highestKills  = score.Value._kills;
                winner = score.Key;
            }
        }

        return winner;
    }



    private void CheckForDictionaryEntry(PlayerID _playerID)
    {
        if (!_scores.ContainsKey(_playerID))
            _scores.Add(_playerID, new ScoreData());
    }


    public struct ScoreData
    {
        public int _kills;
        public int _deaths;
        public int _damage;

        public override string ToString()
        {
            return $"K: {_kills}, Deaths: {_deaths}, Damage: {_damage}"; 
        }

    }




}
