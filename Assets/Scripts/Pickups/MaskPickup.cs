using UnityEngine;
using Masks;
using Player;

namespace Pickups
{
    /// <summary>
    /// Pickup che sblocca una maschera quando il player lo raccoglie.
    /// Posizionare nel livello con un Collider trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MaskPickup : MonoBehaviour
    {
        [Header("Mask Settings")]
        [SerializeField] private MaskType maskToUnlock;
        [SerializeField] private bool autoEquip = true;
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Visual Feedback")]
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.3f;

        [Header("Audio/FX")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private GameObject pickupEffect;

        private Vector3 startPosition;
        private AudioSource audioSource;

        private void Awake()
        {
            // Assicurati che il collider sia un trigger
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            startPosition = transform.position;
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            // Rotazione continua
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Movimento su/giù
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Controlla se è il player
            MaskManager maskManager = other.GetComponent<MaskManager>();
            if (maskManager == null)
            {
                maskManager = other.GetComponentInParent<MaskManager>();
            }

            if (maskManager != null)
            {
                Pickup(maskManager);
            }
        }

        private void Pickup(MaskManager maskManager)
        {
            // Sblocca la maschera
            maskManager.UnlockMask(maskToUnlock);

            // Equipaggia automaticamente se richiesto
            if (autoEquip)
            {
                maskManager.EquipMask(maskToUnlock);
            }

            // Feedback audio
            if (pickupSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
            }

            // Feedback visivo
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            Debug.Log($"Raccolta maschera: {maskToUnlock}");

            // Distruggi o disattiva il pickup
            if (destroyOnPickup)
            {
                if (pickupSound != null && audioSource == null)
                {
                    // Se stiamo usando PlayClipAtPoint, possiamo distruggere subito
                    Destroy(gameObject);
                }
                else if (audioSource != null && pickupSound != null)
                {
                    // Aspetta che il suono finisca
                    GetComponent<Collider>().enabled = false;
                    GetComponent<Renderer>().enabled = false;
                    Destroy(gameObject, pickupSound.length);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            // Visualizza il tipo di maschera con un colore diverso
            switch (maskToUnlock)
            {
                case MaskType.Pulcinella:
                    Gizmos.color = Color.white;
                    break;
                case MaskType.Arlecchino:
                    Gizmos.color = new Color(1f, 0.5f, 0f); // Arancione
                    break;
                case MaskType.Colombina:
                    Gizmos.color = Color.cyan;
                    break;
                case MaskType.Capitano:
                    Gizmos.color = Color.red;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }

            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
