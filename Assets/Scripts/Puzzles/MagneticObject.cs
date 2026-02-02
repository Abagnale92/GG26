using UnityEngine;

namespace Puzzles
{
    /// <summary>
    /// Oggetto magnetico che può essere attratto dalla maschera di Colombina.
    /// Se viene lanciato troppo lontano dalla posizione iniziale, viene respawnato.
    /// Supporta identificazione univoca per i MagneticReceiver.
    /// </summary>
    public class MagneticObject : MonoBehaviour
    {
        [Header("Identification")]
        [Tooltip("ID univoco dell'oggetto (es. 'chiave_rossa', 'gemma_blu', 'cubo_1')")]
        [SerializeField] private string objectID = "default";
        [Tooltip("Nome visualizzato per l'oggetto (opzionale, per UI)")]
        [SerializeField] private string displayName = "";
        [Tooltip("Icona dell'oggetto (opzionale, per UI)")]
        [SerializeField] private Sprite icon;

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

        // Stati dell'oggetto
        private bool isBeingAttracted = false; // È attratto dal player (Colombina)
        private bool isOnReceiver = false; // È posizionato su un receiver
        private bool wasThrown = false; // È stato lanciato

        /// <summary>
        /// ID univoco dell'oggetto usato per l'identificazione nei MagneticReceiver
        /// </summary>
        public string ObjectID => objectID;

        /// <summary>
        /// Nome visualizzato dell'oggetto
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? objectID : displayName;

        /// <summary>
        /// Icona dell'oggetto
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Imposta l'ID dell'oggetto via codice
        /// </summary>
        public void SetObjectID(string newID)
        {
            objectID = newID;
        }

        /// <summary>
        /// Indica se l'oggetto è attualmente attratto dal player
        /// </summary>
        public bool IsBeingAttracted => isBeingAttracted;

        /// <summary>
        /// Indica se l'oggetto è posizionato su un receiver
        /// </summary>
        public bool IsOnReceiver => isOnReceiver;

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
            // Non fare respawn se:
            // - È attratto dal player (Colombina attiva)
            // - È posizionato su un receiver
            // - Non è stato ancora lanciato
            if (isBeingAttracted || isOnReceiver || !wasThrown)
            {
                // Resetta il timer se non siamo in condizioni di respawn
                isOutOfRange = false;
                outOfRangeTimer = 0f;
                return;
            }

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

        /// <summary>
        /// Chiamato quando l'oggetto viene attratto dal player (Colombina)
        /// </summary>
        public void SetBeingAttracted(bool attracted)
        {
            isBeingAttracted = attracted;

            // Se viene attratto, non è più "lanciato"
            if (attracted)
            {
                wasThrown = false;
            }
        }

        /// <summary>
        /// Chiamato quando l'oggetto viene lanciato
        /// </summary>
        public void SetThrown()
        {
            wasThrown = true;
            isBeingAttracted = false;
        }

        /// <summary>
        /// Chiamato quando l'oggetto viene posizionato su un receiver
        /// </summary>
        public void SetOnReceiver(bool onReceiver)
        {
            isOnReceiver = onReceiver;

            // Se è su un receiver, non è più "lanciato"
            if (onReceiver)
            {
                wasThrown = false;
            }
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

            // Mostra l'ID sopra l'oggetto nell'editor
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"ID: {objectID}");
#endif
        }
    }
}
