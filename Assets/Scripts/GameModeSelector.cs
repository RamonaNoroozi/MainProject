using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Diagnostics; // for Process.Start
using System.IO;

public class GameModeSelector : MonoBehaviour
{
    public static GameModeSelector Instance;

    [SerializeField] private bool isOnline = false; // false = offline/local, true = online

    // Path to your offline build executable
    [SerializeField] private string offlineBuildPath = "OfflineGame.exe"; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChooseLocalCoop()
    {
        UnityEngine.Debug.Log("Launching Offline Build...");

        isOnline = false;

        string fullPath = Path.Combine(Application.dataPath, "..", offlineBuildPath);
        fullPath = Path.GetFullPath(fullPath);

        if (File.Exists(fullPath))
        {
            Process.Start(fullPath);   // launch offline build
            Application.Quit();        // close current build
        }
        else
        {
            //Debug.LogError("Offline build not found at: " + fullPath);
        }
    }

    public void HostGame()
    {

    //Debug.Log("Hosting Online Game...");
        isOnline = true;

        if (!NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }

        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        //.Log("Joining Online Game...");
        isOnline = true;

        if (!NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void BackButton()
    {
        SceneManager.LoadScene("Signup");
    }

    public bool IsOnline()
    {
        return isOnline;
    }
}