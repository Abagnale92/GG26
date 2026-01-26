using UnityEngine;
using System.Collections.Generic;

namespace Masks
{
    /// <summary>
    /// Maschera di Colombina: attrae oggetti magnetici verso il player.
    /// Gli oggetti attraibili devono avere il tag "Magnetic" e un Rigidbody.
    /// </summary>
    public class ColombinaMask : MaskAbility
    {
        [Header("Colombina Settings")]
        [SerializeField] private string magneticTag = "Magnetic";
        [SerializeField] private float attractRadius = 8f;
        [SerializeField] private float attractForce = 10f;
        [SerializeField] private float holdDistance = 1.5f;
        [SerializeField] private float holdHeight = 1f;
        [SerializeField] private LayerMask magneticLayer = ~0;

        private List<Rigidbody> attractedObjects = new List<Rigidbody>();
        private Collider[] overlapResults = new Collider[20];

        private void Awake()
        {
            maskType = MaskType.Colombina;
        }

        protected override void OnActivate()
        {
            Debug.Log("Colombina attivata - Effetto magnete attivo!");
        }

        protected override void OnDeactivate()
        {
            // Rilascia tutti gli oggetti attratti
            ReleaseAllObjects();
            Debug.Log("Colombina disattivata - Oggetti rilasciati");
        }

        public override void UpdateAbility()
        {
            FindMagneticObjects();
            AttractObjects();
        }

        private void FindMagneticObjects()
        {
            attractedObjects.Clear();

            // Trova tutti gli oggetti nel raggio
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                attractRadius,
                overlapResults,
                magneticLayer
            );

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapResults[i];

                // Controlla se ha il tag giusto
                if (col.CompareTag(magneticTag))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        attractedObjects.Add(rb);
                    }
                }
            }
        }

        private void AttractObjects()
        {
            Vector3 targetPosition = transform.position + transform.forward * holdDistance + Vector3.up * holdHeight;

            foreach (var rb in attractedObjects)
            {
                if (rb == null) continue;

                // Calcola la direzione verso il player
                Vector3 direction = (targetPosition - rb.position);
                float distance = direction.magnitude;

                if (distance > 0.1f)
                {
                    // Forza proporzionale alla distanza (più forte se lontano)
                    float forceMagnitude = attractForce * Mathf.Clamp01(distance / attractRadius);
                    rb.AddForce(direction.normalized * forceMagnitude, ForceMode.Acceleration);

                    // Riduci velocità quando vicino per evitare oscillazioni
                    if (distance < holdDistance * 0.5f)
                    {
                        rb.linearVelocity *= 0.9f;
                    }
                }
            }
        }

        private void ReleaseAllObjects()
        {
            // Gli oggetti cadranno naturalmente grazie alla gravità
            attractedObjects.Clear();
        }

        /// <summary>
        /// Lancia l'oggetto più vicino nella direzione in cui guarda il player
        /// </summary>
        public override void UseAbility()
        {
            if (attractedObjects.Count == 0)
            {
                Debug.Log("Nessun oggetto da lanciare!");
                return;
            }

            // Trova l'oggetto più vicino
            Rigidbody closest = null;
            float closestDist = float.MaxValue;

            foreach (var rb in attractedObjects)
            {
                if (rb == null) continue;

                float dist = Vector3.Distance(rb.position, transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = rb;
                }
            }

            if (closest != null)
            {
                // Lancia l'oggetto in avanti
                Vector3 throwDirection = transform.forward + Vector3.up * 0.3f;
                closest.linearVelocity = Vector3.zero;
                closest.AddForce(throwDirection.normalized * attractForce * 2f, ForceMode.Impulse);
                Debug.Log($"Oggetto lanciato: {closest.name}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attractRadius);

            Gizmos.color = Color.cyan;
            Vector3 holdPos = transform.position + transform.forward * holdDistance + Vector3.up * holdHeight;
            Gizmos.DrawWireSphere(holdPos, 0.3f);
        }
    }
}
