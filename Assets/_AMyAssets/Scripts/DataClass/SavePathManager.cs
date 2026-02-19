using System.IO;
using Steamworks;
using UnityEngine;

public static class SavePathManager
{
    private const string BASE_FOLDER = "Users";

    public static string GetPath(string fileName)
    {
        string userId = "DefaultUser";
        
        
        userId = SteamUser.GetSteamID().m_SteamID.ToString();
        

        string directory = Path.Combine(Application.persistentDataPath, BASE_FOLDER, userId);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, fileName);
    }
}