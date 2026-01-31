using UnityEngine;

namespace Puzzles
{
    /// <summary>
    /// Oggetto magnetico che può essere attratto dalla maschera di Colombina.
    /// Se viene lanciato troppo lontano dalla posizione iniziale, viene respawnato.
    /// </summary>
    public class MagneticObject : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [Tooltip("Distanza massima dalla posizione iniziale prima del respawn")]
        [SerializeField] private float maxDistanceFromStart = 20f;
        [Tooltip("Tempo di attesa prima del respawn (per evitare respawn durante il lancio)")]
        [SerializeField] private float respawnDelay = 1f;

        [Header("Audio")]
        [SerializeField] private AudioClip respawnSound;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody rb;
        private AudioSource audioSource;
        private float outOfRangeTimer = 0f;
        private bool isOutOfRange = false;

        private void Start()
        {
            // Salva posizione e rotazione iniziale
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            // Ottieni riferimenti
            rb = GetComponent<Rigidbody>();

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && respawnSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            // Controlla la distanza dalla posizione iniziale
            float distance = Vector3.Distance(transform.position, initialPosition);

            if (distance > maxDistanceFromStart)
            {
                if (!isOutOfRange)
                {
                    isOutOfRange = true;
                    outOfRangeTimer = 0f;
                }

                outOfRangeTimer += Time.deltaTime;

                // Se è fuori range per troppo tempo, respawna
                if (outOfRangeTimer >= respawnDelay)
                {
                    Respawn();
                }
            }
            else
            {
                // È tornato nel range, resetta il timer
                isOutOfRange = false;
                outOfRangeTimer = 0f;
            }
        }

        /// <summary>
        /// Respawna l'oggetto alla posizione iniziale
        /// </summary>
        public void Respawn()
        {
            Debug.Log($"{gameObject.name} respawnato alla posizione iniziale");

            // Resetta la fisica
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Temporaneamente kinematic per il teletrasporto
            }

            // Teletrasporta alla posizione iniziale
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            // Riattiva la fisica
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Riproduci suono
            if (respawnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(respawnSound);
            }

            // Resetta i timer
            isOutOfRange = false;
            outOfRangeTimer = 0f;
        }

        /// <summary>
        /// Imposta una nuova posizione iniziale (utile se l'oggetto viene posizionato su un receiver)
        /// </summary>
        public void SetNewInitialPosition(Vector3 newPosition, Quaternion newRotation)
        {
            initialPosition = newPosition;
            initialRotation = newRotation;
        }

        private void OnDrawGizmosSelected()
        {
            // Mostra il range massimo
            Vector3 center = Application.isPlaying ? initialPosition : transform.position;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Arancione trasparente
            Gizmos.DrawWireSphere(center, maxDistanceFromStart);

            // Posizione iniziale
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, 0.3f);
        }
    }
}
