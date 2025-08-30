using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField email;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private TMP_Text errorText;

    public void Signup()
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email.text,
            Password = password.text,
            RequireBothUsernameAndEmail = false
        };
        
        PlayFabClientAPI.RegisterPlayFabUser(request, OnSignupSuccess, OnError);
    }

    public void Signin()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email.text,
            Password = password.text,
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }
    
    public void RecoverPassword()
    {
        var request = new SendAccountRecoveryEmailRequest()
        {
            Email = email.text,
            TitleId = "1C83CE",
        };
        
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnRecoverySuccess, OnError);
    }
    
    void OnSignupSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("Signup success");
        SceneManager.LoadScene("MainMenu");
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login success");
        SceneManager.LoadScene("MainMenu+");
    }

    void OnRecoverySuccess(SendAccountRecoveryEmailResult result)
    {
        Debug.Log("Email sent");
    }
    void OnError(PlayFabError error)
    {
        errorText.text = error.GenerateErrorReport();
    }
    
}
