using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine.ProBuilder.MeshOperations;

public class SettingsSystem : MonoBehaviour
{
    [SerializeField] private CrosshairController[] crosshairControllers;
    public SettingsData settings;
    public AudioMixer mainMixer;

    [Header("General")]
    [Space]
    public Slider sensitivitySlider;
    public Slider AimingsensitivitySlider;
    public Slider SnipersensitivitySlider;
    
    [Space] // Video
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Slider gammaSlider;
    public TMP_InputField fpsInputField;
    [Space] public Volume globalVolume;

    
    [Space] // Audio
    public Slider masterVolSlider;
    public Slider musicVolSlider;
    public Slider sfxVolSlider;
    public Slider uiVolSlider;

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


    private Resolution[] filteredResolutions;
    private ColorAdjustments colorAdjustments;

    void Awake()
    {
        SetupVideoSettings();
    }

    void Start()
    {
        LoadValuesFromSettings();

        RegisterEvents();
    }


    public void Intialize(Volume _globalVolume)
    {
        globalVolume = _globalVolume;
    }

    private void RegisterEvents()
    {
        // Video
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(SetQuality);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (gammaSlider) gammaSlider.onValueChanged.AddListener(SetGamma);
        if (fpsInputField) fpsInputField.onEndEdit.AddListener(SetMaxFPS);

        // Audio
        if (masterVolSlider) masterVolSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolSlider) musicVolSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolSlider) sfxVolSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiVolSlider) uiVolSlider.onValueChanged.AddListener(SetUIVolume);

        // General
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(OnNormalSensibilityChange);
        if (AimingsensitivitySlider) AimingsensitivitySlider.onValueChanged.AddListener(OnAimingSensibilityChange);
        if (SnipersensitivitySlider) SnipersensitivitySlider.onValueChanged.AddListener(OnSniperSensibilityChange);

        // Crosshair
        if (sliderR) sliderR.onValueChanged.AddListener(UpdateRGBColor);
        if (sliderG) sliderG.onValueChanged.AddListener(UpdateRGBColor);
        if (sliderB) sliderB.onValueChanged.AddListener(UpdateRGBColor);

        if (dotToggle) dotToggle.onValueChanged.AddListener(OnUseDotChanged);
        if (dotSizeSlider) dotSizeSlider.onValueChanged.AddListener(OnDotSizeChanged);
        if (dotOpacitySlider) dotOpacitySlider.onValueChanged.AddListener(OnDotOpacityChanged);

        if (showInnerToggle) showInnerToggle.onValueChanged.AddListener(OnShowInner);
        if (innerThicknessSlider) innerThicknessSlider.onValueChanged.AddListener(OnInnerThicknessChanged);
        if (innerOpacitySlider) innerOpacitySlider.onValueChanged.AddListener(OnInnerOpacityChanged);
        if (innerLenghtSlider) innerLenghtSlider.onValueChanged.AddListener(OnInnerLenghtChanged);
        if (innerOffsetSlider) innerOffsetSlider.onValueChanged.AddListener(OnInnerOffsetChanged);
    }

    private void SetupVideoSettings()
    {
        // --- 1. FULLSCREEN ---
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }

        // --- 2. CALIDAD (QUALITY) ---
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            
            List<string> qualityNames = new List<string>(QualitySettings.names);
            
            qualityDropdown.AddOptions(qualityNames);
            
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }

        // --- 3. RESOLUCIONES ---
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();

            Resolution[] allResolutions = Screen.resolutions;
            
            List<Resolution> uniqueResolutions = new List<Resolution>();

            for (int i = 0; i < allResolutions.Length; i++)
            {
                bool exists = uniqueResolutions.Any(x => x.width == allResolutions[i].width && x.height == allResolutions[i].height);
                if (!exists)
                {
                    uniqueResolutions.Add(allResolutions[i]);
                }
            }

            filteredResolutions = uniqueResolutions.OrderByDescending(x => x.width).ToArray();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < filteredResolutions.Length; i++)
            {
                string option = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
                options.Add(option);

                if (filteredResolutions[i].width == Screen.width &&
                    filteredResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }
            

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // --- 4. FPS ---
        if (fpsInputField != null)
        {
            int currentTarget = Application.targetFrameRate;

            if (currentTarget != -1)
            {
                fpsInputField.text = currentTarget.ToString();
            }
            else
            {
                int monitorHz = (int)Screen.currentResolution.refreshRateRatio.value;
                
                fpsInputField.text = monitorHz.ToString();
                
                Application.targetFrameRate = monitorHz;
            }
        }
    }

    private void LoadValuesFromSettings()
    {
        // Video
        if(gammaSlider) gammaSlider.value = settings.gamma;

        // Audio (Aplicamos valor al Slider Y al Mixer directamente)
        if(masterVolSlider) { masterVolSlider.value = settings.masterVolume; SetMixerVolume("MasterVolume", settings.masterVolume); }
        if(musicVolSlider) { musicVolSlider.value = settings.musicVolume; SetMixerVolume("MusicVolume", settings.musicVolume); }
        if(sfxVolSlider) { sfxVolSlider.value = settings.sfxVolume; SetMixerVolume("SFXVolume", settings.sfxVolume); }
        if(uiVolSlider) { uiVolSlider.value = settings.uiVolume; SetMixerVolume("UIVolume", settings.uiVolume); }
        
        // General (Sensibilidad con InverseLerp)
        if (sensitivitySlider) sensitivitySlider.value = Mathf.InverseLerp(0.05f, 10f, settings.sensitivity);
        if (AimingsensitivitySlider) AimingsensitivitySlider.value = Mathf.InverseLerp(0.05f, 10f, settings.aimingSensitivity);
        if (SnipersensitivitySlider) SnipersensitivitySlider.value = Mathf.InverseLerp(0.05f, 10f, settings.sniperSensitivity);

        // Crosshair
        if(crosshairControllers.Length > 0)
        {
            sliderR.value = settings.crosshairColor.r;
            sliderG.value = settings.crosshairColor.g;
            sliderB.value = settings.crosshairColor.b;
            if(dotToggle) dotToggle.isOn = settings.useCenterDot;
            if(dotSizeSlider) dotSizeSlider.value = Mathf.InverseLerp(3, 18, settings.centerDotSize);
            if(dotOpacitySlider) dotOpacitySlider.value = settings.centerDotOpacity; 
            if(showInnerToggle) showInnerToggle.isOn = settings.showInnerLines;
            if(innerThicknessSlider) innerThicknessSlider.value = Mathf.InverseLerp(1, 60, settings.innerThickness);
            if(innerLenghtSlider) innerLenghtSlider.value = Mathf.InverseLerp(1, 50, settings.innerLenght);
            if(innerOffsetSlider) innerOffsetSlider.value = Mathf.InverseLerp(1, 65, settings.innerOffset);
            if(innerOpacitySlider) innerOpacitySlider.value = settings.innerOpacity;
        }
    }


    #region Video settings

    public void SetResolution(int resolutionIndex)
    {
        if (filteredResolutions == null || resolutionIndex >= filteredResolutions.Length) return;
        
        Resolution res = filteredResolutions[resolutionIndex];
        
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        
        settings.resolutionIndex = resolutionIndex;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        
        settings.qualityIndex = qualityIndex;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        settings.isFullscreen = isFullscreen;
    }

    public void SetGamma(float val)
    {
        settings.gamma = val;
        if (globalVolume == null) return;

        if (globalVolume.profile.TryGet(out ColorAdjustments adj))
        {
            float exposureValue = Mathf.Lerp(-2f, 2f, val);
            adj.postExposure.value = exposureValue;
        }
    }

    public void SetMaxFPS(string inputVal)
    {
        if (int.TryParse(inputVal, out int targetFPS))
        {
            if (targetFPS <= 0)
            {
                targetFPS = -1;
                fpsInputField.text = "0";
            }
            else if (targetFPS < 30)
            {
                targetFPS = 30;
                fpsInputField.text = "30";
            }
            else if (targetFPS > 999)
            {
                targetFPS = 999;
                fpsInputField.text = "999";
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
    }

    #endregion


    #region Audio Logic
    private void SetMixerVolume(string paramName, float sliderVal)
    {
        if (mainMixer == null) return;
        float dB = Mathf.Log10(Mathf.Max(sliderVal, 0.0001f)) * 20;
        mainMixer.SetFloat(paramName, dB);
    }

    public void SetMasterVolume(float val) { settings.masterVolume = val; SetMixerVolume("MasterVolume", val); }
    public void SetMusicVolume(float val) { settings.musicVolume = val; SetMixerVolume("MusicVolume", val); }
    public void SetSFXVolume(float val) { settings.sfxVolume = val; SetMixerVolume("SFXVolume", val); }
    public void SetUIVolume(float val) { settings.uiVolume = val; SetMixerVolume("UIVolume", val); }
    
    #endregion



    #region Crosshair settings

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

    public void OnNormalSensibilityChange(float newValue)
    {
        float realValue = Mathf.Lerp(0.05f, 10f, newValue);

        settings.sensitivity = realValue;
        settings.OnSensitivityChanged?.Invoke();
    }

    public void OnAimingSensibilityChange(float newValue)
    {
        float realValue = Mathf.Lerp(0.05f, 10f, newValue);

        settings.aimingSensitivity = realValue;
        settings.OnSensitivityChanged?.Invoke();
    }

    public void OnSniperSensibilityChange(float newValue)
    {
        float realValue = Mathf.Lerp(0.05f, 10f, newValue);

        settings.sniperSensitivity = realValue;
        settings.OnSensitivityChanged?.Invoke();
    }
    #endregion
    
}
