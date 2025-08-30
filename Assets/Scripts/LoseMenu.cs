using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LoseMenu : MonoBehaviour
{
    public TMP_Text[] options; // assign in Inspector
    private int selectedIndex = 0;

    private void Start()
    {
        UpdateHighlight();
    }

    private void Update()
    {
        // Shared input for local co-op (both players can control menu)
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % options.Length;
            UpdateHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
            UpdateHighlight();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmSelection();
        }
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (i == selectedIndex)
                options[i].color = Color.yellow;
            else
                options[i].color = Color.white;
        }
    }

    private void ConfirmSelection()
    {
        if (selectedIndex == 1)
        {
            Debug.Log("Give Up selected - go to main menu"); 
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
        else if (selectedIndex == 0)
        {
            Debug.Log("Restart selected - reload scene");
            playerStatsManager.Instance.ResetAllStats();
            SceneManager.LoadScene("Level1", LoadSceneMode.Single);
        }
    }
}