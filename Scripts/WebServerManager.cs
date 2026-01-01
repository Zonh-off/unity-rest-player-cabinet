using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class WebServerManager : MonoBehaviour
{
    private static WebServerManager _instance;
    public static WebServerManager Instance => _instance ??= new WebServerManager();
    
    private const string AccountGuidKey = nameof(AccountGuidKey);
    private Guid accountGuid;
    private string jwtToken;

#if UNITY_EDITOR
    string url = "https://localhost:7059";
#else
    string url = "https://example.com";
#endif

    private void OnEnable()
    {
        GameEvents.OnUsernameSubmitted += OnUsernameSubmitted;
    }
    
    private void OnDisable()
    {
        GameEvents.OnUsernameSubmitted -= OnUsernameSubmitted;
    }
    
    private void OnUsernameSubmitted(string newUsername)
    {
        StartCoroutine(UpdateUsername(newUsername));
    }

    void Start()
    {
        StartCoroutine(OnStart());
    }

    IEnumerator OnStart()
    {
        // If player was logged in before
        if (PlayerPrefs.HasKey(AccountGuidKey))
        {
            accountGuid = Guid.Parse(PlayerPrefs.GetString(AccountGuidKey));
            Debug.Log($"Loaded existing GUID: {accountGuid}");
        }
        // If player first time
        else
        {
            accountGuid = Guid.NewGuid();
            PlayerPrefs.SetString(AccountGuidKey, accountGuid.ToString());
            Debug.Log($"Generated new GUID: {accountGuid}");
        }
        yield return LoginAndGetToken(accountGuid);
        yield return GetMe();
    }
    
    private void OnApplicationQuit()
    {
        StartCoroutine(UpdateLastActiveOnQuit());
    }
    
    private IEnumerator UpdateLastActiveOnQuit()
    {
        string requestUrl = $"{url}/api/accounts/updateLastActive";

        using (UnityWebRequest www = new UnityWebRequest(requestUrl, "PUT"))
        {
#if UNITY_EDITOR
            www.certificateHandler = new BypassCertificate();
#endif
            www.downloadHandler = new DownloadHandlerBuffer();
        
            if (!string.IsNullOrEmpty(jwtToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
            else
            {
                Debug.LogWarning("JWT Token is missing when updating last active.");
            }

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error updating last active: {www.error}");
            }
            else
            {
                Debug.Log("LastActive updated successfully");
            }
        }
    }

    private IEnumerator LoginAndGetToken(Guid guid)
    {
        string requestUrl = $"{url}/api/accounts/login";
        var requestData = JsonUtility.ToJson(new AccountCreateRequest { guid = guid.ToString() });
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);

        using (UnityWebRequest www = new UnityWebRequest(requestUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

#if UNITY_EDITOR
            www.certificateHandler = new BypassCertificate();
#endif

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Login error: {www.error}");
            }
            else
            {
                var json = www.downloadHandler.text;
                var tokenResponse = JsonUtility.FromJson<TokenResponse>(json);
                jwtToken = tokenResponse.token;
                Debug.Log($"JWT Token received: {jwtToken}");
            }
        }
    }

    private IEnumerator GetMe()
    {
        string requestUrl = $"{url}/api/accounts/me";

        using (UnityWebRequest www = UnityWebRequest.Get(requestUrl))
        {
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
#if UNITY_EDITOR
            www.certificateHandler = new BypassCertificate();
#endif
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error getting account data: {www.error}");
            }
            else
            {
                var json = www.downloadHandler.text;
                var response = JsonUtility.FromJson<AccountResponse>(json);
                Debug.Log($"Account data: {response}");
                GameEvents.OnPlayerDataReceived?.Invoke(response);
            }
        }
    }
    
    private IEnumerator UpdateUsername(string newUsername)
    {
        string requestUrl = $"{url}/api/accounts/updateUsername";
        var requestData = JsonUtility.ToJson(new AccountUsernameUpdateRequest() { username = newUsername});
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);

        using (UnityWebRequest www = new UnityWebRequest(requestUrl, "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

#if UNITY_EDITOR
            www.certificateHandler = new BypassCertificate();
#endif

            yield return www.SendWebRequest();
            
            var responseText = www.downloadHandler.text;
            Debug.Log($"Server response: {responseText}");
            
            UsernameResponseType usernameResponseType;

            switch (www.responseCode)
            {
                case 200:
                    Debug.Log("Username updated successfully.");
                    usernameResponseType = UsernameResponseType.SUCCESS;
                    break;

                case 409:
                    Debug.LogWarning("Username already taken.");
                    usernameResponseType = UsernameResponseType.TAKEN;
                    break;

                case 404:
                    Debug.LogWarning("Player not found.");
                    usernameResponseType = UsernameResponseType.NOT_FOUND;
                    break;

                default:
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Error updating username: {www.error}");
                        usernameResponseType = UsernameResponseType.ERROR;
                    }
                    else
                    {
                        Debug.LogWarning($"Unexpected response: {www.responseCode}");
                        usernameResponseType = UsernameResponseType.UNEXPECTED;
                    }
                    break;
            }

            GameEvents.OnUsernameSubmittedResponse?.Invoke(usernameResponseType);
        }
    }
}

public enum UsernameResponseType
{
    NONE,
    SUCCESS,
    TAKEN,
    UNEXPECTED,
    NOT_FOUND,
    ERROR
}

[Serializable]
public class AccountCreateRequest
{
    public string guid;
}

[Serializable]
public class AccountUsernameUpdateRequest
{
    public string username;
}
    
[Serializable]
public class TokenResponse
{
    public string token;
}

[Serializable]
public class AccountResponse
{
    public string guid;
    public string username;
    public string createdAt;
    public string lastActive;
    
    public override string ToString()
    {
        return $"username: {username}, guid: {guid}, CreatedAt: {createdAt}, LastActive: {lastActive}";
    }
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}