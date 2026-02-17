using UnityEngine;
using PurrLobby; 
using Unity.Cinemachine;
using UnityEngine.Playables;
using System.Collections;

public class LobbyCameraBridge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private CinemachineCamera lobbyCam;
    [SerializeField] private CinemachineCamera CinematicCam;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayableDirector menuTimeline;

    void Start()
    {
        menuTimeline.Play();
    }

    private void OnEnable()
    {
        if (lobbyManager != null)
        {
            // Nos suscribimos a ambos eventos
            lobbyManager.OnRoomJoined.AddListener(OnJoined);
            lobbyManager.OnRoomLeft.AddListener(OnLeft);
        }
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomJoined.RemoveListener(OnJoined);
            lobbyManager.OnRoomLeft.AddListener(OnLeft);
        }
    }

    // Se ejecuta al entrar en una lobby
    private void OnJoined(Lobby lobby)
    {
        if (menuTimeline != null)
            menuTimeline.Stop(); 

        if (lobbyCam != null)
            lobbyCam.Priority = 20; 
            
        Debug.Log("<color=cyan>Cámara: Entrando en Lobby.</color>");
    }

    // NUEVO: Se ejecuta al salir de la lobby
    private void OnLeft()
    {
        // 1. BAJAR PRIORIDAD: Al bajarla, Cinemachine buscará la siguiente cámara 
        // con mayor prioridad (las de tu menú que están en el Timeline).
        if (lobbyCam != null)
            lobbyCam.Priority = 5; 

        // 2. REINICIAR EL TIMELINE: Volvemos a arrancar la cinemática del menú.
        StartCoroutine(returnCinematic());

        Debug.Log("<color=orange>Cámara: Volviendo al Menú Principal.</color>");
    }

    private IEnumerator returnCinematic()
    {
        var dolly = CinematicCam.GetComponent<CinemachineSplineDolly>();
        dolly.CameraPosition = 0;

        float tolerace = 1f;
        while(Vector3.Distance(mainCamera.transform.position, CinematicCam.transform.position) > tolerace) yield return null;
        if (menuTimeline != null)
        {
            menuTimeline.Play();
        }
    }
}