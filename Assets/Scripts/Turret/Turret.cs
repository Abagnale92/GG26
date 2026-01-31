using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Torretta che spara proiettili dritti a intervalli regolari
    /// </summary>
    public class Turret : MonoBehaviour
    {
        [Header("Shooting Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint; // Punto da cui sparare
        [SerializeField] private float fireRate = 1f; // Colpi al secondo
        [SerializeField] private float projectileSpeed = 10f;
        
        [Header("Direction Settings")]
        [SerializeField] private Vector3 shootDirection = Vector3.forward; // Direzione di sparo
        [SerializeField] private bool normalizeDirection = true;
        
        [Header("Optional Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float startDelay = 0f; // Delay iniziale prima di iniziare a sparare
        
        private float fireTimer;
        private bool isShooting;

        private void Start()
        {
            if (firePoint == null)
            {
                firePoint = transform;
                Debug.LogWarning($"FirePoint non assegnato su {gameObject.name}, uso la posizione della torretta");
            }

            if (normalizeDirection)
            {
                shootDirection = shootDirection.normalized;
            }

            if (autoStart)
            {
                if (startDelay > 0)
                {
                    Invoke(nameof(StartShooting), startDelay);
                }
                else
                {
                    StartShooting();
                }
            }
        }

        private void Update()
        {
            if (!isShooting) return;

            fireTimer += Time.deltaTime;

            if (fireTimer >= 1f / fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
        }

        private void Shoot()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"Projectile Prefab non assegnato su {gameObject.name}");
                return;
            }

            // Crea il proiettile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // Ottieni il componente TurretProjectile
            TurretProjectile projectileScript = projectile.GetComponent<TurretProjectile>();
            
            if (projectileScript != null)
            {
                // Calcola la direzione finale (considera anche la rotazione della torretta se necessario)
                Vector3 finalDirection = transform.TransformDirection(shootDirection);
                
                // Inizializza il proiettile
                projectileScript.Initialize(finalDirection, projectileSpeed);
            }
            else
            {
                Debug.LogError($"Il prefab del proiettile non ha il componente TurretProjectile!");
                Destroy(projectile);
            }
        }

        /// <summary>
        /// Avvia lo sparo
        /// </summary>
        public void StartShooting()
        {
            isShooting = true;
            fireTimer = 0f;
        }

        /// <summary>
        /// Ferma lo sparo
        /// </summary>
        public void StopShooting()
        {
            isShooting = false;
        }

        // Visualizza la direzione di sparo nell'editor
        private void OnDrawGizmos()
        {
            if (firePoint == null) return;

            Gizmos.color = Color.red;
            Vector3 direction = transform.TransformDirection(shootDirection.normalized);
            Gizmos.DrawRay(firePoint.position, direction * 2f);
        }
    }
}
