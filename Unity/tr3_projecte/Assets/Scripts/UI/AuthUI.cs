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

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Referències als panells
        authPanel = root.Q<VisualElement>("AuthPanel");
        lobbyPanel = root.Q<VisualElement>("LobbyPanel");

        // Referències als inputs i botons
        usernameInput = root.Q<TextField>("UsernameInput");
        passwordInput = root.Q<TextField>("PasswordInput");
        loginButton = root.Q<Button>("LoginButton");
        registerButton = root.Q<Button>("RegisterButton");
        statusText = root.Q<Label>("StatusText");

        // Afegim els esdeveniments
        loginButton.clicked += OnLoginClicked;
        registerButton.clicked += OnRegisterClicked;

        // Mostrar panell d'autenticació per defecte
        ShowAuthPanel();
    }

    private void OnDisable()
    {
        // És bona pràctica desvincular els esdeveniments
        loginButton.clicked -= OnLoginClicked;
        registerButton.clicked -= OnRegisterClicked;
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
        
        // Un cop autenticats, connectem els WebSockets
        SocketClient.Instance.Connect();
    }
}
