using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using static ScoreManager;

public class ScoreboardView : View
{
    [SerializeField] private Transform scoreboardEntriesParent;
    [SerializeField] private ScoreboardEntry scoreboardEntryPrefab;

    private Canvas _gameViewManager;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        
    }

    private void Start()
    {
        _gameViewManager = InstanceHandler.GetInstance<Canvas>();
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ScoreboardView>();
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
        if (Input.GetKeyDown(KeyCode.Tab))
            _gameViewManager.ShowView<ScoreboardView>(false);
        if (Input.GetKeyUp(KeyCode.Tab))
            _gameViewManager.HideView<ScoreboardView>();


    }


    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }

    
}
