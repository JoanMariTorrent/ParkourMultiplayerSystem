using UnityEngine;

[CreateAssetMenu(fileName = "New Utility", menuName = "Game/Utility")]
public class UtilityScriptableObject : ScriptableObject
{
    [Header("Info")]
    public string utilityName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Settings")]
    public UtilityType utilityType;
    public GameObject utilityPrefab;

    [Header("Stats")]
    public float cooldown = 10f;
    public int maxCharges = 1;
    public bool isInfinite = false;
    [Range(0f, 1f)]
    public float dropChance;

    [Header("Throw Settings")]
    public float chargeTime = 0.5f; 
    public bool throwOnRelease = true;
}

public enum UtilityType
{
    Throwable,  // Granadas (Humo, Flash, Frag)
    Deployable, // Torretas, Muros, JumpPads
    Device,     // Gancho, Estimulante, Radar
    Passive     // (Opcional) Algo que no se usa pero da bonificaciones
}
