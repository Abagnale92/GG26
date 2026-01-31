using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Nemico invisibile che si muove tra due punti.
    /// Visibile solo con la maschera di Pulcinella.
    /// Danneggia il player al contatto.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InvisibleEnemy : MonoBehaviour
    {
        public enum MoveDirection
        {
            Horizontal,  // Destra/Sinistra (asse X)
            Forward      // Avanti/Indietro (asse Z)
        }

        [Header("Movement Settings")]
        [SerializeField] private MoveDirection direction = MoveDirection.Horizontal;
        [Tooltip("Se attivo, inverte la direzione (Sinistra invece di Destra, Indietro invece di Avanti)")]
        [SerializeField] private bool invertDirection = false;
        [SerializeField] private float moveDistance = 5f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float waitTime = 0.5f;
        [SerializeField] private bool startMovingOnAwake = true;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Damage Settings")]
        [SerializeField] private int damageToPlayer = 1;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 targetPosition;
        private bool isWaiting = false;
        private float waitTimer = 0f;
        private bool isMoving = true;
        private Renderer enemyRenderer;
        private bool movingToEnd = true;

        private void Start()
        {
            startPosition = transform.position;

            // Calcola il vettore direzione in base alle impostazioni
            Vector3 moveVector;
            if (direction == MoveDirection.Horizontal)
            {
                // Destra o Sinistra
                moveVector = invertDirection ? Vector3.left : Vector3.right;
            }
            else
            {
                // Avanti o Indietro
                moveVector = invertDirection ? Vector3.back : Vector3.forward;
            }

            endPosition = startPosition + moveVector * moveDistance;

            targetPosition = endPosition;
            movingToEnd = true;
            isMoving = startMovingOnAwake;

            // Ottieni il renderer (anche nei figli per modelli FBX)
            enemyRenderer = GetComponentInChildren<Renderer>();

            // Nascondi all'avvio (verrà mostrato dalla maschera di Pulcinella)
            SetVisible(false);
        }

        private void Update()
        {
            if (!isMoving) return;

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                }
                return;
            }

            // Muovi il nemico
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Ruota verso la direzione di movimento
            RotateTowardsMovement();

            // Controlla se ha raggiunto il target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                if (movingToEnd)
                {
                    targetPosition = startPosition;
                    movingToEnd = false;
                }
                else
                {
                    targetPosition = endPosition;
                    movingToEnd = true;
                }

                if (waitTime > 0)
                {
                    isWaiting = true;
                    waitTimer = waitTime;
                }
            }
        }

        private void RotateTowardsMovement()
        {
            // Calcola la direzione di movimento
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            if (moveDirection != Vector3.zero)
            {
                // Crea la rotazione target
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

                // Ruota gradualmente verso il target
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Danneggia il player
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageToPlayer);
                    Debug.Log($"{gameObject.name} ha colpito il player!");
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                // Danneggia il player
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageToPlayer);
                    Debug.Log($"{gameObject.name} ha colpito il player!");
                }
            }
        }

        /// <summary>
        /// Mostra o nasconde il nemico (chiamato dalla maschera di Pulcinella)
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (enemyRenderer != null)
            {
                enemyRenderer.enabled = visible;
            }
        }

        public void StartMoving()
        {
            isMoving = true;
        }

        public void StopMoving()
        {
            isMoving = false;
        }

        private void OnDrawGizmos()
        {
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Vector3 moveVector;

            if (direction == MoveDirection.Horizontal)
            {
                moveVector = invertDirection ? Vector3.left : Vector3.right;
            }
            else
            {
                moveVector = invertDirection ? Vector3.back : Vector3.forward;
            }

            Vector3 end = start + moveVector * moveDistance;

            // Linea del percorso
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(end, 0.3f);

            // Punto centrale per indicare la direzione
            Gizmos.color = Color.yellow;
            Vector3 midPoint = (start + end) / 2f;
            Gizmos.DrawSphere(midPoint, 0.15f);
        }
    }
}
