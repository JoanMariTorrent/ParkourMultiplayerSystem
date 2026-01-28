using UnityEngine;
using TMPro;
using PurrNet;

public class GameMainView : View
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _timerText, _bckgTimerText;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        //InstanceHandler.UnregisterInstance<GameMainView>();
    }


    public override void OnHide() { }

    public override void OnShow() { }


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

    void Update()
    {
        if(!InstanceHandler.TryGetInstance(out RoundRunningStateDM roundRunningStateDM)) return;
        if(_timerText == null || _bckgTimerText == null) return;

        float timeInSeconds = roundRunningStateDM.timerReference;
        if(timeInSeconds < 0) timeInSeconds = 0;


        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);

        string finalStringFormat = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        _timerText.text = finalStringFormat;
        _bckgTimerText.text = finalStringFormat;
    }
}
