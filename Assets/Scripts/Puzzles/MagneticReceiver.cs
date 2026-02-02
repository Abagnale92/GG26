using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Puzzles
{
    /// <summary>
    /// Base/piedistallo che riceve oggetti magnetici.
    /// Quando un oggetto con tag "Magnetic" viene posizionato sopra, attiva un trigger.
    /// Supporta richiesta di oggetti specifici e multipli.
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

        public enum IdentificationType
        {
            TagOnly,            // Qualsiasi oggetto con il tag magnetico
            SpecificNames,      // Solo oggetti con nomi specifici
            SpecificIDs         // Solo oggetti con ID specifici (componente MagneticObject)
        }

        [Header("Receiver Settings")]
        [SerializeField] private string magneticTag = "Magnetic";
        [Tooltip("Se true, l'oggetto magnetico viene bloccato sulla base")]
        [SerializeField] private bool lockObjectOnReceiver = true;
        [Tooltip("Posizione dove l'oggetto viene bloccato (se null, usa il centro del receiver)")]
        [SerializeField] private Transform snapPoint;

        [Header("Object Identification")]
        [SerializeField] private IdentificationType identificationType = IdentificationType.TagOnly;
        [Tooltip("Lista dei nomi degli oggetti accettati (se IdentificationType = SpecificNames)")]
        [SerializeField] private List<string> acceptedObjectNames = new List<string>();
        [Tooltip("Lista degli ID degli oggetti accettati (se IdentificationType = SpecificIDs)")]
        [SerializeField] private List<string> acceptedObjectIDs = new List<string>();

        [Header("Multi-Object Settings")]
        [Tooltip("Numero di oggetti richiesti per attivare il receiver")]
        [SerializeField] private int requiredObjectCount = 1;
        [Tooltip("Se true, ogni oggetto deve essere su uno snap point diverso")]
        [SerializeField] private bool useMultipleSnapPoints = false;
        [Tooltip("Snap points aggiuntivi per oggetti multipli")]
        [SerializeField] private List<Transform> additionalSnapPoints = new List<Transform>();

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
        [SerializeField] private UnityEvent onWrongObject; // Quando l'oggetto sbagliato viene inserito
        [SerializeField] private UnityEvent onPartialProgress; // Quando un oggetto corretto viene inserito ma ne servono altri

        private bool isActivated = false;
        private List<GameObject> currentMagneticObjects = new List<GameObject>();
        private AudioSource audioSource;

        public bool IsActivated => isActivated;
        public int CurrentObjectCount => currentMagneticObjects.Count;
        public int RequiredObjectCount => requiredObjectCount;

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
                // Verifica se l'oggetto è accettato
                if (!IsObjectAccepted(other.gameObject))
                {
                    Debug.Log($"{gameObject.name}: Oggetto '{other.gameObject.name}' non accettato!");
                    onWrongObject?.Invoke();
                    return;
                }

                // Verifica se l'oggetto è già nel receiver
                if (currentMagneticObjects.Contains(other.gameObject))
                {
                    return;
                }

                AddObject(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentMagneticObjects.Contains(other.gameObject))
            {
                RemoveObject(other.gameObject);
            }
        }

        /// <summary>
        /// Verifica se l'oggetto è accettato dal receiver
        /// </summary>
        private bool IsObjectAccepted(GameObject obj)
        {
            switch (identificationType)
            {
                case IdentificationType.TagOnly:
                    // Qualsiasi oggetto con il tag è accettato
                    return true;

                case IdentificationType.SpecificNames:
                    // Controlla se il nome dell'oggetto è nella lista
                    foreach (string acceptedName in acceptedObjectNames)
                    {
                        if (obj.name.Contains(acceptedName) || obj.name == acceptedName)
                        {
                            return true;
                        }
                    }
                    return false;

                case IdentificationType.SpecificIDs:
                    // Controlla se l'oggetto ha il componente MagneticObject con l'ID corretto
                    MagneticObject magneticObj = obj.GetComponent<MagneticObject>();
                    if (magneticObj != null)
                    {
                        return acceptedObjectIDs.Contains(magneticObj.ObjectID);
                    }
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Aggiunge un oggetto al receiver
        /// </summary>
        private void AddObject(GameObject magneticObject)
        {
            currentMagneticObjects.Add(magneticObject);

            Debug.Log($"{gameObject.name}: Oggetto '{magneticObject.name}' aggiunto ({currentMagneticObjects.Count}/{requiredObjectCount})");

            // Notifica l'oggetto che è su un receiver
            MagneticObject magObj = magneticObject.GetComponent<MagneticObject>();
            if (magObj != null)
            {
                magObj.SetOnReceiver(true);
            }

            // Blocca l'oggetto sulla base
            if (lockObjectOnReceiver)
            {
                LockObject(magneticObject, currentMagneticObjects.Count - 1);
            }

            // Controlla se abbiamo raggiunto il numero richiesto
            if (currentMagneticObjects.Count >= requiredObjectCount)
            {
                ActivateReceiver();
            }
            else
            {
                // Progresso parziale
                onPartialProgress?.Invoke();
            }
        }

        /// <summary>
        /// Rimuove un oggetto dal receiver
        /// </summary>
        private void RemoveObject(GameObject magneticObject)
        {
            if (!currentMagneticObjects.Contains(magneticObject)) return;

            // Notifica l'oggetto che non è più su un receiver
            MagneticObject magObj = magneticObject.GetComponent<MagneticObject>();
            if (magObj != null)
            {
                magObj.SetOnReceiver(false);
            }

            bool wasActivated = isActivated;
            currentMagneticObjects.Remove(magneticObject);

            Debug.Log($"{gameObject.name}: Oggetto '{magneticObject.name}' rimosso ({currentMagneticObjects.Count}/{requiredObjectCount})");

            // Se era attivato e ora non abbiamo abbastanza oggetti, disattiva
            if (wasActivated && currentMagneticObjects.Count < requiredObjectCount)
            {
                DeactivateReceiver();
            }
        }

        private void ActivateReceiver()
        {
            if (isActivated) return;

            isActivated = true;

            Debug.Log($"{gameObject.name}: Ricevitore ATTIVATO! ({currentMagneticObjects.Count} oggetti)");

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

            Debug.Log($"{gameObject.name}: Ricevitore disattivato");

            // Riproduci suono
            PlaySound(deactivationSound);

            // Invoca evento custom
            onDeactivated?.Invoke();
        }

        private void LockObject(GameObject obj, int objectIndex)
        {
            // Determina lo snap point da usare
            Transform targetSnapPoint = GetSnapPointForIndex(objectIndex);

            // Posiziona l'oggetto sullo snap point
            obj.transform.position = targetSnapPoint.position;
            obj.transform.rotation = targetSnapPoint.rotation;

            // Disabilita la fisica
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Ottiene lo snap point per un determinato indice di oggetto
        /// </summary>
        private Transform GetSnapPointForIndex(int index)
        {
            if (!useMultipleSnapPoints || index == 0)
            {
                return snapPoint;
            }

            // Usa gli snap points aggiuntivi
            int additionalIndex = index - 1;
            if (additionalIndex < additionalSnapPoints.Count && additionalSnapPoints[additionalIndex] != null)
            {
                return additionalSnapPoints[additionalIndex];
            }

            // Fallback: usa lo snap point principale
            return snapPoint;
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
