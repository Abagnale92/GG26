using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Nemico base che muore dopo un certo numero di colpi.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int hitsToKill = 3;

        [Header("Visual Feedback")]
        [SerializeField] private float hitFlashDuration = 0.1f;
        [SerializeField] private Color hitColor = Color.red;

        private int currentHits = 0;
        private Renderer enemyRenderer;
        private Color originalColor;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isDead = false;

        public int HitsToKill => hitsToKill;
        public int CurrentHits => currentHits;
        public int RemainingHits => hitsToKill - currentHits;
        public bool IsDead => isDead;

        private void Awake()
        {
            // Cerca il Renderer anche nei figli (per modelli FBX con SkinnedMeshRenderer)
            enemyRenderer = GetComponentInChildren<Renderer>();
            if (enemyRenderer != null)
            {
                originalColor = enemyRenderer.material.color;
            }

            // Salva la posizione e rotazione iniziale
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            // Configura il Rigidbody per evitare che il nemico cada o si inclini
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
        }

        /// <summary>
        /// Chiamato quando il nemico viene colpito dalla spada
        /// </summary>
        public void TakeHit()
        {
            if (isDead) return;

            currentHits++;
            Debug.Log($"{gameObject.name} colpito! Colpi: {currentHits}/{hitsToKill}");

            // Flash rosso
            if (enemyRenderer != null)
            {
                StartCoroutine(HitFlash());
            }

            // Controlla se deve morire
            if (currentHits >= hitsToKill)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator HitFlash()
        {
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = hitColor;
                yield return new WaitForSeconds(hitFlashDuration);

                // Controlla che l'oggetto esista ancora
                if (enemyRenderer != null)
                {
                    enemyRenderer.material.color = originalColor;
                }
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} sconfitto!");
            isDead = true;

            // Disabilita il nemico invece di distruggerlo
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Resetta i colpi (utile per respawn)
        /// </summary>
        public void ResetHits()
        {
            currentHits = 0;
        }

        /// <summary>
        /// Resetta il nemico alla posizione iniziale (se non è morto)
        /// </summary>
        public void ResetToInitialPosition()
        {
            // Se il nemico è morto, non fare nulla (resta morto)
            if (isDead) return;

            // Resetta posizione e rotazione
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            // Resetta i colpi subiti
            currentHits = 0;

            // Resetta il colore se necessario
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = originalColor;
            }

            Debug.Log($"{gameObject.name} resettato alla posizione iniziale");
        }
    }
}
