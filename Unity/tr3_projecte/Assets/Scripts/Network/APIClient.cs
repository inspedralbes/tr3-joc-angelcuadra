using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class AuthRequest
{
    public string username;
    public string password;
}

[Serializable]
public class AuthResponse
{
    public bool success;
    public string message;
    public UserData data;
}

[Serializable]
public class UserData
{
    public string token;
    public UserInfo user;
}

[Serializable]
public class UserInfo
{
    public string id;
    public string username;
    public int gamesPlayed;
    public int wins;
    public int losses;
    public int coinsCollected;
}

public class APIClient : MonoBehaviour
{
    public static APIClient Instance { get; private set; }
    
    private const string API_URL = "http://204.168.213.113:3000/api/users";
    
    [HideInInspector]
    public string Token;
    [HideInInspector]
    public UserInfo CurrentUser;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(string username, string password, Action<bool, string> onComplete)
    {
        StartCoroutine(AuthRoutine("/register", username, password, onComplete));
    }

    public void Login(string username, string password, Action<bool, string> onComplete)
    {
        StartCoroutine(AuthRoutine("/login", username, password, onComplete));
    }

    private IEnumerator AuthRoutine(string endpoint, string username, string password, Action<bool, string> onComplete)
    {
        AuthRequest reqData = new AuthRequest { username = username, password = password };
        string json = JsonUtility.ToJson(reqData);

        using (UnityWebRequest www = new UnityWebRequest(API_URL + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, www.error);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);
                if (response != null && response.success && response.data != null)
                {
                    Token = response.data.token;
                    CurrentUser = response.data.user;
                }
                onComplete?.Invoke(response != null && response.success, response?.message);
            }
        }
    }

    public void GetProfile(Action<bool> onComplete)
    {
        StartCoroutine(GetProfileRoutine(onComplete));
    }

    private IEnumerator GetProfileRoutine(Action<bool> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(API_URL + "/profile"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + Token);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error GetProfile: " + www.error);
                onComplete?.Invoke(false);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);
                if (response != null && response.success && response.data != null)
                {
                    CurrentUser = response.data.user;
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }
        }
    }
    
    public void AddCoins(int amount, Action<bool> onComplete)
    {
        StartCoroutine(AddCoinsRoutine(amount, onComplete));
    }

    private IEnumerator AddCoinsRoutine(int amount, Action<bool> onComplete)
    {
        var body = new { amount = amount };
        string json = JsonConvert.SerializeObject(body);
        
        using (UnityWebRequest www = new UnityWebRequest(API_URL + "/add-coins", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + Token);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error AddCoins: " + www.error);
                onComplete?.Invoke(false);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);
                if (response != null && response.success && response.data != null)
                {
                    CurrentUser = response.data.user;
                    Debug.Log("CurrentUser actualitzat després de AddCoins: " + CurrentUser.coinsCollected);
                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }
        }
    }
}
