using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerSpawnerTest : NetworkBehaviour
{
    [Header("Prefabs (index matches CharacterSelectManager characterId)")]
    [SerializeField] private GameObject[] characters;

    [Header("Offline Prefabs (No Netcode Components)")]
    public GameObject shooterPrefabOffline;
    public GameObject meleePrefabOffline;

    [Header("Chunk Manager")]
    public ChunkGenerator chunkGenerator;

    [Header("Cinemachine")]
    public CinemachineCamera cinemachiCamera1;
    public CinemachineCamera cinemachiCamera2;
    public Camera assignedCamera;

    //[Header("Mode")]
    //public bool isOfflineMode = false; // Toggle in inspector for local play
    public static event Action<GameObject[]> OnPlayerUpdated;

    [Header("Spawn Settings")]
    public Vector3 basePosition = new Vector3(-17, 5, 0); // starting point
    public Vector3 spacing = new Vector3(2f, 0, 0);       // offset per player
    public Vector3[] customPositions;                      // optional full list


    private void Start()
    {
        bool isOfflineMode = (GameModeSelector.Instance != null && !GameModeSelector.Instance.IsOnline());

        Debug.Log($"[Spawner] Starting PlayerSpawnerTest. OfflineMode={isOfflineMode}");

        if (isOfflineMode)
        {
            SpawnOfflinePlayers();
        }
        else
        {
            if (!IsServer)
            {
                Debug.Log("[Spawner] Not the server. Online spawning disabled for this client.");
                return;
            }

            // Spawn immediately on level load
            SpawnAllSelectedPlayers();

            // Listen for any future client connections (optional)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            Debug.Log("[Spawner] Server ready. Spawned players and listening for future connections.");
        }
    }

    private void SpawnOfflinePlayers()
{
    Debug.Log("[Spawner] Spawning OFFLINE players as NETWORK OBJECTS (host-only)...");

    if (NetworkManager.Singleton == null)
    {
        Debug.LogError("[Spawner] NetworkManager not found in scene — cannot spawn NetworkObjects.");
        return;
    }

    EnsureHostStartedForOffline();

    var spawnedPlayers = new List<GameObject>();

    // --- Spawn Shooter (Player 1) ---
    var shooterGO = Instantiate(characters[0], new Vector3(-17, 7, 0), Quaternion.identity);
    SpawnAsNetworkOwnedByHost(shooterGO);
    shooterGO.GetComponent<PlayerInput>()?.SwitchCurrentControlScheme("KeyboardLeft", Keyboard.current);
    AssignCameraToPlayerScripts(shooterGO, assignedCamera);
    spawnedPlayers.Add(shooterGO);

    // --- Spawn Melee (Player 2) ---
    var meleeGO = Instantiate(characters[1], new Vector3(-17, 5, 0), Quaternion.identity);
    SpawnAsNetworkOwnedByHost(meleeGO);
    meleeGO.GetComponent<PlayerInput>()?.SwitchCurrentControlScheme("KeyboardRight", Keyboard.current);
    AssignCameraToPlayerScripts(meleeGO, assignedCamera);
    spawnedPlayers.Add(meleeGO);

    // --- Initialize ChunkGenerator like before ---
    if (chunkGenerator != null)
        chunkGenerator.InitializeOfflineChunks(new Transform[] { shooterGO.transform, meleeGO.transform });

    // Mirror the online setup: give boss & shooter device their targets
    AssignBossAndShooterTargets(new Transform[] { shooterGO.transform, meleeGO.transform });

    // Notify listeners
    OnPlayerUpdated?.Invoke(spawnedPlayers.ToArray());
}

private void EnsureHostStartedForOffline()
{
    // If not already listening, start host. This will auto-create a PlayerObject for the host,
    // which we immediately despawn since we spawn our own two characters.
    if (!NetworkManager.Singleton.IsListening)
    {
        Debug.Log("[Spawner] Starting Host for offline mode...");
        var started = NetworkManager.Singleton.StartHost();
        if (!started)
        {
            Debug.LogError("[Spawner] Failed to StartHost().");
            return;
        }

        var autoPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (autoPlayer != null && autoPlayer.IsSpawned)
        {
            Debug.Log("[Spawner] Despawning auto-created host PlayerObject (offline uses custom characters).");
            autoPlayer.Despawn(true);
        }
    }
}

private void SpawnAsNetworkOwnedByHost(GameObject go)
{
    var netObj = go.GetComponent<NetworkObject>();
    if (netObj == null)
    {
        Debug.LogWarning($"[Spawner] Prefab '{go.name}' has no NetworkObject. Adding one at runtime (offline host-only is fine).");
        netObj = go.AddComponent<NetworkObject>();
    }

    // Give ownership to the host (the only client in offline mode)
    netObj.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
}

// Helper method to assign camera to all relevant movement scripts
    private void AssignCameraToPlayerScripts(GameObject player, Camera cam)
    {
        var pm = player.GetComponent<playerMovement>();
        if (pm != null) pm.hobbitCamera = cam;

        var pc = player.GetComponent<PlayerControllerNew>();
        if (pc != null) pc.hobbitCamera = cam;

        var nspm = player.GetComponent<newShooterPlayerMovement>();
        if (nspm != null) nspm.hobbitCamera = cam;
    }

    // ------------------- Online -------------------
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Spawner] Client connected: {clientId}. Spawning all selected players...");
        SpawnAllSelectedPlayers();
    }

    public void SpawnAllSelectedPlayers()
    {
        if (!IsServer) return;
        if (CharacterSelectManager.Instance == null)
        {
            Debug.LogWarning("[Spawner] CharacterSelectManager.Instance is null! Cannot spawn players.");
            return;
        }
        DespawnAllPlayers();
        Debug.Log($"[Spawner] Spawning {CharacterSelectManager.Instance.players.Count} selected players...");

        List<GameObject> spawnedPlayers = new List<GameObject>();

        for (int i = 0; i < CharacterSelectManager.Instance.players.Count; i++)
        {
            var player = CharacterSelectManager.Instance.players[i];

            if (player.characterId < 0 || player.characterId >= characters.Length)
            {
                Debug.LogWarning($"[Spawner] Invalid characterId {player.characterId} for client {player.clientId}");
                continue;
            }

            Vector3 spawnPos = GetSpawnPositionForClient(i);
            var prefabToSpawn = characters[player.characterId];
            Debug.Log($"[Spawner] Instantiating prefab '{prefabToSpawn.name}' for client {player.clientId} at {spawnPos}");

            var playerInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            var netObj = playerInstance.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(player.clientId);
                Debug.Log($"[Spawner] Spawned NetworkObject for client {player.clientId}: {playerInstance.name}");
            }
            else
            {
                Debug.LogWarning($"[Spawner] No NetworkObject found on prefab {prefabToSpawn.name}! It won't be networked.");
            }
            var movementScript = playerInstance.GetComponent<playerMovement>();
            if (movementScript != null)
            {
                movementScript.hobbitCamera = assignedCamera;
            }
            var movementScript2 = playerInstance.GetComponent<PlayerControllerNew>();
            if (movementScript2 != null)
            {
                movementScript2.hobbitCamera = assignedCamera;
            }
            var movementScript3 = playerInstance.GetComponent<newShooterPlayerMovement>();
            if (movementScript3 != null)
            {
                movementScript3.hobbitCamera = assignedCamera;
            }


            spawnedPlayers.Add(playerInstance);
        }

        // Update ChunkGenerator
        if (chunkGenerator != null)
            chunkGenerator.players = GetAllSpawnedPlayerTransforms();

        // Assign Boss + Shooter Device targets
        AssignBossAndShooterTargets(GetAllSpawnedPlayerTransforms());

        // Notify listeners
        OnPlayerUpdated?.Invoke(spawnedPlayers.ToArray());
        Debug.Log($"[Spawner] Finished spawning. Total spawned players: {spawnedPlayers.Count}");

        
    }
    private void DespawnAllPlayers()
    {
        if (!IsServer) return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObj != null && playerObj.IsSpawned)
            {
                Debug.Log($"[Spawner] Despawning old player object for client {clientId}: {playerObj.name}");
                playerObj.Despawn(true); // true = destroy on all clients
            }
        }
    }
    public Vector3 GetSpawnPositionForClient(int index)
    {
        // If custom positions are set and index is valid, use them
        if (customPositions != null && index < customPositions.Length)
            return customPositions[index];

        // Otherwise, calculate using basePosition + spacing * index
        return basePosition + Vector3.Scale(spacing, new Vector3(index, index, index));
    }

    private Transform[] GetAllSpawnedPlayerTransforms()
    {
        var playersList = new List<Transform>();
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObj != null)
            {
                playersList.Add(playerObj.transform);
                Debug.Log($"[Spawner] Added player transform for client {clientId}: {playerObj.name}");
            }
        }
        return playersList.ToArray();
    }
    private void AssignBossAndShooterTargets(Transform[] playerTransforms)
    {
        // Look for BossEnemy
        var boss = GameObject.FindFirstObjectByType<BossEnemy>();


        if (boss != null)
        {
            if (playerTransforms.Length > 0) boss.player1 = playerTransforms[0];
            if (playerTransforms.Length > 1) boss.player2 = playerTransforms[1];
            Debug.Log("[Spawner] Assigned players to BossEnemy.");
        }
        else
        {
            Debug.LogWarning("[Spawner] No BossEnemy found in scene.");
        }

        // Look for BossShooterDevice
        var shooter = GameObject.FindFirstObjectByType<BossShooterDevice>();
        if (shooter != null)
        {
            if (playerTransforms.Length > 0) shooter.player1 = playerTransforms[0];
            if (playerTransforms.Length > 1) shooter.player2 = playerTransforms[1];
            Debug.Log("[Spawner] Assigned players to BossShooterDevice.");
        }
        else
        {
            Debug.LogWarning("[Spawner] No BossShooterDevice found in scene.");
        }
}

}
