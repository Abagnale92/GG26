using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Nemico a distanza che sta fermo e spara proiettili al player.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyRanged : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRange = 12f;

        [Header("Attack")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 2f; // Secondi tra uno sparo e l'altro
        [SerializeField] private float projectileSpeed = 8f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private bool lookAtPlayer = true;

        private Transform player;
        private float lastFireTime;

        private void Start()
        {
            // Trova il player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }

            // Se non c'è un fire point, usa la posizione del nemico
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Controlla se il player è nel range
            if (distanceToPlayer <= detectionRange)
            {
                // Guarda verso il player
                if (lookAtPlayer)
                {
                    LookAtPlayer();
                }

                // Spara
                TryFire();
            }
        }

        private void LookAtPlayer()
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void TryFire()
        {
            if (Time.time - lastFireTime >= fireRate)
            {
                lastFireTime = Time.time;
                Fire();
            }
        }

        private void Fire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{gameObject.name}: Nessun prefab proiettile assegnato!");
                return;
            }

            // Crea il proiettile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            // Direzione verso il player
            Vector3 direction = (player.position - firePoint.position).normalized;

            // Imposta la velocità del proiettile
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                projScript.Initialize(direction, projectileSpeed);
            }
            else
            {
                // Fallback: usa Rigidbody se non c'è lo script Projectile
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * projectileSpeed;
                }
            }

            Debug.Log($"{gameObject.name} spara!");
        }

        private void OnDrawGizmosSelected()
        {
            // Range di detection
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Fire point
            if (firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(firePoint.position, 0.2f);
            }
        }
    }
}
