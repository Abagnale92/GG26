using UnityEngine;
using Player;

namespace Hazards
{
    /// <summary>
    /// Zona che infligge danno continuo al player (es. fuoco, veleno).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float damagePerTick = 1f;
        [SerializeField] private float tickRate = 1f; // Danno ogni X secondi

        private PlayerHealth playerInZone;
        private float lastTickTime;

        private void Awake()
        {
            // Assicurati che il collider sia trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (playerInZone != null && playerInZone.IsAlive)
            {
                if (Time.time - lastTickTime >= tickRate)
                {
                    lastTickTime = Time.time;
                    playerInZone.TakeDamage(damagePerTick);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = other.GetComponent<PlayerHealth>();
                lastTickTime = Time.time - tickRate; // Infliggi danno subito
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInZone = null;
            }
        }
    }
}
