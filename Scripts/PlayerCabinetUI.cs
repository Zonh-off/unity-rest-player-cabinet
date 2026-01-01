using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCabinetUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI guidTxt;
    [SerializeField] private TextMeshProUGUI responseTxt;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button changeNicknameBtn;
    
    private string currentGuid;
    private string currentUsername;

    private void OnEnable()
    {
        GameEvents.OnPlayerDataReceived += OnPlayerDataReceived;
        GameEvents.OnUsernameSubmittedResponse += OnUsernameSubmittedResponse;
        changeNicknameBtn.onClick.AddListener(OnChangeUsernameClicked);
        usernameInput.onEndEdit.AddListener(OnUsernameSubmitted);
        
        responseTxt.gameObject.SetActive(false);
    }
    
    private void OnDisable()
    {
        GameEvents.OnPlayerDataReceived -= OnPlayerDataReceived;
        GameEvents.OnPlayerDataReceived -= OnPlayerDataReceived;
        GameEvents.OnUsernameSubmittedResponse -= OnUsernameSubmittedResponse;
        changeNicknameBtn.onClick.RemoveListener(OnChangeUsernameClicked);
        usernameInput.onEndEdit.RemoveListener(OnUsernameSubmitted);
    }
    
    private void OnUsernameSubmittedResponse(UsernameResponseType type)
    {
        responseTxt.gameObject.SetActive(true);
    
        switch (type)
        {
            case UsernameResponseType.SUCCESS:
                responseTxt.text = "Username updated successfully.";
                responseTxt.color = Color.green;
                currentUsername = usernameInput.text;
                break;

            case UsernameResponseType.TAKEN:
                responseTxt.text = "Username already taken.";
                responseTxt.color = Color.red;
                break;

            case UsernameResponseType.UNEXPECTED:
                responseTxt.text = "Unexpected error occurred.";
                responseTxt.color = Color.red;
                break;

            case UsernameResponseType.NOT_FOUND:
                responseTxt.text = "User not found.";
                responseTxt.color = Color.red;
                break;

            case UsernameResponseType.ERROR:
                responseTxt.text = "Server error.";
                responseTxt.color = Color.red;
                break;

            default:
                responseTxt.text = "Unknown response.";
                responseTxt.color = Color.red;
                break;
        }
    }

    private void OnPlayerDataReceived(AccountResponse response)
    {
        currentGuid = response.guid;
        currentUsername = response.username;

        guidTxt.text = currentGuid;
        usernameInput.text = currentUsername;
    }

    private void OnUsernameSubmitted(string value)
    {
        IsUsernameValid(value);
    }

    private void OnChangeUsernameClicked()
    {
        IsUsernameValid(usernameInput.text);
    }

    private bool IsUsernameValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ShowResponse("Username cannot be empty.", Color.red);
            return false;
        }

        if (value.Any(char.IsWhiteSpace))
        {
            ShowResponse("Username cannot contain spaces or tabs.", Color.red);
            return false;
        }

        if (value == currentUsername)
        {
            ShowResponse("Username is the same as current.", Color.gray);
            return false;
        }

        if (value.Length <= 3)
        {
            ShowResponse("Username must be more than 3 characters.", Color.yellow);
            return false;
        }

        responseTxt.gameObject.SetActive(false);
        GameEvents.OnUsernameSubmitted?.Invoke(value);
        return true;
    }

    private void ShowResponse(string message, Color color)
    {
        responseTxt.gameObject.SetActive(true);
        responseTxt.text = message;
        responseTxt.color = color;
    }
}