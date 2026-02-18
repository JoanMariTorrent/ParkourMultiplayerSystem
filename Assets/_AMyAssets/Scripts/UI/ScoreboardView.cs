using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class ScoreboardView : View
{
    [SerializeField] private Transform scoreboardEntriesParent;
    [SerializeField] private ScoreboardEntry scoreboardEntryPrefab;

    [SerializeField] private Canvas _gameViewManager;
    [SerializeField] private Player player;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        
    }

    private void Start()
    {
        if(_gameViewManager == null) _gameViewManager = InstanceHandler.GetInstance<Canvas>();
    }

    private void OnDestroy()
    {
        //InstanceHandler.UnregisterInstance<ScoreboardView>();

        if (InstanceHandler.TryGetInstance(out ScoreboardView registeredView))
        {
            if (registeredView == this)
            {
                InstanceHandler.UnregisterInstance<ScoreboardView>();
            }
        }
    }

    public void SetData(Dictionary<PlayerID, ScoreManager.ScoreData> data)
    {
        foreach (Transform children in scoreboardEntriesParent)
        { 
            Destroy(children.gameObject);
        }


        foreach (var playerscore in data)
        {
            var entry = Instantiate(scoreboardEntryPrefab, scoreboardEntriesParent);
            entry.SetData(playerscore.Key.id.ToString(), playerscore.Value._kills, playerscore.Value._deaths, playerscore.Value._damage);         
        }
    }


    private void Update()
    {
        if (player._inputActions.GamePlay.OpenScoreBoard.IsPressed())
            _gameViewManager.ShowView<ScoreboardView>(false);
        if (player._inputActions.GamePlay.OpenScoreBoard.WasReleasedThisFrame())
            _gameViewManager.HideView<ScoreboardView>();


    }


    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }

    
}
