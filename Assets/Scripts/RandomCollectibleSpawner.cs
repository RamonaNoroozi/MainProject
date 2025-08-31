using UnityEngine;
using System.Collections.Generic;

public class RandomCollectibleSpawner : MonoBehaviour
{
    [Header("Collectibles")]
    public GameObject[] collectibles; // Drag collectible prefabs here

    [Header("Spawn Settings")]
    public int collectiblesToSpawn = 3;
    public Transform[] spawnPoints;

    void Start()
    {
        SpawnCollectiblesRandomly();
    }

    void SpawnCollectiblesRandomly()
    {
        if (collectibles.Length == 0 || spawnPoints.Length == 0) return;

        // Shuffle collectibles
        List<GameObject> shuffledCollectibles = new List<GameObject>(collectibles);
        Shuffle(shuffledCollectibles);

        // Shuffle spawn points
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        Shuffle(availablePoints);

        int spawned = 0;

        for (int i = 0; i < shuffledCollectibles.Count && spawned < collectiblesToSpawn && i < availablePoints.Count; i++)
        {
            GameObject prefab = shuffledCollectibles[i];
            Transform spawnPoint = availablePoints[i];

            // ✅ Check if this collectible already saved as collected
            UniqueID uid = prefab.GetComponent<UniqueID>();
            if (uid != null && SaveTracker.Instance != null && SaveTracker.Instance.IsCollected(uid.id))
            {
                // Skip spawning if it was already collected
                continue;
            }

            // Spawn collectible
            GameObject spawnedObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

            // ✅ Re-check in case the spawned object gets a UniqueID
            UniqueID spawnedUID = spawnedObj.GetComponent<UniqueID>();
            if (spawnedUID != null && SaveTracker.Instance != null && SaveTracker.Instance.IsCollected(spawnedUID.id))
            {
                // Immediately disable if already marked as collected
                spawnedObj.SetActive(false);
            }

            spawned++;
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
