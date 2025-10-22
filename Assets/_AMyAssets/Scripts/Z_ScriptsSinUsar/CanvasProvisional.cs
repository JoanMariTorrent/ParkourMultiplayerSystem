using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasProvisional : MonoBehaviour
{
    [SerializeField] private GameObject createLobby;
    [SerializeField] private GameObject defaultMenu;
    [SerializeField] private GameObject browseMenu;

    private void Start()
    {
        defaultMenu.SetActive(true);
        browseMenu.SetActive(false);
        createLobby.SetActive(false);
    }

    public void GoCreateLobby()
    {
        defaultMenu.SetActive(false);
        browseMenu.SetActive(false);
        createLobby.SetActive(true);
    }

    public void GoBrowseMenu()
    {
        defaultMenu.SetActive(false);
        createLobby.SetActive(false);
        browseMenu.SetActive(true);
    }

    public void GoDefaultMenu()
    {
        defaultMenu.SetActive(true);
        createLobby.SetActive(false);
        browseMenu.SetActive(false);
    }

    public void GoLevelShoot()
    {
        SceneManager.LoadScene(1);
    }

    public void GoLevelMovement()
    {
        SceneManager.LoadScene(2);
    }
}
