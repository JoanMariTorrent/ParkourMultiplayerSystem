using UnityEngine;
using TMPro;
using PurrNet;

public class GameMainView : View
{
    [SerializeField] private TMP_Text _healthText;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<GameMainView>();
    }


    public override void OnHide()
    {
        
    }

    public override void OnShow()
    {
       
    }


    public void UpdateHealth(int _health)
    {
        if (_health < 0)
        { 
            _health = 0;
        }
        _healthText.text = _health.ToString();
    }
}
