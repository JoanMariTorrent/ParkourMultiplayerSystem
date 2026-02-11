using UnityEngine;
using TMPro;
using PurrNet;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;

public class GameMainView : View
{
    [Header("Hit Marker")]
    [SerializeField] private Image hitMarkerImage;
    [SerializeField] private float startFadeCooldow;
    [SerializeField] private float hitMarkerFadeSpeed;

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text _healthText, bckHealthText;
    [SerializeField] private TMP_Text _ammoText, bckAmmoText;
    [SerializeField] private TMP_Text _timerText, _bckgTimerText;

    [Header("Enemy deaths")]
    [SerializeField] private GameObject PF_EnemyDown;
    [SerializeField] private Transform enemyDowTranform;  
    [SerializeField] private float timeToDespawn;

    [Header("Barras de vida")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image ghostBar;

    [Header("Configuracion animaciones")]
    [SerializeField] private float mainBarSpeed = 10f;
    [SerializeField] private float ghostSpeed = 5f; 
    [SerializeField] private float ghostDelay = 0.3f;
    private Coroutine healthCoroutine, ghostCoroutine, hitMarkerCoroutine;
    
    [Header("Kill animation")]
    [SerializeField] private GameObject killAnimationContainer;
    private Coroutine KillAnimation;


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

    public void SpawnEnemyDeathUI(string victimName, string killerName)
    {
        if (PF_EnemyDown == null || enemyDowTranform == null) return;

        GameObject newItem = Instantiate(PF_EnemyDown, enemyDowTranform);

        EnemyDown itemScript = newItem.GetComponentInChildren<EnemyDown>();
        if (itemScript != null)
        {
            itemScript.Intiialize(killerName, victimName);
        }
        Destroy(newItem, timeToDespawn);

        if (enemyDowTranform.childCount > 5)
        {
            Destroy(enemyDowTranform.GetChild(0).gameObject);
        }
    }

    public void HitMarker(bool lastHit)
    {
        if(hitMarkerImage == null) return;
        if(hitMarkerCoroutine != null) StopCoroutine(hitMarkerCoroutine);  
        hitMarkerCoroutine = StartCoroutine(HitMarkerCoroutine(lastHit));
    }

    private IEnumerator HitMarkerCoroutine(bool lastHit)
    {
        Color c = lastHit ? Color.red : Color.white;
        c.a = 1f;
        hitMarkerImage.color = c;


        if(startFadeCooldow > 0) yield return new WaitForSeconds(startFadeCooldow);

        float x = lastHit ? hitMarkerFadeSpeed / 2 : hitMarkerFadeSpeed;


        while (c.a > 0.01f)
        {
            c.a = Mathf.Lerp(c.a, 0, Time.deltaTime * x);
            hitMarkerImage.color = c;
            yield return null;
        }

        c.a = 0f;
        hitMarkerImage.color = c;
        
    }


    public void RequestKillAnimation()
    {
        if(KillAnimation != null) StopCoroutine(KillAnimation);
        
        KillAnimation = StartCoroutine(PlayKillAnimation());
    }

    private IEnumerator PlayKillAnimation()
    {
        if (killAnimationContainer == null)
            yield break;
        
        if(killAnimationContainer.activeSelf) killAnimationContainer.SetActive(false);
        killAnimationContainer.SetActive(true);

        yield return new WaitForSeconds(2);

        killAnimationContainer.SetActive(false);
    }
}
