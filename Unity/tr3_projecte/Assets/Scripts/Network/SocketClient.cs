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
        var uri = new Uri("http://localhost:3000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                {"token", "UNITY" }
            }
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Socket.IO Connectat!");
        };

        // Escoltar esdeveniments
        socket.On("matchesUpdated", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchesUpdated?.Invoke(data));
        });

        socket.On("matchCreated", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchCreated?.Invoke(data));
        });

        socket.On("matchStarted", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchStarted?.Invoke(data));
        });

        socket.On("playerJoined", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnPlayerJoined?.Invoke(data));
        });

        socket.On("opponentMoved", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnOpponentMoved?.Invoke(data));
        });

        socket.On("matchEnded", (response) => {
            string data = response.GetValue<string>();
            UnityMainThreadDispatcher.Instance().Enqueue(() => OnMatchEnded?.Invoke(data));
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

    public void CreateMatch(string roomName)
    {
        if (socket != null && socket.Connected && APIClient.Instance.CurrentUser != null)
        {
            var data = new { hostId = APIClient.Instance.CurrentUser.id, roomName = roomName };
            socket.Emit("createMatch", data);
        }
    }

    public void JoinMatch(int matchId)
    {
        if (socket != null && socket.Connected && APIClient.Instance.CurrentUser != null)
        {
            var data = new { matchId = matchId, playerId = APIClient.Instance.CurrentUser.id };
            socket.Emit("joinMatch", data);
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
