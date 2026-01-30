using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> _scores = new();
    public SyncDictionary<PlayerID, int> _playersWins = new SyncDictionary<PlayerID, int>();
    

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

    // -----------------------------------------------
    // --------------- ZONA DE RPCs ------------------
    // -----------------------------------------------

    // 1. Para añadir kills a un jugador
    [ServerRpc(requireOwnership: false)]
    public void Addkills(PlayerID _playerID)
    {
        CheckForDictionaryEntry(_playerID);
        Debug.Log($"<color=blue> Añadiendo kills al jugador: {_playerID} </color>");

        var _scoreData = _scores[_playerID];
        _scoreData._kills++;
        _scores[_playerID] = _scoreData;
    }

    // 2. Para añadir muertes  a un jugador
    [ServerRpc(requireOwnership: false)]
    public void AddDeath(PlayerID _playerID)
    {
        CheckForDictionaryEntry(_playerID);

        var _scoreData = _scores[_playerID];
        _scoreData._deaths++;
        _scores[_playerID] = _scoreData;
    }

    // 3. Para añadir daño  a un jugador
    [ServerRpc(requireOwnership: false)]
    public void AddDamageServerRpc(PlayerID victimID, PlayerID attackerID, int amount)
    {
        CheckForDictionaryEntry(attackerID);
        //var attackerID = info.sender; 

        var attackerScore = _scores[attackerID];
        attackerScore._damage += amount;
        _scores[attackerID] = attackerScore;

        CheckForDictionaryEntry(victimID);
    }

    // 4. Para añadir victorias a un jugador
    [ServerRpc]
    public void AddWins(PlayerID playerID, int wins)
    {
        if (!_playersWins.ContainsKey(playerID))
        {
            _playersWins.Add(playerID, wins);
        }
        else if (_playersWins.ContainsKey(playerID))
        {
            _playersWins[playerID] += wins;
        }

        Debug.Log($"{playerID} won this round, now he have {_playersWins[playerID]} wins!");
    }




    


    public ScoreData GetPlayerStats(PlayerID playerID)
    {
        if(_scores.ContainsKey(playerID))
        {
            return _scores[playerID];
        }

        return new ScoreData();
    }


    public PlayerID GetWinner()
    {
        PlayerID winner = default;
        var highestWins = -1;

        foreach (var entry in _playersWins)
        {
            if (entry.Value > highestWins)
            { 
                highestWins = entry.Value;
                winner = entry.Key;
            }
        }

        return winner;
    }


    public List<PlayerID> GetLosers()
    {
        List<PlayerID> losers = new List<PlayerID>();
        var highestWins = -1;

        foreach(var entry in _playersWins)
        {
            if(entry.Value > highestWins)
            {
                highestWins = entry.Value;
            }
        }

        foreach(var entry in _playersWins)
        {
            if(entry.Value < highestWins)
            {
                losers.Add(entry.Key);
            }
        }

        return losers;
    }



    private void CheckForDictionaryEntry(PlayerID _playerID)
    {
        if (!_scores.ContainsKey(_playerID))
            _scores.Add(_playerID, new ScoreData());
    }
    
    public List<KeyValuePair<PlayerID, ScoreData>> GetDeathmatchPodium(int topCount = 3)
    {
        // 1. Convertimos el SyncDictionary a un Diccionario normal para poder operar con LINQ
        var sortedPlayers = _scores.ToDictionary()
            // ORDENAMIENTO:
            .OrderByDescending(x => x.Value._kills)   // 1 Prioridad: Más Kills
            .ThenBy(x => x.Value._deaths)             // 2 Desempate: Menos Muertes es mejor
            .ThenByDescending(x => x.Value._damage)   // 3 Desempate: Más Daño es mejor
            .Take(topCount)                           // Cogemos solo los 'topCount' (3)
            .ToList();

        return sortedPlayers;
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
