using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectDisplay : MonoBehaviour
{
    [SerializeField] private PlayCard[] playerCards;

    private void OnEnable()
    {
        if (CharacterSelectManager.Instance != null)
        {
            CharacterSelectManager.Instance.players.OnListChanged += HandlePlayerStateChange;
            HandlePlayerStateChange(default); // initial sync
        }
    }

    private void OnDisable()
    {
        if (CharacterSelectManager.Instance != null)
            CharacterSelectManager.Instance.players.OnListChanged -= HandlePlayerStateChange;
    }

    private void HandlePlayerStateChange(NetworkListEvent<CharacterSelection> e)
    {
        var players = CharacterSelectManager.Instance.players;

        // update active players
        for (int i = 0; i < players.Count; i++)
            playerCards[i].updateDisplay(players[i]);

        // disable unused slots
        for (int i = players.Count; i < playerCards.Length; i++)
            playerCards[i].disableDisplay();
    }

    public void Select(int characterId)
    {
        CharacterSelectManager.Instance.SelectServerRpc(characterId);
    }
    
    public void BackButton()
    {
        SceneManager.LoadScene("OnlineOrLocal");
    }

    public void StartButton()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Only host can start the game.");
            return;
        }

        foreach (var player in CharacterSelectManager.Instance.players)
        {
            if (player.characterId == -1)
            {
                Debug.Log("Not all players have chosen a character!");
                return;
            }
        }

        Debug.Log("Loading MainMenu for all players...");
        NetworkManager.Singleton.SceneManager.LoadScene("Level1",LoadSceneMode.Single);
    }
}