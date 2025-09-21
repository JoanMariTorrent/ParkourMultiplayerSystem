using System.Collections;
using PurrNet;
using TMPro;
using UnityEngine;

public class EndGameView : View
{
    [SerializeField] private float _fadeDuration = 1f; 
    [SerializeField] private TMP_Text _winnerText;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<EndGameView>();
    }

    public void SetWinner(PlayerID winner)
    { 
        _winnerText.text = $"Player {winner} wins the game!";
    }


    public override void OnHide()
    {
        
    }

    public override void OnShow()
    {
        
    }


}
