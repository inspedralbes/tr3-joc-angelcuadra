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

        SetupGameView();
    }

    private void SetupGameView()
    {
        // 1. Busquem si existeix una TrainingArena a l'escena
        GameObject arena = GameObject.Find("TrainingArena");
        Vector3 arenaCenter = Vector3.zero;
        float cameraSize = 15f;

        if (arena != null)
        {
            Debug.Log("<color=green>GameManager: S'ha trobat una TrainingArena! L'utilitzarem de base.</color>");
            arenaCenter = arena.transform.position;
            cameraSize = 8.5f; // Zoom més agressiu perquè es vegin les motos
        }
        else
        {
            Debug.Log("<color=orange>GameManager: No s'ha trobat TrainingArena. Creant límits dinàmics.</color>");
            CreateArenaBoundaries(15f); // Arena més petita i controlada
            cameraSize = 18f;
        }

        // 2. Configurem la càmera de forma FIXA per veure tot el camp
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(arenaCenter.x, arenaCenter.y, -10f);
            cam.transform.rotation = Quaternion.Euler(0, 0, 0);
            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            
            // Eliminem el script de seguiment si existia
            CameraFollow oldFollow = cam.GetComponent<CameraFollow>();
            if (oldFollow != null) Destroy(oldFollow);
        }
    }

    private void CreateArenaBoundaries(float size)
    {
        float thickness = 1f;
        SpawnBoundaryWall(new Vector2(0, size), new Vector2(size * 2, thickness), "TopWall");
        SpawnBoundaryWall(new Vector2(0, -size), new Vector2(size * 2, thickness), "BottomWall");
        SpawnBoundaryWall(new Vector2(-size, 0), new Vector2(thickness, size * 2), "LeftWall");
        SpawnBoundaryWall(new Vector2(size, 0), new Vector2(thickness, size * 2), "RightWall");
    }

    private void SpawnBoundaryWall(Vector2 pos, Vector2 scale, string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = pos;
        wall.tag = "Wall";
        var col = wall.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        wall.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        
        // Opcional: Afegir un color perquè es vegi el límit
        var sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UnityCommandLineInternal_UIPoint"); // Un sprite blanc per defecte
        sr.color = new Color(1, 1, 1, 0.2f);
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
            
            // Host a l'esquerra (-5), Convidat a la dreta (5) - Més segur per arenes petites
            Vector2 spawnPos = (i == 0) ? new Vector2(-5, 0) : new Vector2(5, 0);
            
            // Si tenim arena, sumem la seva posició per centrar-los
            GameObject arena = GameObject.Find("TrainingArena");
            if (arena != null) spawnPos += (Vector2)arena.transform.position;
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
        
        // IGNORAR si som nosaltres mateixos (el servidor ens reenvia el nostre propi moviment)
        if (APIClient.Instance.CurrentUser != null && move.playerId == APIClient.Instance.CurrentUser.id) return;

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
        currentMatchId = 0;

        // 1. Destruir jugadors
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.isDead = true;
            Destroy(p.gameObject);
        }

        // 2. Destruir TOTS els objectes amb tag "Wall" (incloses esteles i límits vells)
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in walls)
        {
            Destroy(wall);
        }

        // 3. Destruir monedes
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

        // Direcció inicial basada en posició: esquerra va cap amunt, dreta cap amunt
        // Ambdós comencen cap amunt (UP) → els murs creixen cap avall, no xoquen mai immediatament
        if (pos.x < 0)
            controller.initialDirection = Vector2.up;   // jugador esquerra
        else
            controller.initialDirection = Vector2.up;   // jugador dreta (també amunt, diferent carril)

        activePlayers[pId] = controller;

        // Ja no fem que la càmera segueixi el jugador individual
        // cam.SetTarget(playerObj.transform);
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
