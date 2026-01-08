using UnityEngine;

public class ConfigurationSettingsView : MonoBehaviour
{
    public enum Views
    {
        General,
        Controls,
        Crosshair,
        Video,
        Audio
    }

    [SerializeField] private Views views;
    [SerializeField] private GameObject GeneralView, ControlsView, CrosshairView, VideoView, AudioView;


    void OnEnable()
    {
        ChangeView(0);
    }
    public void ChangeView(int newView)
    {
        Views viewToChange = (Views)newView;

        GeneralView.SetActive(false);
        ControlsView.SetActive(false);
        CrosshairView.SetActive(false);
        VideoView.SetActive(false);
        AudioView.SetActive(false);

        switch(viewToChange)
        {
            case Views.General:
            GeneralView.SetActive(true);
            break;

            case Views.Controls:
            ControlsView.SetActive(true);
            break;

            case Views.Crosshair:
            CrosshairView.SetActive(true);
            break;

            case Views.Video:
            VideoView.SetActive(true);
            break;

            case Views.Audio:
            AudioView.SetActive(true);
            break;
        }
    }
}
