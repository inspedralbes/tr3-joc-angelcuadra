using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                if (response.success && response.data != null)
                {
                    Token = response.data.token;
                    CurrentUser = response.data.user;
                }
                onComplete?.Invoke(response.success, response.message);
            }
        }
    }
}
