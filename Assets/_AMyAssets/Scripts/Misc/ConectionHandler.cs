using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameWatchdog : MonoBehaviour
{
    [SerializeField] private int menuSceneIndex = 0;
    
    private bool _hasConnectedOnce = false;

    private void Start()
    {
        if (InstanceHandler.NetworkManager != null)
        {
            if (InstanceHandler.NetworkManager.isClient || InstanceHandler.NetworkManager.isServer)
            {
                _hasConnectedOnce = true;
            }
        }
    }

    private void Update()
    {
        
        bool isConnected = false;
        
        if (InstanceHandler.NetworkManager != null)
        {
            if (InstanceHandler.NetworkManager.isClient || InstanceHandler.NetworkManager.isServer)
            {
                isConnected = true;
            }
        }

        if (_hasConnectedOnce && !isConnected)
        {
            Debug.Log("Detectada desconexión. Volviendo al menú...");
            BackToMenu();
        }
        
        if (isConnected) _hasConnectedOnce = true;
    }


    public void ForceExitGame()
    {

        
        if (InstanceHandler.NetworkManager != null)
        {
            Destroy(InstanceHandler.NetworkManager.gameObject);
        }
        else
        {
            BackToMenu();
        }
    }

    private void BackToMenu()
    {
        _hasConnectedOnce = false; 
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        SceneManager.LoadScene(menuSceneIndex);
    }
}