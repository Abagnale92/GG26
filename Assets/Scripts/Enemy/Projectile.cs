using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Proiettile sparato dai nemici a distanza.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private bool destroyOnHit = true;

        private Vector3 direction;
        private float speed;
        private bool initialized = false;

        public void Initialize(Vector3 dir, float spd)
        {
            direction = dir.normalized;
            speed = spd;
            initialized = true;

            // Ruota il proiettile nella direzione del movimento
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Distruggi dopo un certo tempo
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!initialized) return;

            // Muovi il proiettile
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignora altri proiettili e nemici
            if (other.CompareTag("Enemy") || other.GetComponent<Projectile>() != null)
            {
                return;
            }

            // Controlla se ha colpito il player
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }

            // Distruggi il proiettile
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
