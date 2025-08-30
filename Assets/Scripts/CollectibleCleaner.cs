using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CollectibleCleaner : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            foreach (var collectible in FindObjectsOfType<Collectibles>())
            {
                collectible.GetComponent<NetworkObject>()?.Despawn();
            }
        }
    }
}