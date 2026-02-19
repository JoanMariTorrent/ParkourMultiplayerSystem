using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewGameSettings", menuName = "Game/Settings Data")]
public class SettingsData : ScriptableObject
{

    [Header("General Settings")]
    // Sensibilidad
    public float sensitivity = 7f;
    public float aimingSensitivity = 4f;
    public float sniperSensitivity = 3f;
    public Action OnSensitivityChanged;

    // Configuracion de pantalla / video
    public int resolutionIndex;
    public int qualityIndex = 2;
    public bool isFullscreen = true;
    public float gamma = 1f;
    public int targetFPS;

    // Configuracion de audio
    public float masterVolume = 0.8f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float uiVolume = 0.8f;


    [Space][Header("Crosshair Settings")]
    public Color crosshairColor = Color.white;
    public bool useCenterDot = true;
    public float centerDotSize = 1.0f;
    public float centerDotOpacity = 1.0f;

    public bool showInnerLines = true;
    public float innerOpacity = 1f;
    public float innerLenght = 6f;
    public float innerThickness = 2f;
    public float innerOffset = 3f;
}
