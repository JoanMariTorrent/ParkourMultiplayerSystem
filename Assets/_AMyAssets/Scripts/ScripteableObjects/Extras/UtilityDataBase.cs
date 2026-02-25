using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UtilityDatabase", menuName = "Game/UtilityDatabase")]
public class UtilityDatabase : ScriptableObject
{
    public List<UtilityScriptableObject> allUtilities;

    public int GetIdOfUtility(UtilityScriptableObject utility)
    {
        return allUtilities.IndexOf(utility);
    }

    public UtilityScriptableObject GetUtilityByID(int id)
    {
        if (id < 0 || id >= allUtilities.Count) return null;
        return allUtilities[id];
    }

    // --- LÓGICA DE PROBABILIDAD ---

    public UtilityScriptableObject GetRandomUtilityWeighted(List<UtilityScriptableObject> subset = null)
    {
        // 1. Determinar qué lista usar
        List<UtilityScriptableObject> listToUse = (subset != null && subset.Count > 0) ? subset : allUtilities;

        // 2. SEGURIDAD
        if (listToUse == null || listToUse.Count == 0)
        {
            return null;
        }

        float totalChance = 0f;
        foreach (var u in listToUse)
        {
            if (u != null) 
                totalChance += u.dropChance;
        }

        float r = Random.Range(0f, totalChance);
        float accum = 0f;

        foreach (var u in listToUse)
        {
            if (u == null) continue;

            accum += u.dropChance;
            if (r <= accum)
                return u;
        }

        // Fallback seguro
        return listToUse[listToUse.Count - 1];
    }
}