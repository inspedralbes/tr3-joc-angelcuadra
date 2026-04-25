using UnityEngine;
using UnityEngine.UIElements;

public class AuthUI : MonoBehaviour
{
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
    private Label lobbyStatusText;

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
        lobbyStatusText.text = "Creant partida...";
        SocketClient.Instance.CreateMatch("Sala Tron");
        lobbyStatusText.text = "Partida creada! Esperant un rival...";
    }

    private void OnJoinMatchClicked()
    {
        lobbyStatusText.text = "Unint-se a la partida...";
        SocketClient.Instance.JoinMatch(1);
    }

    private void OnPlayAiClicked()
    {
        lobbyStatusText.text = "Això ho programarem a la Fase 5!";
    }
}
