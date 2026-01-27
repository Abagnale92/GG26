using UnityEngine;

namespace Platforms
{
    /// <summary>
    /// Piattaforma mobile che si muove tra due punti.
    /// Il player viene trasportato quando ci sale sopra.
    /// Richiede un BoxCollider trigger posizionato sopra la piattaforma per rilevare il player.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        public enum MoveDirection
        {
            Horizontal,
            Vertical
        }

        [Header("Movement Settings")]
        [SerializeField] private MoveDirection direction = MoveDirection.Horizontal;
        [SerializeField] private float moveDistance = 5f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float waitTime = 0.5f;

        [Header("Start Settings")]
        [SerializeField] private bool startMovingOnAwake = true;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 targetPosition;
        private Vector3 lastPosition;
        private bool isWaiting = false;
        private float waitTimer = 0f;
        private bool isMoving = true;

        private CharacterController playerController;
        private bool playerOnPlatform = false;

        private void Start()
        {
            startPosition = transform.position;

            if (direction == MoveDirection.Horizontal)
            {
                endPosition = startPosition + Vector3.right * moveDistance;
            }
            else
            {
                endPosition = startPosition + Vector3.up * moveDistance;
            }

            targetPosition = endPosition;
            isMoving = startMovingOnAwake;
            lastPosition = transform.position;
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

            // Salva la posizione precedente
            lastPosition = transform.position;

            // Muovi la piattaforma
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Calcola quanto si è mossa la piattaforma
            Vector3 movement = transform.position - lastPosition;

            // Se il player è sulla piattaforma, muovilo insieme
            if (playerOnPlatform && playerController != null && playerController.enabled)
            {
                playerController.Move(movement);
            }

            // Controlla se ha raggiunto il target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                if (targetPosition == endPosition)
                {
                    targetPosition = startPosition;
                }
                else
                {
                    targetPosition = endPosition;
                }

                if (waitTime > 0)
                {
                    isWaiting = true;
                    waitTimer = waitTime;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerController = other.GetComponent<CharacterController>();
                playerOnPlatform = true;
                Debug.Log("Player salito sulla piattaforma");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Mantieni il riferimento anche durante lo stay
            if (other.CompareTag("Player") && !playerOnPlatform)
            {
                playerController = other.GetComponent<CharacterController>();
                playerOnPlatform = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerOnPlatform = false;
                playerController = null;
                Debug.Log("Player sceso dalla piattaforma");
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
            Vector3 end;

            if (direction == MoveDirection.Horizontal)
            {
                end = start + Vector3.right * moveDistance;
            }
            else
            {
                end = start + Vector3.up * moveDistance;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(end, 0.3f);
        }
    }
}
