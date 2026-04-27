using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

public class AuthUI : MonoBehaviour
{
    public static AuthUI Instance { get; private set; }
    private UIDocument uiDocument;
    
    // Elements de la UI
    private VisualElement authPanel;
    private VisualElement lobbyPanel;
    
    private TextField usernameInput;
    private TextField passwordInput;
    private Button loginButton;
    private Button registerButton;
    private Label statusText;

    // Elements del Lobby
    private Button createMatchButton;
    private Button joinMatchButton;
    private Button playAiButton;
    private Button soloModeButton;
    private Label lobbyStatusText;
    
    private Label userWinsLabel;
    private Label userCoinsLabel;
    private ScrollView matchList;

    // Elements de Game Over
    private VisualElement gameOverPanel;
    private Label winnerText;
    private Button returnLobbyButton;

    private Color selectedColor = Color.cyan;
    private List<Button> colorButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        authPanel = root.Q<VisualElement>("AuthPanel");
        lobbyPanel = root.Q<VisualElement>("LobbyPanel");

        usernameInput = root.Q<TextField>("UsernameInput");
        passwordInput = root.Q<TextField>("PasswordInput");
        loginButton = root.Q<Button>("LoginButton");
        registerButton = root.Q<Button>("RegisterButton");
        statusText = root.Q<Label>("StatusText");

        createMatchButton = root.Q<Button>("CreateMatchButton");
        joinMatchButton = root.Q<Button>("JoinMatchButton");
        playAiButton = root.Q<Button>("PlayAiButton");
        lobbyStatusText = root.Q<Label>("LobbyStatusText");

        loginButton.clicked += OnLoginClicked;
        registerButton.clicked += OnRegisterClicked;
        
        createMatchButton.clicked += OnCreateMatchClicked;
        playAiButton.clicked += OnPlayAiClicked;
        soloModeButton = root.Q<Button>("SoloModeButton");
        soloModeButton.clicked += OnSoloModeClicked;

        userWinsLabel = root.Q<Label>("UserWinsLabel");
        userCoinsLabel = root.Q<Label>("UserCoinsLabel");
        matchList = root.Q<ScrollView>("MatchList");

        gameOverPanel = root.Q<VisualElement>("GameOverPanel");
        winnerText = root.Q<Label>("WinnerText");
        returnLobbyButton = root.Q<Button>("ReturnLobbyButton");
        returnLobbyButton.clicked += OnReturnLobbyClicked;

        // Colors
        SetupColorButton(root, "ColorCyan", Color.cyan);
        SetupColorButton(root, "ColorYellow", Color.yellow);
        SetupColorButton(root, "ColorMagenta", Color.magenta);
        SetupColorButton(root, "ColorGreen", Color.green);

        ShowAuthPanel();
        
