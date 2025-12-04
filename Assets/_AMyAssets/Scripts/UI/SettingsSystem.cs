using UnityEngine;
using UnityEngine.UI;


public class SettingsSystem : MonoBehaviour
{
    [SerializeField] private CrosshairController[] crosshairControllers;
    public SettingsData settings;

    [Header("General")]
    public Slider sensitivitySlider;

    [Space(5)][Header("Crosshair")]

    [Header("Referencias UI - Color")]
    public Slider sliderR;
    public Slider sliderG;
    public Slider sliderB;

    [Header("Referencias UI - Center Dot")]
    public Toggle dotToggle; 
    public Slider dotSizeSlider;  
    public Slider dotOpacitySlider; 

    [Header("Referencias UI - Inner Lines")]
    public Toggle showInnerToggle; 
    public Slider innerThicknessSlider;
    public Slider innerOpacitySlider;
    public Slider innerLenghtSlider;
    public Slider innerOffsetSlider;

    private void Start()
    {
        LoadValuesFromSettings();
        
        // Escuchamos cambios en cualquiera de los 3
        sliderR.onValueChanged.AddListener(UpdateRGBColor);
        sliderG.onValueChanged.AddListener(UpdateRGBColor);
        sliderB.onValueChanged.AddListener(UpdateRGBColor);
    }

    private void LoadValuesFromSettings()
    {
        if(crosshairControllers.Length == 0) return;

        sliderR.value = settings.crosshairColor.r;
        sliderG.value = settings.crosshairColor.g;
        sliderB.value = settings.crosshairColor.b;

        if(dotToggle != null) dotToggle.isOn = settings.useCenterDot;
        
        if(dotSizeSlider != null) 
            dotSizeSlider.value = Mathf.InverseLerp(3, 18, settings.centerDotSize);
            
        if(dotOpacitySlider != null) 
            dotOpacitySlider.value = settings.centerDotOpacity; 

        if(showInnerToggle != null) showInnerToggle.isOn = settings.showInnerLines;

        if(innerThicknessSlider != null)
            innerThicknessSlider.value = Mathf.InverseLerp(1, 60, settings.innerThickness);

        if(innerLenghtSlider != null)
            innerLenghtSlider.value = Mathf.InverseLerp(1, 50, settings.innerLenght);

        if(innerOffsetSlider != null)
            innerOffsetSlider.value = Mathf.InverseLerp(1, 65, settings.innerOffset);

        if(innerOpacitySlider != null)
            innerOpacitySlider.value = settings.innerOpacity;

        if(sensitivitySlider != null)
            sensitivitySlider.value = settings.sensitivity;
    }

    public void OnUseDotChanged(bool newValue)
    {
        settings.useCenterDot = newValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnDotSizeChanged(float newValue)
    {
        float realValue = Mathf.Lerp(3, 18, newValue);
        settings.centerDotSize = realValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnDotOpacityChanged(float newValue)
    {
        settings.centerDotOpacity = newValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    



    public void OnInnerThicknessChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 60, newValue);
        settings.innerThickness = realValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnInnerOpacityChanged(float newValue)
    {
        settings.innerOpacity = newValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnInnerLenghtChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 50, newValue);
        settings.innerLenght = realValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnInnerOffsetChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 65, newValue);
        settings.innerOffset = realValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnShowInner(bool newValue)
    {
        settings.showInnerLines = newValue;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void UpdateRGBColor(float valueIgnored) 
    {
        Color finalColor = new Color(sliderR.value, sliderG.value, sliderB.value, 1f);

        settings.crosshairColor = finalColor;
        foreach (var c in crosshairControllers) c.UpdateCrosshair();
    }

    public void OnSensibilityChange(float newValue)
    {
        float realValue = Mathf.Lerp(0.05f, 2.5f, newValue);

        settings.sensitivity = realValue;
        settings.OnSensitivityChanged?.Invoke();
    }
}
