using UnityEngine;
using UnityEngine.Events;

namespace Puzzles
{
    /// <summary>
    /// Base/piedistallo che riceve oggetti magnetici.
    /// Quando un oggetto con tag "Magnetic" viene posizionato sopra, attiva un trigger.
    /// </summary>
    public class MagneticReceiver : MonoBehaviour
    {
        public enum TriggerAction
        {
            OpenDoor,       // Apre una porta/cancello (animazione o scomparsa)
            DestroyObject,  // Distrugge un oggetto
            ShowObject,     // Fa apparire un oggetto nascosto
            CustomEvent     // Evento personalizzato via UnityEvent
        }

        [Header("Receiver Settings")]
        [SerializeField] private string magneticTag = "Magnetic";
        [Tooltip("Se true, l'oggetto magnetico viene bloccato sulla base")]
        [SerializeField] private bool lockObjectOnReceiver = true;
        [Tooltip("Posizione dove l'oggetto viene bloccato (se null, usa il centro del receiver)")]
        [SerializeField] private Transform snapPoint;

        [Header("Trigger Action")]
        [SerializeField] private TriggerAction action = TriggerAction.OpenDoor;
        [SerializeField] private GameObject targetObject; // L'oggetto su cui agire

        [Header("Door Settings (se action = OpenDoor)")]
        [Tooltip("Se true, usa animazione. Se false, fa scomparire l'oggetto")]
        [SerializeField] private bool useDoorAnimation = false;
        [SerializeField] private string doorAnimationTrigger = "Open";

        [Header("Show Object Settings (se action = ShowObject)")]
        [Tooltip("Se true, usa animazione di apparizione")]
        [SerializeField] private bool useShowAnimation = false;
        [SerializeField] private string showAnimationTrigger = "Show";

        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioClip deactivationSound;

        [Header("Custom Event")]
        [SerializeField] private UnityEvent onActivated;
        [SerializeField] private UnityEvent onDeactivated;

        private bool isActivated = false;
        private GameObject currentMagneticObject;
        private AudioSource audioSource;

        public bool IsActivated => isActivated;

        private void Start()
        {
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (activationSound != null || deactivationSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Se snapPoint non è assegnato, usa questo transform
            if (snapPoint == null)
            {
                snapPoint = transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            if (other.CompareTag(magneticTag))
            {
                ActivateReceiver(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isActivated) return;

            if (other.gameObject == currentMagneticObject)
            {
                DeactivateReceiver();
            }
        }

        private void ActivateReceiver(GameObject magneticObject)
        {
            isActivated = true;
            currentMagneticObject = magneticObject;

            Debug.Log($"{gameObject.name}: Ricevitore attivato da {magneticObject.name}");

            // Blocca l'oggetto sulla base
            if (lockObjectOnReceiver)
            {
                LockObject(magneticObject);
            }

            // Riproduci suono
            PlaySound(activationSound);

            // Esegui l'azione
            ExecuteAction();

            // Invoca evento custom
            onActivated?.Invoke();
        }

        private void DeactivateReceiver()
        {
            if (!isActivated) return;

            isActivated = false;
            currentMagneticObject = null;

            Debug.Log($"{gameObject.name}: Ricevitore disattivato");

            // Riproduci suono
            PlaySound(deactivationSound);

            // Invoca evento custom
            onDeactivated?.Invoke();
        }

        private void LockObject(GameObject obj)
        {
            // Posiziona l'oggetto sullo snap point
            obj.transform.position = snapPoint.position;
            obj.transform.rotation = snapPoint.rotation;

            // Disabilita la fisica
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Disabilita il collider trigger per evitare che venga raccolto di nuovo
            // (opzionale - dipende dal tuo sistema)
        }

        private void ExecuteAction()
        {
            if (targetObject == null)
            {
                Debug.LogWarning($"{gameObject.name}: Nessun target object assegnato!");
                return;
            }

            switch (action)
            {
                case TriggerAction.OpenDoor:
                    OpenDoor();
                    break;

                case TriggerAction.DestroyObject:
                    DestroyTargetObject();
                    break;

                case TriggerAction.ShowObject:
                    ShowTargetObject();
                    break;

                case TriggerAction.CustomEvent:
                    // L'evento custom viene già invocato in onActivated
                    break;
            }
        }

        private void OpenDoor()
        {
            if (useDoorAnimation)
            {
                // Usa animazione
                Animator animator = targetObject.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(doorAnimationTrigger);
                    Debug.Log($"Porta {targetObject.name} aperta con animazione");
                }
                else
                {
                    Debug.LogWarning($"{targetObject.name} non ha un Animator!");
                    // Fallback: disabilita l'oggetto
                    targetObject.SetActive(false);
                }
            }
            else
            {
                // Fa scomparire la porta
                targetObject.SetActive(false);
                Debug.Log($"Porta {targetObject.name} scomparsa");
            }
        }

        private void DestroyTargetObject()
        {
            Debug.Log($"Oggetto {targetObject.name} distrutto");
            Destroy(targetObject);
        }

        private void ShowTargetObject()
        {
            targetObject.SetActive(true);

            if (useShowAnimation)
            {
                Animator animator = targetObject.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(showAnimationTrigger);
                }
            }

            Debug.Log($"Oggetto {targetObject.name} apparso");
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmos()
        {
            // Visualizza il receiver
            Gizmos.color = isActivated ? Color.green : Color.blue;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }

            // Visualizza snap point
            Vector3 snap = snapPoint != null ? snapPoint.position : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(snap, 0.2f);

            // Linea verso il target
            if (targetObject != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetObject.transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Mostra info aggiuntive quando selezionato
            if (targetObject != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawSphere(targetObject.transform.position, 0.5f);
            }
        }
    }
}
