using UnityEngine;
using System;
using Checkpoints;
using Enemies;

namespace Player
{
    /// <summary>
    /// Gestisce la vita del player, la morte e il respawn.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1f;

        [Header("Visual Feedback")]
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private Color damageColor = Color.red;

        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 1.5f; // Tempo prima del respawn (dopo la morte)

        private int currentHealth;
        private bool isInvincible = false;
        private Renderer playerRenderer;
        private Color originalColor;
        private Animator animator;

        // Animator parameter hash
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int RespawnHash = Animator.StringToHash("Respawn");

        // Riferimento al checkpoint manager
        private CheckpointManager checkpointManager;

        // Riferimento al MaskManager per rimuovere la maschera alla morte
        private MaskManager maskManager;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;

        public event Action<int> OnHealthChanged;
        public event Action OnPlayerDeath;
        public event Action OnPlayerRespawn;

        private void Awake()
        {
            currentHealth = maxHealth;

            playerRenderer = GetComponent<Renderer>();
            if (playerRenderer == null)
            {
                playerRenderer = GetComponentInChildren<Renderer>();
            }

            if (playerRenderer != null)
            {
                originalColor = playerRenderer.material.color;
            }

            // Trova l'Animator
            animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // Trova il checkpoint manager
            checkpointManager = FindFirstObjectByType<CheckpointManager>();

            // Trova il MaskManager
            maskManager = GetComponent<MaskManager>();
        }

        /// <summary>
        /// Infligge danno al player
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isInvincible || !IsAlive) return;

            currentHealth -= Mathf.RoundToInt(damage);
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"Player colpito! Vita: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);

            // Flash visivo
            if (playerRenderer != null)
            {
                StartCoroutine(DamageFlash());
            }

            // Invincibilità temporanea
            StartCoroutine(InvincibilityFrames());

            // Controlla morte
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Uccide istantaneamente il player (per zone letali)
        /// </summary>
        public void InstantKill()
        {
            if (!IsAlive) return;

            currentHealth = 0;
            OnHealthChanged?.Invoke(currentHealth);
            Die();
        }

        private void Die()
        {
            Debug.Log("Player morto!");
            OnPlayerDeath?.Invoke();

            // Avvia animazione di morte
            if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }

            // Rimuovi la maschera visivamente
            if (maskManager != null)
            {
                maskManager.RemoveMask();
            }

            // Respawn dopo il delay configurato
            StartCoroutine(RespawnAfterDelay(respawnDelay));
        }

        /// <summary>
        /// Resetta tutti i nemici vivi alla posizione iniziale
        /// </summary>
        private void ResetAllEnemies()
        {
            // Trova tutti i nemici nella scena e resettali
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ResetToInitialPosition();
                }
            }
        }

        private System.Collections.IEnumerator RespawnAfterDelay(float delay)
        {
            // Disabilita il movimento durante il respawn
            var controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            yield return new WaitForSeconds(delay);

            Respawn();
        }

        private void Respawn()
        {
            // Ripristina la vita
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth);

            // Teletrasporta al checkpoint
            if (checkpointManager != null)
            {
                Vector3 respawnPos = checkpointManager.GetRespawnPosition();
                transform.position = respawnPos;
            }

            // Riabilita il controller
            var controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = true;
            }

            // Trigger animazione respawn (torna a Idle)
            if (animator != null)
            {
                animator.SetTrigger(RespawnHash);
            }

            // Resetta i nemici DOPO il respawn del player
            ResetAllEnemies();

            Debug.Log("Player respawnato!");
            OnPlayerRespawn?.Invoke();
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            if (playerRenderer != null)
            {
                playerRenderer.material.color = damageColor;
                yield return new WaitForSeconds(flashDuration);

                if (playerRenderer != null)
                {
                    playerRenderer.material.color = originalColor;
                }
            }
        }

        private System.Collections.IEnumerator InvincibilityFrames()
        {
            isInvincible = true;

            // Effetto lampeggio durante invincibilità
            float elapsed = 0f;
            while (elapsed < invincibilityDuration)
            {
                if (playerRenderer != null)
                {
                    playerRenderer.enabled = !playerRenderer.enabled;
                }
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (playerRenderer != null)
            {
                playerRenderer.enabled = true;
            }

            isInvincible = false;
        }

        /// <summary>
        /// Cura il player
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }
    }
}
