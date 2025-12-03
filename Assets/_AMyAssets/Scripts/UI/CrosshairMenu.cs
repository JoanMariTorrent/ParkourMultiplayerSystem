using UnityEngine;
using UnityEngine.UI;

public class CrosshairMenu : MonoBehaviour
{
    [SerializeField] private CrosshairController[] crosshairControllers;

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
        var settings = crosshairControllers[0].settings;

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
    }

    public void OnUseDotChanged(bool newValue)
    {
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.useCenterDot = newValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnDotSizeChanged(float newValue)
    {
        float realValue = Mathf.Lerp(3, 18, newValue);
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.centerDotSize = realValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnDotOpacityChanged(float newValue)
    {
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.centerDotOpacity = newValue;
            crosshair.UpdateCrosshair();
        }
    }

    



    public void OnInnerThicknessChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 60, newValue);
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.innerThickness = realValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnInnerOpacityChanged(float newValue)
    {
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.innerOpacity = newValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnInnerLenghtChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 50, newValue);
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.innerLenght = realValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnInnerOffsetChanged(float newValue)
    {
        float realValue = Mathf.Lerp(1, 65, newValue);
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.innerOffset = realValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void OnShowInner(bool newValue)
    {
        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.showInnerLines = newValue;
            crosshair.UpdateCrosshair();
        }
    }

    public void UpdateRGBColor(float valueIgnored) 
    {
        Color finalColor = new Color(sliderR.value, sliderG.value, sliderB.value, 1f);

        foreach(var crosshair in crosshairControllers)
        {
            crosshair.settings.crosshairColor = finalColor;
            crosshair.UpdateCrosshair();
        }
    }
}
