using System;
using NUnit.Framework;
using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private SyncVar<int> _health = new(100);
    [SerializeField] private int _selfLayer, _otherLayer;

    public Action<PlayerID> OnDeath_Server;
    public PlayerID PlayerID => owner.Value;

    public int health => _health;


    protected override void OnSpawned()
    {
        base.OnSpawned();

        var _actualLayer = isOwner ? _selfLayer : _otherLayer;
        SetLayerRecursive(gameObject, _actualLayer);


        if (isOwner)
        {
            InstanceHandler.GetInstance<GameMainView>().UpdateHealth(_health.value);
            _health.onChanged += OnHealthChanged;
        }
    }


    protected override void OnDestroy()
    { 
        base.OnDestroy();

        _health.onChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int _newHealth)
    {
        InstanceHandler.GetInstance<GameMainView>().UpdateHealth(_newHealth);
    }

    private void SetLayerRecursive(GameObject _obj, int _layer)
    {
        _obj.layer = _layer;

        foreach (Transform child in _obj.transform)
        { 
            SetLayerRecursive(child.gameObject, _layer);
        }
    }


    [ServerRpc(requireOwnership:false)]
    public void ChangeHealth(int _amount, RPCInfo _info = default)
    { 
        _health.value += _amount;
        Debug.Log(_amount);

        if (_health.value <= 0)
        {
            if (InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                scoreManager.Addkills(_info.sender);
                if(owner.HasValue)
                    scoreManager.AddDeath(owner.Value);
            }
            OnDeath_Server?.Invoke(owner.Value);
            Destroy(gameObject);
        }


    }
}
