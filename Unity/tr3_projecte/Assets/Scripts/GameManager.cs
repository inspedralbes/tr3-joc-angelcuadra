using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject motoPrefab; // Prefab de la moto estètica Tron
    public GameObject trailPrefab; // Prefab de l'estela de la moto

    // Diccionari per guardar els jugadors actius a l'escena
    private Dictionary<int, GameObject> activePlayers = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Ens subscrivim als esdeveniments de xarxa per actualitzar el joc
        if (SocketClient.Instance != null)
        {
            SocketClient.Instance.OnMatchStarted += HandleMatchStarted;
            SocketClient.Instance.OnOpponentMoved += HandleOpponentMoved;
            SocketClient.Instance.OnMatchEnded += HandleMatchEnded;
        }
    }

    private void HandleMatchStarted(string matchDataJson)
    {
        Debug.Log("Partida iniciada! Dades: " + matchDataJson);
        // Aquí instanciaríem les nostres motos (player1 i player2 o AI)
        // Parsejar JSON per obtenir posicions inicials i instanciar motoPrefab
    }

    private void HandleOpponentMoved(string moveDataJson)
    {
        // Aquest esdeveniment es dispara quan rebem una actualització per WebSocket
        Debug.Log("El rival s'ha mogut: " + moveDataJson);
        // Deserialitzar i actualitzar la posició o direcció de la moto rival
    }

    private void HandleMatchEnded(string endDataJson)
    {
        Debug.Log("La partida ha finalitzat: " + endDataJson);
        // Mostrar pantalla de victòria o derrota segons si som el winnerId
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
