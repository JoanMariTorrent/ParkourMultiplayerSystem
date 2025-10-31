using UnityEngine;
using TMPro;
using PurrNet;
using UnityEngine.Rendering;

public class GameMainView : View
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _ammoText;


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

    public void UpdateAmmo(int _ammo, int _reloadsAmmo)
    {
        if (_ammo >= 0)
        {
            _ammoText.enabled = true;
            _ammoText.text = _ammo.ToString() + " / " + _reloadsAmmo.ToString();
        }
        else
            _ammoText.enabled = false;
            
    }
}
