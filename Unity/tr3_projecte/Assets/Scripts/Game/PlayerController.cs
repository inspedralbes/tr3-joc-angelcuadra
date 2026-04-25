using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    public GameObject wallPrefab;
    
    // Variables de xarxa
    public bool isLocalPlayer = false;
    public int playerId;
    public bool isTrainingMode = false;

    private Vector2 currentDirection = Vector2.up;
    private Vector2 nextDirection = Vector2.up;
    private Collider2D wallCollider;
    private Vector2 lastWallEnd;
    
    private List<GameObject> myWalls = new List<GameObject>();
    private float gracePeriod = 0.5f;
    private MotoAgent agentRef;

    private void Awake()
    {
        agentRef = GetComponent<MotoAgent>();
    }

    private void Start()
    {
        // Totes dues motos (local i remota) comencen a deixar l'estela
        SpawnWall();
    }

    private void Update()
    {
        if (gracePeriod > 0) gracePeriod -= Time.deltaTime;

        if (isTrainingMode)
        {
            MovePlayer();
            UpdateWall();
            return;
        }

        if (!isLocalPlayer) return;

        HandleInput();
        MovePlayer();
        UpdateWall();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && currentDirection != Vector2.down) nextDirection = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && currentDirection != Vector2.up) nextDirection = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && currentDirection != Vector2.right) nextDirection = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentDirection != Vector2.left) nextDirection = Vector2.right;

        if (nextDirection != currentDirection)
        {
            currentDirection = nextDirection;
            
            if (currentDirection == Vector2.up) transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (currentDirection == Vector2.down) transform.rotation = Quaternion.Euler(0, 0, 180);
            else if (currentDirection == Vector2.left) transform.rotation = Quaternion.Euler(0, 0, 90);
            else if (currentDirection == Vector2.right) transform.rotation = Quaternion.Euler(0, 0, -90);

            SpawnWall();
            SocketClient.Instance.SendMove(GameManager.Instance.currentMatchId, transform.position, currentDirection);
        }
    }

    private void MovePlayer()
    {
        transform.Translate(currentDirection * speed * Time.deltaTime, Space.World);
    }

    private void SpawnWall()
    {
        lastWallEnd = transform.position;
        // Parentem el mur al pare (ex: TrainingArena) perquè no quedi suelto
        GameObject wall = Instantiate(wallPrefab, transform.position, Quaternion.identity, transform.parent);
        wallCollider = wall.GetComponent<Collider2D>();
        myWalls.Add(wall);
    }

    private void UpdateWall()
    {
        if (wallCollider != null)
        {
            Vector2 currentPos = transform.position;
            Vector2 center = (lastWallEnd + currentPos) / 2f;
            wallCollider.transform.position = center;
            
            float dist = Vector2.Distance(lastWallEnd, currentPos);
            
            if (currentDirection.x != 0) wallCollider.transform.localScale = new Vector3(dist, 0.5f, 1f);
            else wallCollider.transform.localScale = new Vector3(0.5f, dist, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLocalPlayer && !isTrainingMode) return;
        if (gracePeriod > 0) return; // Invencible el primer mig segon

        if (collision == wallCollider) return;

        if (collision.CompareTag("Wall") || collision.CompareTag("Player"))
        {
            Debug.Log($"Has xocat amb: {collision.gameObject.name} (Tag: {collision.tag})");
            
            if (isTrainingMode && agentRef != null)
            {
                agentRef.RegisterCrash();
            }
            else
            {
                SocketClient.Instance.SendCollision(GameManager.Instance.currentMatchId);
                Destroy(gameObject);
            }
        }
    }

    public void UpdateRemoteTransform(Vector2 pos, Vector2 dir)
    {
        transform.position = pos;
        
        if (dir != currentDirection)
        {
            currentDirection = dir;

            if (currentDirection == Vector2.up) transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (currentDirection == Vector2.down) transform.rotation = Quaternion.Euler(0, 0, 180);
            else if (currentDirection == Vector2.left) transform.rotation = Quaternion.Euler(0, 0, 90);
            else if (currentDirection == Vector2.right) transform.rotation = Quaternion.Euler(0, 0, -90);

            SpawnWall();
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer && !isTrainingMode)
        {
            MovePlayer();
            UpdateWall();
        }
    }

    // Funcions cridades per la IA (ML-Agents)
    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }

    public void SetDirectionFromAI(Vector2 newDir)
    {
        nextDirection = newDir;
    }

    public void ResetForTraining()
    {
        // Esborrem tots els murs d'aquesta moto
        foreach (var w in myWalls)
        {
            if (w != null) Destroy(w);
        }
        myWalls.Clear();

        gracePeriod = 0.5f;
        currentDirection = Vector2.up;
        nextDirection = Vector2.up;
        transform.rotation = Quaternion.identity;

        SpawnWall();
    }
}
