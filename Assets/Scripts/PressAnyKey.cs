using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PressAnyKey : MonoBehaviour
{

    void Update()
    {
        if (Input.anyKeyDown)
        { 
            if(playerStatsManager.Instance != null) playerStatsManager.Instance.ResetAllStats();
            SceneManager.LoadScene("MainMenu");
        }
    }
}