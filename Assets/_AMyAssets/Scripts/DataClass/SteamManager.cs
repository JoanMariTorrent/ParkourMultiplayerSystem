using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager s_instance;
    private static bool s_EverInitialized;

    public static bool Initialized => s_instance != null && s_EverInitialized;

    [SerializeField] private uint m_uAppID; // Aquí pondrás tu AppID en el Inspector

    private void Awake()
    {
        if (s_instance != null) { Destroy(gameObject); return; }
        s_instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test()) { Debug.LogError("Steamworks: Packsize Test failed!"); return; }

        if (m_uAppID != 0)
        {
            // Crea el archivo steam_appid.txt automáticamente si no existe (solo en el editor)
            if (!System.IO.File.Exists("steam_appid.txt"))
                System.IO.File.WriteAllText("steam_appid.txt", m_uAppID.ToString());
        }

        try
        {
            s_EverInitialized = SteamAPI.Init();
            if (!s_EverInitialized)
            {
                Debug.LogError("Steamworks: SteamAPI.Init() failed! (¿Está Steam abierto?)");
            }
            else
            {
                Debug.Log("<color=green>Steamworks Inicializado con éxito.</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Steamworks Error: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        if (s_instance != this) return;
        s_instance = null;
        if (s_EverInitialized) SteamAPI.Shutdown();
    }

    private void Update()
    {
        if (s_EverInitialized) SteamAPI.RunCallbacks();
    }
}