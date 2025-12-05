using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewGameSettings", menuName = "Game/Settings Data")]
public class SettingsData : ScriptableObject
{

    [Header("General Settings")]
    public float sensitivity = 0.1f;
    public Action OnSensitivityChanged;

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
