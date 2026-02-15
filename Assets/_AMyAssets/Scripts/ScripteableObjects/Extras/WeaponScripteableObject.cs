using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponScripteableObject : ScriptableObject
{
    public string weaponName;
    public Sprite icon;
    [Range(0f, 1f)] public float dropChance;
    public WeaponScripteableType weaponType;
    public GameObject gunPrefab;
    public AnimatorOverrideController animatorOverride;

}




public enum WeaponScripteableType
{
    Primary,
    Secondary,
    Utility,
    Knife
}
