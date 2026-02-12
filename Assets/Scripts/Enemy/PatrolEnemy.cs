using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Nemico che pattuglia muovendosi tra due punti.
    /// Danneggia il player al contatto.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PatrolEnemy : MonoBehaviour
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

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string walkBoolParameter = "IsWalking";

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 targetPosition;
        private bool isWaiting = false;
        private float waitTimer = 0f;
        private bool isMoving = true;
        private bool movingToEnd = true;
        private AudioSource audioSource;

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

            // Cerca animator se non assegnato
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && hitSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Imposta animazione iniziale
            UpdateAnimation();
        }

        private void Update()
        {
            if (!isMoving)
            {
                UpdateAnimation();
                return;
            }

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                }
                UpdateAnimation();
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

            UpdateAnimation();
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

        private void UpdateAnimation()
        {
            if (animator != null && !string.IsNullOrEmpty(walkBoolParameter))
            {
                bool shouldWalk = isMoving && !isWaiting;
                animator.SetBool(walkBoolParameter, shouldWalk);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                DamagePlayer(other.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                DamagePlayer(collision.gameObject);
            }
        }

        private void DamagePlayer(GameObject playerObj)
        {
            PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageToPlayer);
                PlaySound(hitSound);
                Debug.Log($"{gameObject.name} ha colpito il player!");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Inizia a muoversi
        /// </summary>
        public void StartMoving()
        {
            isMoving = true;
        }

        /// <summary>
        /// Ferma il movimento
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
        }

        /// <summary>
        /// Imposta la velocità di movimento
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
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
            Gizmos.color = Color.red;
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
