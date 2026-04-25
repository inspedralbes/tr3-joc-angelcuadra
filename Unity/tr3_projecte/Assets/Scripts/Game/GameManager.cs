using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class MatchData
{
    public int id;
    public int hostId;
    public string roomName;
    public List<int> players;
    public string status;
    public int? winnerId;
}

[Serializable]
public class MoveData
{
    public int playerId;
    public Vector2Data position;
    public Vector2Data direction;
}

[Serializable]
public class Vector2Data
{
    public float x;
    public float y;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject motoPrefab; // Prefab de la moto controlada pel jugador
    public GameObject aiPrefab;   // Prefab de la moto de l'ML-Agent

    [HideInInspector]
    public int currentMatchId;

    private Dictionary<int, PlayerController> activePlayers = new Dictionary<int, PlayerController>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (SocketClient.Instance != null)
        {
            SocketClient.Instance.OnMatchStarted += HandleMatchStarted;
            SocketClient.Instance.OnOpponentMoved += HandleOpponentMoved;
            SocketClient.Instance.OnMatchEnded += HandleMatchEnded;
        }
    }

    private void HandleMatchStarted(string matchDataJson)
    {
        Debug.Log("Intentant iniciar partida amb dades: " + matchDataJson);
        try
        {
            MatchData match = JsonConvert.DeserializeObject<MatchData>(matchDataJson);
            currentMatchId = match.id;
            
            int myId = APIClient.Instance.CurrentUser.id;

            Vector2 pos1 = new Vector2(-5, 0);
            Vector2 pos2 = new Vector2(5, 0);

            for (int i = 0; i < match.players.Count; i++)
            {
                int pId = match.players[i];
                bool isMe = (pId == myId);
                Vector2 startPos = (i == 0) ? pos1 : pos2;

                GameObject playerObj = Instantiate(motoPrefab, startPos, Quaternion.identity);
                PlayerController controller = playerObj.GetComponent<PlayerController>();
                
                controller.playerId = pId;
                controller.isLocalPlayer = isMe;
                controller.wallPrefab = Resources.Load<GameObject>("WallPrefab");

                activePlayers[pId] = controller;
            }

            if (match.players.Count == 1)
            {
                Debug.Log("Iniciant partida contra la IA (ML-Agents)");
                GameObject aiObj = Instantiate(aiPrefab, pos2, Quaternion.identity);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error a HandleMatchStarted: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private void HandleOpponentMoved(string moveDataJson)
    {
        MoveData move = JsonConvert.DeserializeObject<MoveData>(moveDataJson);
        
        if (activePlayers.TryGetValue(move.playerId, out PlayerController opponent))
        {
            Vector2 pos = new Vector2(move.position.x, move.position.y);
            Vector2 dir = new Vector2(move.direction.x, move.direction.y);
            
            opponent.UpdateRemoteTransform(pos, dir);
        }
    }

    private void HandleMatchEnded(string endDataJson)
    {
        Debug.Log("La partida ha finalitzat: " + endDataJson);
        // Aquí podríem aturar el temps: Time.timeScale = 0;
        // I mostrar un panell de Victòria/Derrota
    }

    private void OnDestroy()
    {
        if (SocketClient.Instance != null)
        {
            SocketClient.Instance.OnMatchStarted -= HandleMatchStarted;
            SocketClient.Instance.OnOpponentMoved -= HandleOpponentMoved;
            SocketClient.Instance.OnMatchEnded -= HandleMatchEnded;
        }
    }
}
