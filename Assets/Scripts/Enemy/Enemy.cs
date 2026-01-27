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

        public int HitsToKill => hitsToKill;
        public int CurrentHits => currentHits;
        public int RemainingHits => hitsToKill - currentHits;

        private void Awake()
        {
            enemyRenderer = GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                originalColor = enemyRenderer.material.color;
            }

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
            Destroy(gameObject);
        }

        /// <summary>
        /// Resetta i colpi (utile per respawn)
        /// </summary>
        public void ResetHits()
        {
            currentHits = 0;
        }
    }
}
