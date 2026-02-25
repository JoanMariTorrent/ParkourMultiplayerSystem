using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponScripteableObject> weapons;

    public int GetIdOfWeapon(WeaponScripteableObject weapon)
    {
        return weapons.IndexOf(weapon);
    }

    public WeaponScripteableObject GetWeaponByID(int id)
    {
        return weapons[id];
    }


    public WeaponScripteableObject GetRandomWeaponWeighted(List<WeaponScripteableObject> subset = null)
    {
        // 1. Determinar qué lista usar
        List<WeaponScripteableObject> listToUse = (subset != null && subset.Count > 0) ? subset : weapons;

        // 2. SEGURIDAD: Si la lista está vacía o es nula, salimos para evitar el crash
        if (listToUse == null || listToUse.Count == 0)
        {

            return null;
        }

        float totalChance = 0f;
        foreach (var w in listToUse)
        {
            // Seguridad extra por si hay huecos vacíos en la lista del inspector
            if (w != null) 
                totalChance += w.dropChance;
        }

        float r = Random.Range(0f, totalChance);
        float accum = 0f;

        foreach (var w in listToUse)
        {
            if (w == null) continue;

            accum += w.dropChance;
            if (r <= accum)
                return w;
        }

        // Fallback seguro (ahora sabemos que Count > 0)
        return listToUse[listToUse.Count - 1];
    }
}
