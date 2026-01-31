using UnityEngine;
using TMPro;
using PurrNet;
using UnityEngine.UI;
using System.Collections;

public class GameMainView : View
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text _healthText, bckHealthText;
    [SerializeField] private TMP_Text _ammoText, bckAmmoText;
    [SerializeField] private TMP_Text _timerText, _bckgTimerText;

    [Header("Barras de vida")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image ghostBar;

    [Header("Configuracion animaciones")]
    [SerializeField] private float mainBarSpeed = 10f;
    [SerializeField] private float ghostSpeed = 5f; 
    [SerializeField] private float ghostDelay = 0.3f;
    private Coroutine healthCoroutine, ghostCoroutine;


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


    public void UpdateHealth(int _health, int _maxHealth)
    {
        if (_health < 0)
        {
            _health = 0;
        }
        _healthText.text = _health.ToString();
        bckHealthText.text = _health.ToString();

        float targetFill = (float)_health/_maxHealth;
        targetFill = Mathf.Clamp01(targetFill);


        // Health bar
        if(healthBar)
        {
            if(healthCoroutine != null) StopCoroutine(healthCoroutine);
            healthCoroutine = StartCoroutine(AnimateHealthBar(targetFill));
        }

        // Ghost bar

        if(ghostBar)
        {
            if(ghostCoroutine != null) StopCoroutine(ghostCoroutine);
            ghostCoroutine = StartCoroutine(AnimationGhostBar(targetFill));
        }
    }

    private IEnumerator AnimateHealthBar(float targetVal)
    {
        while(Mathf.Abs(healthBar.fillAmount - targetVal) > 0.001f)
        {
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetVal, Time.deltaTime * mainBarSpeed);
            yield return null;
        } 
        healthBar.fillAmount = targetVal;
    }

    private IEnumerator AnimationGhostBar(float targetVal)
    {
        // Suma vida
        if(ghostBar.fillAmount < targetVal)
        {
            ghostBar.fillAmount = targetVal;
            yield break;
        }
        
        yield return new WaitForSeconds(ghostDelay);

        // Baja vida
        while (Mathf.Abs(ghostBar.fillAmount - targetVal) > 0.001f)
        {
            ghostBar.fillAmount = Mathf.Lerp(ghostBar.fillAmount, targetVal, Time.deltaTime * ghostSpeed);
            yield return null;
        }
        ghostBar.fillAmount = targetVal;

    }

    public void UpdateAmmo(int _ammo, int _reloadsAmmo)
    {
        if (_ammo >= 0)
        {
            _ammoText.enabled = true;
            bckAmmoText.enabled = true;
            _ammoText.text = _ammo.ToString() + " / " + _reloadsAmmo.ToString();
            bckAmmoText.text = _ammo.ToString() + " / " + _reloadsAmmo.ToString();
        }
        else
        {
            _ammoText.enabled = false;
            bckAmmoText.enabled = false;
        }   
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
