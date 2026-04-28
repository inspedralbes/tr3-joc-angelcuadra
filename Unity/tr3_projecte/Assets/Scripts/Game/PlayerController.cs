using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    public GameObject wallPrefab;
    public float wallThickness = 0.5f;
    
    // Variables de xarxa
    public bool isLocalPlayer = false;
    public string playerId;
    public bool isTrainingMode = false;

    public Vector2 initialDirection = Vector2.up; // Assignada pel GameManager abans de Start()
    private Vector2 currentDirection = Vector2.up;
    private Vector2 nextDirection = Vector2.up;
    private Collider2D wallCollider;
    private Collider2D previousWallCollider;
    private Vector2 lastWallEnd;
    
    private List<GameObject> myWalls = new List<GameObject>();
    private float gracePeriod = 2.0f; // Immortal des del naixement (mil·lisegon zero)
    private MotoAgent agentRef;
    private Color myColor = Color.white;
    public bool isDead = false; // Bandera per evitar col·lisions fantasma en el darrer frame

    private void Awake()
    {
        agentRef = GetComponent<MotoAgent>();
        gracePeriod = 2.0f; // Forçem la imunitat aquí perquè l'Inspector no ens la canvii

        // Seguretat física: Evitem que la moto es mogui per forces o gravetat
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void Start()
    {
        gracePeriod = 2.0f; // Imunitat durant 2 segons per evitar morts prematures
        Debug.Log($"[START] Moto {playerId} | Local: {isLocalPlayer} | Grace: {gracePeriod}");

        // Apliquem la direcció assignada pel GameManager (UP per defecte si no s'ha assignat)
        if (initialDirection != Vector2.zero)
        {
            currentDirection = initialDirection;
            nextDirection = initialDirection;
        }
        else if (currentDirection == Vector2.zero)
        {
            currentDirection = Vector2.right;
            nextDirection = Vector2.right;
        }

        // Rotem el sprite per coincidir amb la direcció inicial
        if (currentDirection == Vector2.up)         transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (currentDirection == Vector2.down)   transform.rotation = Quaternion.Euler(0, 0, 180);
        else if (currentDirection == Vector2.left)   transform.rotation = Quaternion.Euler(0, 0, 90);
        else if (currentDirection == Vector2.right)  transform.rotation = Quaternion.Euler(0, 0, -90);

        Debug.Log($"[START] Moto {playerId} | Local: {isLocalPlayer} | Dir: {currentDirection}");
        SpawnWall();
    }

    private void Update()
    {
        if (gracePeriod > 0) gracePeriod -= Time.deltaTime;

        // CAS 1: JUGADOR LOCAL
        if (isLocalPlayer)
        {
            if (!isTrainingMode) HandleInput();
            
            ApplyDirection();
            MovePlayer();
            UpdateWall();
        }
        // CAS 2: IA (Inference o Training)
        else if (agentRef != null)
        {
            ApplyDirection();
            MovePlayer();
            UpdateWall();
        }
        // CAS 3: JUGADOR REMOT (Xarxa) -> Es mou a FixedUpdate o UpdateRemoteTransform
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && currentDirection != Vector2.down) nextDirection = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && currentDirection != Vector2.up) nextDirection = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && currentDirection != Vector2.right) nextDirection = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentDirection != Vector2.left) nextDirection = Vector2.right;
    }

    private void ApplyDirection()
    {
        if (nextDirection != currentDirection)
        {
            // Primer, tanquem el mur actual exactament on som ara abans de girar
            UpdateWall();

            currentDirection = nextDirection;
            
            if (currentDirection == Vector2.up) transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (currentDirection == Vector2.down) transform.rotation = Quaternion.Euler(0, 0, 180);
            else if (currentDirection == Vector2.left) transform.rotation = Quaternion.Euler(0, 0, 90);
            else if (currentDirection == Vector2.right) transform.rotation = Quaternion.Euler(0, 0, -90);

            previousWallCollider = wallCollider;
            SpawnWall();
            
            if (!isTrainingMode && isLocalPlayer)
            {
                SocketClient.Instance.SendMove(GameManager.Instance.currentMatchId, transform.position, currentDirection);
            }
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
        wall.tag = "Wall"; // SEGURETAT: Ens assegurem que tingui el Tag per a la IA
        
        SpriteRenderer wallSR = wall.GetComponent<SpriteRenderer>();
        wallSR.color = myColor;
        wallSR.sortingOrder = -1; // L'estela es renderitza DARRERE de la moto (moto = ordre 0)
        
        wallCollider = wall.GetComponent<Collider2D>();
        myWalls.Add(wall);
    }

    private void UpdateWall()
    {
        if (wallCollider != null)
        {
            Vector2 currentPos = transform.position;
            float dist = Vector2.Distance(lastWallEnd, currentPos);
            
            // Posicionem el centre exactament a la meitat del camí recorregut
            wallCollider.transform.position = (lastWallEnd + currentPos) / 2f;
            
            // Calculem l'angle segons la direcció actual
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90;
            wallCollider.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Ara la Y és sempre la llargada i la X el gruix.
            // No afegim el +0.01f per veure si així queda més net.
            wallCollider.transform.localScale = new Vector3(wallThickness, dist, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug de qualsevol cosa que toquem
        Debug.Log($"[COL·LISIÓ] {playerId} ha tocat {other.gameObject.name} (Tag: {other.tag}) | Grace: {gracePeriod} | isDead: {isDead}");

        // Protecció principal: si ja estem morts (cleanup en curs), ignorem tot
        if (isDead) return;
        // Només comprovem col·lisions si som el jugador local o una IA
        if (!isLocalPlayer && agentRef == null) return;
        
        if (gracePeriod > 0) 
        {
            Debug.Log($"Ignorant col·lisió de {playerId} per gracePeriod ({gracePeriod})");
            return; 
        }

        if (other.CompareTag("Wall") || other.CompareTag("Player"))
        {
            // IGNORAR si és un dels meus murs (comprovem tota la llista, no només l'últim)
            if (myWalls.Contains(other.gameObject)) return;
            
            // IGNORAR també si és el collider de la pròpia moto (per si de cas)
            if (other.gameObject == gameObject) return;

            Debug.Log("<color=red>COL·LISIÓ!</color> El jugador " + playerId + " ha xocat amb: " + other.gameObject.name + " (Tag: " + other.tag + ") a la posició " + transform.position);
            
            if (isTrainingMode && agentRef != null)
            {
                agentRef.RegisterCrash();
            }
            else
            {
                isDead = true; // Marquem com a mort ABANS d'enviar res
                Debug.Log("Jugador " + playerId + " MORT. Enviant col·lisió al servidor...");
                SocketClient.Instance.SendCollision(GameManager.Instance.currentMatchId);
                GameManager.Instance.NotifyPlayerDeath(playerId);
                
                // Desactivem el collider immediatament per evitar múltiples triggers
                GetComponent<Collider2D>().enabled = false;
                Destroy(gameObject, 0.1f);
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
        // El FixedUpdate NOMÉS el fem servir per als jugadors remots (xarxa)
        // per interpolar la seva posició si no som nosaltres ni una IA local.
        if (!isLocalPlayer && agentRef == null)
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

        if (currentDirection == Vector2.zero)
            currentDirection = Vector2.right;

        Debug.Log($"Moto {playerId} iniciada. Local: {isLocalPlayer}, Training: {isTrainingMode}");

        gracePeriod = 0.5f;
        currentDirection = Vector2.right;
        nextDirection = Vector2.right;
        transform.rotation = Quaternion.identity;

        SpawnWall();
    }

    public void SetColor(Color c)
    {
        // Multipliquem el color per una intensitat per fer-lo HDR i que el Bloom brilli
        float intensity = 2.5f;
        Color hdrColor = new Color(c.r * intensity, c.g * intensity, c.b * intensity, c.a);
        
        myColor = hdrColor;
        GetComponent<SpriteRenderer>().color = hdrColor;
    }
}
