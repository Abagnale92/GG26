using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Proiettile sparato dalla torretta che si muove dritto e fa danno al player
    /// </summary>
    public class TurretProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float lifetime = 5f; // Secondi prima di autodistruggersi
        
        [Header("Optional Visual")]
        [SerializeField] private GameObject hitEffectPrefab; // Effetto particellare opzionale
        
        private Vector3 direction;
        private float speed;
        private bool isInitialized = false;

        /// <summary>
        /// Inizializza il proiettile con direzione e velocità
        /// </summary>
        public void Initialize(Vector3 shootDirection, float projectileSpeed)
        {
            direction = shootDirection.normalized;
            speed = projectileSpeed;
            isInitialized = true;

            // Autodistruzione dopo il tempo specificato
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Movimento dritto (NO curva, NO gravità)
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Controlla se ha colpito il player
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                // Infliggi danno
                playerHealth.TakeDamage(damage);
                
                Debug.Log($"Proiettile ha colpito il player! Danno: {damage}");
                
                // Distruggi il proiettile
                DestroyProjectile(other.transform.position);
                return;
            }

            // Controlla se ha colpito un muro/ostacolo
            if (other.CompareTag("Wall") || other.CompareTag("Ground") || other.CompareTag("Obstacle"))
            {
                DestroyProjectile(other.transform.position);
            }
        }

        private void DestroyProjectile(Vector3 hitPosition)
        {
            // Spawn effetto visivo opzionale
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
                Destroy(effect, 2f); // Distruggi l'effetto dopo 2 secondi
            }

            Destroy(gameObject);
        }

        // Visualizza la direzione del proiettile nell'editor (debug)
        private void OnDrawGizmos()
        {
            if (isInitialized)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, direction * 0.5f);
            }
        }
    }
}
