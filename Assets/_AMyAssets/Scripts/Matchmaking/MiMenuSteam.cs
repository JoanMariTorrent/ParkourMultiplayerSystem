using UnityEngine;
using PurrLobby; // Necesario para el LobbyManager y Lobby
using PurrLobby.Providers; 
using System.Threading.Tasks;
using PurrNet; // ¡Descomentado! Para arrancar el servidor/cliente
using UnityEngine.SceneManagement; // Necesario para cambiar de mapa
using System.Collections.Generic; // Necesario para las listas de jugadores

public class MiMenuSteam : MonoBehaviour
{
    [Header("Gestores de Red")]
    public SteamLobbyProvider gestorLobbies;
    public LobbyManager miLobbyManager; // Añadimos el manager oficial para escuchar eventos
    
    [Header("Configuración de Partida")]
    [PurrScene, SerializeField] private string nextScene; // Pon el nombre exacto de tu mapa
    public bool modoPersonalizado = false; 

    // --- ESCUCHAMOS LA BENGALA (Para los Clientes) ---
    void OnEnable()
    {
        if (miLobbyManager != null)
            miLobbyManager.OnRoomUpdated.AddListener(AlActualizarseLaSala);
    }

    void OnDisable()
    {
        if (miLobbyManager != null)
            miLobbyManager.OnRoomUpdated.RemoveListener(AlActualizarseLaSala);
    }

    async void Start()
    {
        Debug.Log("Iniciando Steam y creando Auto-Lobby (Party)...");
        
        await gestorLobbies.InitializeAsync(); 
        await Task.Delay(1000); 

        gestorLobbies.lobbyType = SteamLobbyProvider.LobbyType.FriendsOnly;
        var sala = await gestorLobbies.CreateLobbyAsync(4); 

        if (sala.IsValid)
            Debug.Log("Party creada. ¡Tus amigos ya pueden unirse por Steam!");
        else
            Debug.LogWarning("Error al crear la Party. ¿Está Steam abierto?");
    }

    public void BotonJugar()
    {
        if (modoPersonalizado)
            EmpezarPartidaPersonalizada();
        else
            BuscarMatchmaking();
    }

    // --- EL LÍDER ARRANCA LA PARTIDA ---
    private void EmpezarPartidaPersonalizada()
    {
        Debug.Log("Líder: Arrancando Personalizada...");
        
        GuardarDatosDeLaPartida();

        // Avisamos a todos
        miLobbyManager.SetLobbyStarted(); 

        // Viajamos al mapa
        SceneManager.LoadScene(nextScene); 
    }

    private async void BuscarMatchmaking()
    {
        Debug.Log("Buscando rivales en Matchmaking Público...");
        
        var partidasEncontradas = await gestorLobbies.SearchLobbiesAsync();

        if (partidasEncontradas.Count > 0)
        {
            Debug.Log("¡Partida encontrada! Uniéndose a la sala pública...");
            await gestorLobbies.JoinLobbyAsync(partidasEncontradas[0].LobbyId);
            
            // Si nos unimos a una sala pública, esperamos a que el líder de esa sala 
            // le dé a "Empezar". El código de abajo (AlActualizarseLaSala) hará el resto.
        }
        else
        {
            Debug.Log("No hay partidas. Creando un Lobby Público y esperando rivales...");
            
            await miLobbyManager.CurrentProvider.LeaveLobbyAsync(); 
            
            gestorLobbies.lobbyType = SteamLobbyProvider.LobbyType.Public;
            await gestorLobbies.CreateLobbyAsync(4);
            
            // Aquí el líder se quedaría esperando a que entre gente antes de llamar a EmpezarPartidaPersonalizada()
        }
    }

    // --- LOS CLIENTES SIGUEN AL LÍDER ---
    private void AlActualizarseLaSala(Lobby sala)
    {
        // Si la sala dice "Started = True" y YO NO SOY EL LÍDER...
        if (sala.Properties != null && 
            sala.Properties.TryGetValue("Started", out string startedStr) && 
            startedStr == "True" && 
            !sala.IsOwner) 
        {
            Debug.Log("Cliente: ¡El líder ha iniciado! Entrando...");
            
            GuardarDatosDeLaPartida();

            // Viajo al mapa detrás del líder
            SceneManager.LoadScene(nextScene);
        }
    }

    // --- FUNCIÓN COMPARTIDA PARA GUARDAR NOMBRES ---
    private void GuardarDatosDeLaPartida()
    {
        if (miLobbyManager != null && miLobbyManager.CurrentLobby.IsValid)
        {
            MatchData.Players = new List<LobbyUser>(miLobbyManager.CurrentLobby.Members);
            MatchData.PlayerCount = miLobbyManager.CurrentLobby.Members.Count;
        }
    }
}