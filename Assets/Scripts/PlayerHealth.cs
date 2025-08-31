using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health & Lives")]
    public int maxHealth = 9;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int maxLives = 3;
    public NetworkVariable<int> currentLives = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Invincibility")]
    public float invincibilityDuration = 3f;
    public float flashInterval = 0.1f;

    [Header("References")]
    public Animator animator;
    public Collider2D playerCollider;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public MonoBehaviour movementScriptMono;

    //private IPlayerInputBlocker inputBlocker;
    private bool isDead = false;
    private bool isInvincible = false;

    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnLivesChanged;
    public int playerId = 1; // Keep for UI/logic

    private void Start()
    {
        //inputBlocker = movementScriptMono as IPlayerInputBlocker;

        if (IsServer && playerStatsManager.Instance != null)
        {
            playerStatsManager.Instance.LoadIntoPlayer(this);
        }

        // Sync initial state
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            currentLives.Value = maxLives;
        }

        // Listen for changes
        currentHealth.OnValueChanged += (oldVal, newVal) => OnHealthChanged?.Invoke(newVal, maxHealth);
        currentLives.OnValueChanged += (oldVal, newVal) => OnLivesChanged?.Invoke(newVal, maxLives);

        OnHealthChanged?.Invoke(currentHealth.Value, maxHealth);
        OnLivesChanged?.Invoke(currentLives.Value, maxLives);
    }

    private void Update()
    {
        // Debugging
        //Debug.Log($"[Networked][PlayerId:{playerId}][IsOwner:{IsOwner}] Health: {currentHealth.Value} / {maxHealth} | Lives: {currentLives.Value} / {maxLives}");
    }

    // ========== DAMAGE ==========
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;

        if (IsServer)
        {
            ApplyDamage(damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        if (!isDead && !isInvincible)
            ApplyDamage(damage);
    }

    private void ApplyDamage(int damage)
    {
        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        if (currentHealth.Value <= 0)
        {
            LoseLife();
        }
        else
        {
            TriggerHurtClientRpc();
        }

        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    [ClientRpc]
    private void TriggerHurtClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    // ========== LIVES ==========
    private void LoseLife()
    {
        currentLives.Value = Mathf.Max(0, currentLives.Value - 1);
        OnLivesChanged?.Invoke(currentLives.Value, maxLives);

        if (currentLives.Value <= 0)
        {
            Die(true);
        }
        else
        {
            StartCoroutine(RespawnAfterDelay(4f));
        }

        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    // ========== DEATH ==========
    private void Die(bool final)
    {

        isDead = true;
        isInvincible = true;
        // if (IsServer && inputBlocker != null)

        // if (IsServer && inputBlocker != null)
        // inputBlocker.isInputBlockedNet.Value = true;

        DieClientRpc(final);
        
        if (currentLives.Value <= 0)
            NetworkManager.Singleton.SceneManager.LoadScene("Lose", LoadSceneMode.Single);

        if (final)
            {
                StartCoroutine(LoadGameOverAfterDeathAnimation());
            }
    }

    [ClientRpc]
    private void DieClientRpc(bool final)
    {
        animator?.SetTrigger("Die");
        // rb.linearVelocity = Vector2.zero;
        // rb.constraints = RigidbodyConstraints2D.FreezeAll;
        // rb.simulated = false;
        // playerCollider.enabled = false;

        //if (inputBlocker != null) inputBlocker.isInputBlocked = true;
        isDead = true;
        isInvincible = true;
    }

    // ========== RESPAWN ==========
    private IEnumerator RespawnAfterDelay(float delay)
    {
        Die(false);
        StartCoroutine(FlashDuringInvincibility());
        yield return new WaitForSeconds(delay);

        Respawn();
    }

    private void Respawn()
    {
        // currentHealth.Value = maxHealth;
        // playerCollider.enabled = true;
        // rb.simulated = true;
        // rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // if (IsServer && inputBlocker != null)
        //     inputBlocker.isInputBlockedNet.Value = false;

        isDead = false;
        isInvincible = false;
        SetHealth(maxHealth);

        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    // ========== HEALING ==========
    public void SetHealth(int newHealth)
    {
        if (IsServer)
        {
            currentHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);
            playerStatsManager.Instance?.SaveFromPlayer(this);
        }
        else
        {
            SetHealthServerRpc(newHealth);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(int newHealth, ServerRpcParams rpcParams = default)
    {
        currentHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);
        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    public void AddHealth(int amount)
    {
        if (amount <= 0 || isDead) return;

        if (IsServer)
        {
            currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
            playerStatsManager.Instance?.SaveFromPlayer(this);
        }
        else
        {
            AddHealthServerRpc(amount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddHealthServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    public void AddLives(int amount)
    {
        if (amount <= 0 || isDead) return;

        if (IsServer)
        {
            currentLives.Value = Mathf.Clamp(currentLives.Value + amount, 0, maxLives);
            playerStatsManager.Instance?.SaveFromPlayer(this);
        }
        else
        {
            AddLivesServerRpc(amount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddLivesServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        currentLives.Value = Mathf.Clamp(currentLives.Value + amount, 0, maxLives);
        playerStatsManager.Instance?.SaveFromPlayer(this);
    }

    // ========== INVINCIBILITY ==========
    private IEnumerator FlashDuringInvincibility()
    {
        isInvincible = true;

        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < invincibilityDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }
            spriteRenderer.enabled = true;
        }

        isInvincible = false;
    }

    private IEnumerator LoadGameOverAfterDeathAnimation()
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Death"))
            yield return null;

        float deathDuration = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(deathDuration);

        NetworkManager.Singleton.SceneManager.LoadScene("Lose", LoadSceneMode.Single);
    }

    // ========== GETTERS ==========
    public bool IsDead() => isDead;
    public bool IsInvincible() => isInvincible;
}
