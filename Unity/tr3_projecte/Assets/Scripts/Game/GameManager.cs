using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class MatchData
{
    public int id;
    public List<string> players;
    public string status;
    public string winnerId;
    public Dictionary<string, string> playerColors;
}

[Serializable]
public class MoveData
{
    public string playerId;
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
    public GameObject coinPrefab; // Prefab de la moneda
    public Vector2 coinSpawnRange = new Vector2(4f, 4f);

    private int currentScore = 0;
    private bool isSoloMode = false;
    private CoinSpawner coinSpawner;

    [HideInInspector]
    public int currentMatchId;

    public Dictionary<string, PlayerController> activePlayers = new Dictionary<string, PlayerController>();

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

        coinSpawner = gameObject.AddComponent<CoinSpawner>();
        coinSpawner.coinPrefab = coinPrefab;
        coinSpawner.spawnRange = coinSpawnRange;
    }

    private void HandleMatchStarted(string matchDataJson)
    {
        Debug.Log("La partida ha començat: " + matchDataJson);
        MatchData match = JsonConvert.DeserializeObject<MatchData>(matchDataJson);
        currentMatchId = match.id;
        
        string myId = APIClient.Instance.CurrentUser.id;

        Vector2 pos1 = new Vector2(-4, 0);
        Vector2 pos2 = new Vector2(4, 0);

        for (int i = 0; i < match.players.Count; i++)
        {
            string pId = match.players[i];
            bool isMe = (pId == myId);
            Vector2 startPos = (i == 0) ? pos1 : pos2;

            // Llegim el color que ens envia el servidor (en Hex)
            Color playerColor = Color.cyan; // Per defecte
            if (match.playerColors != null && match.playerColors.ContainsKey(pId))
            {
                ColorUtility.TryParseHtmlString(match.playerColors[pId], out playerColor);
            }

            SpawnPlayer(pId, startPos, isMe, playerColor);
        }

        if (match.players.Count == 1)
        {
            Debug.Log("Iniciant partida contra la IA (ML-Agents)");
            GameObject aiObj = Instantiate(aiPrefab, pos2, Quaternion.identity);
            PlayerController aiController = aiObj.GetComponent<PlayerController>();
            aiController.playerId = "AI";
            activePlayers["AI"] = aiController;
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
        MatchData match = JsonConvert.DeserializeObject<MatchData>(endDataJson);
        
        string message = "Partida Finalitzada";
        if (!string.IsNullOrEmpty(match.winnerId))
        {
            message = (match.winnerId == APIClient.Instance.CurrentUser.id) ? "HAS GUANYAT!" : "HAS PERDUT...";
        }

        AuthUI.Instance.ShowGameOver(message);
    }

    public void NotifyPlayerDeath(string pId)
    {
        if (activePlayers.ContainsKey(pId))
        {
            activePlayers.Remove(pId);
        }

        if (isSoloMode)
        {
            AuthUI.Instance.ShowGameOver("HAS XOCAT!\nMonedes recollides: " + currentScore);
            isSoloMode = false;
            return;
        }

        // Si només queda un jugador, la partida ha acabat (per al mode offline)
        if (activePlayers.Count == 1)
        {
            foreach (var player in activePlayers.Values)
            {
                string msg = (player.isLocalPlayer) ? "HAS GUANYAT!" : "LA IA T'HA GUANYAT...";
                AuthUI.Instance.ShowGameOver(msg);
                break;
            }
        }
        else if (activePlayers.Count == 0)
        {
            AuthUI.Instance.ShowGameOver("EMPANT (Tots dos heu xocat!)");
        }
    }

    public void CleanCurrentMatch()
    {
        // 1. Esborrem les motos que encara estiguin vives
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            Destroy(player.gameObject);
        }

        // 2. Esborrem TOTS els murs/esteles que s'hagin creat (els que són clons)
        GameObject[] allWalls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in allWalls)
        {
            // Si el nom conté "(Clone)", és una estela creada pel jugador, no una paret de l'arena
            if (wall.name.Contains("(Clone)"))
            {
                Destroy(wall);
            }
        }

        // Esborrem les monedes si n'hi ha
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
        {
            Destroy(coin.gameObject);
        }

        activePlayers.Clear();
        currentScore = 0;
    }

    public void StartOfflineAiMatch(Color playerColor)
    {
        Debug.Log("Iniciant partida OFFLINE contra la IA");
        
        // 1. Spawnegem al jugador humà amb el color triat
        SpawnPlayer("PLAYER", new Vector2(-4, 0), true, playerColor);
        
        // 2. Spawnegem a la IA amb un color vermell/taronja per contrastar
        GameObject aiObj = Instantiate(aiPrefab, new Vector2(4, 0), Quaternion.identity);
        PlayerController aiController = aiObj.GetComponent<PlayerController>();
        aiController.playerId = "AI"; // ID per a la IA
        aiController.isLocalPlayer = false;
        aiController.isTrainingMode = false;
        aiController.wallPrefab = Resources.Load<GameObject>("WallPrefab");
        aiController.SetColor(new Color(1f, 0.3f, 0f)); // Taronja neó per a la IA
        
        activePlayers["AI"] = aiController;
        
        // Ens assegurem que la IA tingui el model carregat (Inference Only)
        // Nota: El fitxer .onnx s'ha d'haver assignat al prefab a l'editor.
    }

    public void StartSoloMode(Color playerColor)
    {
        Debug.Log("Iniciant MODE INDIVIDUAL (Recollir monedes)");
        isSoloMode = true;
        currentScore = 0;
        
        // Spawnegem al jugador al centre
        SpawnPlayer("LOCAL", Vector2.zero, true, playerColor);
        
        // Spawnegem la primera moneda
        coinSpawner.SpawnCoin();
    }

    public void CoinCollected()
    {
        currentScore++;
        Debug.Log("Moneda recollida! Puntuació: " + currentScore);
        
        // Spawnegem la següent moneda
        coinSpawner.SpawnCoin();
    }

    private void SpawnPlayer(string pId, Vector2 pos, bool isLocal, Color c)
    {
        GameObject playerObj = Instantiate(motoPrefab, pos, Quaternion.identity);
        PlayerController controller = playerObj.GetComponent<PlayerController>();
        
        controller.playerId = pId;
        controller.isLocalPlayer = isLocal;
        controller.isTrainingMode = false;
        controller.wallPrefab = Resources.Load<GameObject>("WallPrefab");
        controller.SetColor(c);

        activePlayers[pId] = controller;
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
