using PurrNet;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;



public enum Views
{ 
    Panel,
    Settings,
    Exit
}

public class MainSettingsView : View
{
    [SerializeField] private GameObject PanelView;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject ExitMenu;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Player player;

    [SerializeField] private SettingsData settings;

    public Views views;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);

    }

    private void Start()
    {
        PanelView.SetActive(true);
        settingsMenu.SetActive(false);
        ExitMenu.SetActive(false);

        if(canvas == null) canvas = InstanceHandler.GetInstance<Canvas>();
        if(player == null && canvas != null) player = canvas.GetComponentInParent<Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canvas.ShowView<MainSettingsView>(true);
            canvas.HideView<GameMainView>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //if (settings != null) settings.OnSettingsEnabled?.Invoke();
            player.SettingsEnabled();
        }
    }


    public void ChangeView(int viewIndex)
    {
        Views viewToChange = (Views)viewIndex;

        PanelView.SetActive(false);
        settingsMenu.SetActive(false);
        ExitMenu.SetActive(false);

        switch (viewToChange)
        {
            case Views.Panel:
                PanelView.SetActive(true);
                break;
            case Views.Settings:
                settingsMenu.SetActive(true);
                break;
            case Views.Exit:
                ExitMenu.SetActive(true);
                break;

        }
    }

    public void ExitGame()
    { 
        SceneManager.LoadScene(0);
    }

    public void ReturnGame()
    {
        canvas.HideView<MainSettingsView>();
        canvas.ShowView<GameMainView>(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //settings.OnSettingsDisabled?.Invoke();
        player.SettingsDisabled();
    }

    public override void OnShow()
    {
        ChangeView((int)Views.Panel);
    }

    public override void OnHide()
    {
    }

    public void PlaySound(AudioClip audio)
    {
        PlaySound(audio);
    }
}