        // Ens subscrivim a l'inici del joc per amagar la UI
        if (SocketClient.Instance != null)
        {
            SocketClient.Instance.OnMatchStarted += HandleMatchStarted;
            SocketClient.Instance.OnMatchesUpdated += UpdateMatchList;
            SocketClient.Instance.OnMatchCreated += HandleMatchCreated;
        }
    }

    private void OnDisable()
    {
        if (loginButton != null) loginButton.clicked -= OnLoginClicked;
        if (registerButton != null) registerButton.clicked -= OnRegisterClicked;
        if (createMatchButton != null) createMatchButton.clicked -= OnCreateMatchClicked;
        if (joinMatchButton != null) joinMatchButton.clicked -= OnJoinMatchClicked;
        if (playAiButton != null) playAiButton.clicked -= OnPlayAiClicked;
        if (soloModeButton != null) soloModeButton.clicked -= OnSoloModeClicked;
        
        if (SocketClient.Instance != null)
        {
            SocketClient.Instance.OnMatchStarted -= HandleMatchStarted;
            SocketClient.Instance.OnMatchesUpdated -= UpdateMatchList;
            SocketClient.Instance.OnMatchCreated -= HandleMatchCreated;
        }
    }

    private void HandleMatchStarted(string data)
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void HandleMatchCreated(string matchId)
    {
        lobbyStatusText.text = "Partida creada! Esperant oponent...";
    }

    private void OnLoginClicked()
    {
        string username = usernameInput.value;
        string password = passwordInput.value;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Error: Camps buits.";
            return;
        }

        loginButton.SetEnabled(false);
        statusText.text = "Connectant...";

        APIClient.Instance.Login(username, password, (success, message) =>
        {
            loginButton.SetEnabled(true);
            if (success)
            {
                statusText.text = "Login correcte!";
                ShowLobbyPanel();
            }
            else
            {
                statusText.text = "Error: " + message;
            }
        });
    }

    private void OnRegisterClicked()
    {
        string username = usernameInput.value;
        string password = passwordInput.value;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Error: Camps buits.";
            return;
        }

        registerButton.SetEnabled(false);
        statusText.text = "Registrant...";

        APIClient.Instance.Register(username, password, (success, message) =>
        {
            registerButton.SetEnabled(true);
            if (success)
            {
                statusText.text = "Registre correcte! Ara fes Login.";
            }
            else
            {
                statusText.text = "Error: " + message;
            }
        });
    }

    private void ShowAuthPanel()
    {
        authPanel.style.display = DisplayStyle.Flex;
        lobbyPanel.style.display = DisplayStyle.None;
    }

    private void ShowLobbyPanel()
    {
        authPanel.style.display = DisplayStyle.None;
        lobbyPanel.style.display = DisplayStyle.Flex;
        
        // Omplim les estadístiques de l'usuari que venen de MongoDB
        if (APIClient.Instance.CurrentUser != null)
        {
            userWinsLabel.text = "Wins: " + APIClient.Instance.CurrentUser.wins;
            userCoinsLabel.text = "Monedes: " + APIClient.Instance.CurrentUser.coinsCollected;
        }

        SocketClient.Instance.Connect();
        // Donem un segon perquè es connecti i demanem el lobby
        Invoke("JoinLobbyAfterConnect", 1.0f);
    }

    private void JoinLobbyAfterConnect()
    {
        SocketClient.Instance.JoinLobby();
    }

    private void UpdateMatchList(string json)
    {
        matchList.Clear();
        List<MatchData> matches = JsonConvert.DeserializeObject<List<MatchData>>(json);

        if (matches == null || matches.Count == 0)
        {
            Label empty = new Label("No hi ha partides actives. Crea'n una!");
            empty.style.color = Color.gray;
            empty.style.paddingLeft = 10;
            matchList.Add(empty);
            return;
        }

        foreach (var match in matches)
        {
            Button btn = new Button();
            btn.text = "SALA: " + match.roomName + " (" + match.players.Count + "/2)";
            btn.AddToClassList("btn");
            btn.AddToClassList("btn-secondary");
            btn.style.marginTop = 5;
            
            btn.clicked += () => 
            {
                lobbyStatusText.text = "Unint-se a la sala " + match.id + "...";
                SocketClient.Instance.JoinMatch(match.id, APIClient.Instance.CurrentUser.id, selectedColor);
            };

            matchList.Add(btn);
        }
    }

    private void OnCreateMatchClicked()
    {
        string roomName = "Sala de " + APIClient.Instance.CurrentUser.username;
        string myId = APIClient.Instance.CurrentUser.id;
        SocketClient.Instance.CreateMatch(myId, roomName, selectedColor);
        lobbyStatusText.text = "Creant partida...";
    }

    private void OnJoinMatchClicked()
    {
        // Busquem quin botó s'ha premut (o l'ID de la partida)
        int matchId = 1; // Aquí aniria la lògica d'obtenir l'ID del botó premut
        string myId = APIClient.Instance.CurrentUser.id;
        SocketClient.Instance.JoinMatch(matchId, myId, selectedColor);
        lobbyStatusText.text = "Unint-se a la partida " + matchId + "...";
    }

    private void OnPlayAiClicked()
    {
        lobbyStatusText.text = "Iniciant partida contra la CPU...";
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        GameManager.Instance.StartOfflineAiMatch(selectedColor);
    }

    private void OnSoloModeClicked()
    {
        lobbyStatusText.text = "Iniciant mode individual...";
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        GameManager.Instance.StartSoloMode(selectedColor);
    }

    private void SetupColorButton(VisualElement root, string name, Color color)
    {
        Button btn = root.Q<Button>(name);
        if (btn != null)
        {
            colorButtons.Add(btn);
            btn.clicked += () => 
            {
                selectedColor = color;
                // Feedback visual: opacitat
                foreach (var b in colorButtons) b.style.opacity = 0.5f;
                btn.style.opacity = 1.0f;
                lobbyStatusText.text = "Color seleccionat!";
            };
        }
    }

    public Color GetSelectedColor()
    {
        return selectedColor;
    }

    public void ShowGameOver(string winnerMessage)
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        authPanel.style.display = DisplayStyle.None;
        lobbyPanel.style.display = DisplayStyle.None;
        gameOverPanel.style.display = DisplayStyle.Flex;
        winnerText.text = winnerMessage;
    }

    private void OnReturnLobbyClicked()
    {
        gameOverPanel.style.display = DisplayStyle.None;
        ShowLobbyPanel();
        
        // Netegem el joc vell
        GameManager.Instance.CleanCurrentMatch();
    }
}
