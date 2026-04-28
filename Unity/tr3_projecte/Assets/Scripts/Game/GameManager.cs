using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class MatchData
{
    public int id;
    public string roomName;
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
            // NETEJEM subscripcions velles per evitar duplicats
            SocketClient.Instance.OnMatchStarted -= HandleMatchStarted;
            SocketClient.Instance.OnMatchStarted += HandleMatchStarted;
            
            SocketClient.Instance.OnOpponentMoved -= HandleOpponentMoved;
            SocketClient.Instance.OnOpponentMoved += HandleOpponentMoved;
            
            SocketClient.Instance.OnMatchEnded -= HandleMatchEnded;
            SocketClient.Instance.OnMatchEnded += HandleMatchEnded;
        }

        coinSpawner = gameObject.AddComponent<CoinSpawner>();
        coinSpawner.coinPrefab = coinPrefab;
        coinSpawner.spawnRange = coinSpawnRange;
    }

    private void HandleMatchStarted(string matchDataJson)
    {
        MatchData match = JsonConvert.DeserializeObject<MatchData>(matchDataJson);

        // Protecció: ignorem únivament si és un DUPLICAT de la mateixa partida en curs
        if (currentMatchId != 0 && currentMatchId == match.id) {
            Debug.LogWarning("Avís: S'ha rebut matchStarted duplicat per la partida " + match.id + ". Ignorant.");
            return;
        }

        Debug.Log("La partida ha començat: " + matchDataJson);
        StartMatch(match);
    }

    public void StartMatch(MatchData match)
    {
        string myId = APIClient.Instance.CurrentUser.id;

        // Netegem PRIMER (reseteja currentMatchId a 0 i destrueix objectes vells)
        CleanCurrentMatch();
        // Després assignem el nou ID de partida
        currentMatchId = match.id;
        
        for (int i = 0; i < match.players.Count; i++)
        {
            string pId = match.players[i];
            bool isMe = (pId == myId);
            
            // El host a l'esquerra (-10), el convidat a la dreta (10)
            Vector2 spawnPos = (i == 0) ? new Vector2(-10, 0) : new Vector2(10, 0);
            
            // Llegim el color que ens envia el servidor
            Color playerColor = Color.cyan;
            if (match.playerColors != null && match.playerColors.ContainsKey(pId))
            {
                ColorUtility.TryParseHtmlString(match.playerColors[pId], out playerColor);
            }

            SpawnPlayer(pId, spawnPos, isMe, playerColor);
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
        Debug.Log("La partida ha finalitzat (Resposta Servidor): " + endDataJson);
        currentMatchId = 0; // Netegem l'ID per permetre noves partides
        AuthUI.Instance.ShowGameOver(endDataJson);
    }

    public void NotifyPlayerDeath(string pId)
    {
        Debug.Log("Notificant mort del jugador: " + pId);
        
        // Esborrem el jugador del diccionari de forma segura
        if (activePlayers.ContainsKey(pId)) activePlayers.Remove(pId);

        // Si és multijugador, NO fem res més (esperem el missatge oficial del servidor)
        if (currentMatchId != 0) return;

        if (isSoloMode)
        {
            var soloResult = new { winnerId = "NONE", loserId = pId, message = "HAS XOCAT!\nMonedes: " + currentScore };
            AuthUI.Instance.ShowGameOver(JsonConvert.SerializeObject(soloResult));
            isSoloMode = false;
            return;
        }

        // Lògica per a partides OFFLINE (IA)
        if (activePlayers.Count <= 1)
        {
            string winner = "DESCONEGUT";
            foreach (var p in activePlayers.Values) winner = p.playerId;
            
            var result = new { winnerId = winner, loserId = pId };
            AuthUI.Instance.ShowGameOver(JsonConvert.SerializeObject(result));
        }
    }

    public void CleanCurrentMatch()
    {
        Debug.Log("Netejant escena per a nova partida...");
        
        // Reset de l'ID PRIMER, així cap col·lisió diferida pot enviar per la partida vella
        currentMatchId = 0;

        // 1. Destruir tots els jugadors (marcar isDead i desactivar COL·LIDERS immediatament)
        foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            player.isDead = true; // Evita col·lisions en el darrer frame (Destroy és diferit!)
            foreach (var col in player.GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            player.gameObject.SetActive(false); 
            Destroy(player.gameObject);
        }
        
        // 2. Destruir TOTS els murs/esteles (desactivar col·lidor immediatament)
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in walls)
        {
            var col = wall.GetComponent<Collider2D>();
            if (col != null) col.enabled = false; // Desactiva abans del Destroy diferit
            wall.SetActive(false);
            Destroy(wall);
        }

        // 3. Esborrem les monedes
        foreach (var coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
        {
            coin.gameObject.SetActive(false);
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

        // Direcció inicial basada en posició: esquerra va cap amunt, dreta cap amunt
        // Ambdós comencen cap amunt (UP) → els murs creixen cap avall, no xoquen mai immediatament
        if (pos.x < 0)
            controller.initialDirection = Vector2.up;   // jugador esquerra
        else
            controller.initialDirection = Vector2.up;   // jugador dreta (també amunt, diferent carril)

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
