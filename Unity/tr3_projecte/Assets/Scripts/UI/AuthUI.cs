using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        joinMatchButton.clicked += OnJoinMatchClicked;
        playAiButton.clicked += OnPlayAiClicked;
        soloModeButton = root.Q<Button>("SoloModeButton");
        soloModeButton.clicked += OnSoloModeClicked;

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
            SocketClient.Instance.OnMatchStarted += HandleMatchStarted;
    }

    private void OnDisable()
    {
        loginButton.clicked -= OnLoginClicked;
        registerButton.clicked -= OnRegisterClicked;

        createMatchButton.clicked -= OnCreateMatchClicked;
        joinMatchButton.clicked -= OnJoinMatchClicked;
        playAiButton.clicked -= OnPlayAiClicked;
        
        if (SocketClient.Instance != null)
            SocketClient.Instance.OnMatchStarted -= HandleMatchStarted;
    }

    private void HandleMatchStarted(string data)
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
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
        
        SocketClient.Instance.Connect();
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
