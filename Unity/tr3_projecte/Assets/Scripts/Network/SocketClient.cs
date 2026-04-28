using System;
using System.Collections.Generic;
using UnityEngine;
// Necessites el paquet SocketIOUnity (https://github.com/itisnajim/SocketIOUnity.git)
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;

public class SocketClient : MonoBehaviour
{
    public static SocketClient Instance { get; private set; }
    
    public SocketIOUnity socket;
    
    // Esdeveniments per la interfície i joc
    public Action<string> OnMatchesUpdated;
    public Action<string> OnMatchCreated;
    public Action<string> OnMatchStarted;
    public Action<string> OnPlayerJoined;
    public Action<string> OnOpponentMoved;
    public Action<string> OnMatchEnded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Connect()
    {
        var uri = new Uri("http://204.168.213.113:3000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                {"token", APIClient.Instance.Token }
            }
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("<color=green>Socket.IO Connectat amb èxit!</color>");
            JoinLobby(); // Ens unim al lobby automàticament en connectar
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.LogWarning("<color=orange>Socket.IO Desconnectat!</color>");
        };

        socket.On("matchesUpdated", (response) => {
            try {
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchesUpdated?.Invoke(data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error matchesUpdated: " + ex)); }
        });

        socket.On("matchCreated", (response) => {
            try {
                Debug.Log("<color=cyan>Resposta matchCreated rebuda del servidor!</color>");
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    Debug.Log("Invocant OnMatchCreated des del fil principal...");
                    OnMatchCreated?.Invoke(data);
                });
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error matchCreated: " + ex)); }
        });

        socket.On("matchStarted", (response) => {
            try {
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchStarted?.Invoke(data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error matchStarted: " + ex)); }
        });

        socket.On("playerJoined", (response) => {
            try {
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => OnPlayerJoined?.Invoke(data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error playerJoined: " + ex)); }
        });

        socket.On("opponentMoved", (response) => {
            try {
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => OnOpponentMoved?.Invoke(data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error opponentMoved: " + ex)); }
        });

        socket.On("matchEnded", (response) => {
            try {
                string data = response.GetValue<string>();
                Debug.Log("<color=orange>EVENT matchEnded rebut:</color> " + data);
                UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchEnded?.Invoke(data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error matchEnded: " + ex)); }
        });

        socket.On("error", (response) => {
            try {
                string data = response.GetValue<string>();
                UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error del servidor: " + data));
            } catch (Exception ex) { UnityMainThreadDispatcher.Instance().Enqueue(() => Debug.LogError("Error en error callback: " + ex)); }
        });

        socket.Connect();
    }

    public void JoinLobby()
    {
        if (socket != null && socket.Connected)
        {
            socket.Emit("joinLobby");
        }
    }

    public void CreateMatch(string hostId, string roomName, Color color)
    {
        if (socket != null && socket.Connected)
        {
            Debug.Log("Enviant createMatch al servidor...");
            string colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
            socket.Emit("createMatch", new { hostId, roomName, color = colorHex });
        }
        else
        {
            Debug.LogError("No es pot crear la partida: El Socket NO està connectat!");
        }
    }

    public void JoinMatch(int matchId, string playerId, Color color)
    {
        if (socket != null && socket.Connected)
        {
            Debug.Log("Enviant joinMatch al servidor per la sala: " + matchId);
            string colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
            socket.Emit("joinMatch", new { matchId, playerId, color = colorHex });
        }
        else
        {
            Debug.LogError("No es pot unir a la partida: El Socket NO està connectat!");
        }
    }

    public void SendMove(int matchId, Vector2 position, Vector2 direction)
    {
        if (socket != null && socket.Connected && APIClient.Instance.CurrentUser != null)
        {
            var data = new { 
                matchId = matchId, 
                playerId = APIClient.Instance.CurrentUser.id,
                position = new { x = position.x, y = position.y },
                direction = new { x = direction.x, y = direction.y }
            };
            socket.Emit("playerMove", data);
        }
    }

    public void SendCollision(int matchId)
    {
        if (socket != null && socket.Connected && APIClient.Instance.CurrentUser != null)
        {
            Debug.Log("<color=red>ENVIANT COLLISIO AL SERVIDOR!</color> ID Partida: " + matchId + " | Loser: " + APIClient.Instance.CurrentUser.id);
            var data = new { matchId = matchId, loserId = APIClient.Instance.CurrentUser.id };
            socket.Emit("playerCollision", data);
        }
    }

    private void OnDestroy()
    {
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
        }
    }
}
