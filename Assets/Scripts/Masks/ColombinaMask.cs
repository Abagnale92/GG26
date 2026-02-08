using UnityEngine;
using System.Collections.Generic;
using Puzzles;

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
        [SerializeField] private float holdDistance = 1.5f;
        [SerializeField] private float holdHeight = 1f;
        [SerializeField] private LayerMask magneticLayer = ~0;

        [Header("Attraction Settings")]
        [SerializeField] private float attractSpeed = 8f; // Velocità con cui gli oggetti si avvicinano
        [SerializeField] private float smoothing = 5f; // Smoothing del movimento (più alto = più fluido)
        [SerializeField] private float arrivalThreshold = 0.5f; // Distanza a cui l'oggetto è considerato "arrivato"

        [Header("Throw Settings")]
        [SerializeField] private float throwForce = 8f; // Forza del lancio (separata dall'attrazione)
        [SerializeField] private float throwUpwardAngle = 0.2f; // Angolo verso l'alto (0 = dritto, 1 = molto in alto)
        [SerializeField] private float throwImmunityTime = 1.5f; // Tempo durante il quale l'oggetto lanciato non può essere ri-attratto

        private List<Rigidbody> attractedObjects = new List<Rigidbody>();
        private Collider[] overlapResults = new Collider[20];
        private Dictionary<Rigidbody, float> thrownObjectsImmunity = new Dictionary<Rigidbody, float>(); // Oggetti lanciati con tempo di immunità
        private Dictionary<Rigidbody, bool> originalGravityState = new Dictionary<Rigidbody, bool>(); // Stato originale della gravità

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
            UpdateThrownImmunity();
            FindMagneticObjects();
            AttractObjects();
        }

        /// <summary>
        /// Aggiorna i timer di immunità degli oggetti lanciati
        /// </summary>
        private void UpdateThrownImmunity()
        {
            // Lista temporanea per rimuovere oggetti scaduti
            List<Rigidbody> toRemove = new List<Rigidbody>();

            // Crea una copia delle chiavi per iterare
            List<Rigidbody> keys = new List<Rigidbody>(thrownObjectsImmunity.Keys);

            foreach (var rb in keys)
            {
                if (rb == null)
                {
                    toRemove.Add(rb);
                    continue;
                }

                thrownObjectsImmunity[rb] -= Time.deltaTime;

                if (thrownObjectsImmunity[rb] <= 0f)
                {
                    toRemove.Add(rb);
                }
            }

            // Rimuovi gli oggetti scaduti
            foreach (var rb in toRemove)
            {
                thrownObjectsImmunity.Remove(rb);
            }
        }

        private void FindMagneticObjects()
        {
            // Prima ripristina la gravità e notifica gli oggetti che non sono più attratti
            foreach (var rb in attractedObjects)
            {
                if (rb != null)
                {
                    // Ripristina la gravità originale
                    if (originalGravityState.ContainsKey(rb))
                    {
                        rb.useGravity = originalGravityState[rb];
                        originalGravityState.Remove(rb);
                    }

                    MagneticObject magObj = rb.GetComponent<MagneticObject>();
                    if (magObj != null)
                    {
                        magObj.SetBeingAttracted(false);
                    }
                }
            }

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
                    if (rb != null && !rb.isKinematic) // Non attrarre oggetti bloccati (es. su receiver)
                    {
                        // Non attrarre oggetti appena lanciati (in immunità)
                        if (thrownObjectsImmunity.ContainsKey(rb))
                        {
                            continue;
                        }

                        attractedObjects.Add(rb);

                        // Salva lo stato originale della gravità e disabilitala
                        if (!originalGravityState.ContainsKey(rb))
                        {
                            originalGravityState[rb] = rb.useGravity;
                        }
                        rb.useGravity = false;

                        // Notifica l'oggetto che viene attratto
                        MagneticObject magObj = rb.GetComponent<MagneticObject>();
                        if (magObj != null)
                        {
                            magObj.SetBeingAttracted(true);
                        }
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

                // Calcola la direzione verso il punto di hold
                Vector3 direction = (targetPosition - rb.position);
                float distance = direction.magnitude;

                if (distance > arrivalThreshold)
                {
                    // Movimento fluido verso il target
                    float speed = attractSpeed * Mathf.Clamp01(distance / attractRadius);
                    Vector3 desiredVelocity = direction.normalized * speed;

                    // Interpola verso la velocità desiderata per un movimento fluido
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, smoothing * Time.deltaTime);
                }
                else
                {
                    // Oggetto arrivato: mantienilo fermo nella posizione di hold
                    rb.linearVelocity = Vector3.zero;

                    // Posizione precisa usando MovePosition per evitare jitter
                    rb.MovePosition(Vector3.Lerp(rb.position, targetPosition, smoothing * Time.deltaTime));
                }

                // Ferma completamente la rotazione per un effetto stabile
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void ReleaseAllObjects()
        {
            // Notifica tutti gli oggetti che non sono più attratti
            // e segnali come "lanciati" così possono fare respawn se lontani
            foreach (var rb in attractedObjects)
            {
                if (rb != null)
                {
                    // Ripristina la gravità originale
                    if (originalGravityState.ContainsKey(rb))
                    {
                        rb.useGravity = originalGravityState[rb];
                    }

                    MagneticObject magObj = rb.GetComponent<MagneticObject>();
                    if (magObj != null)
                    {
                        magObj.SetThrown(); // Segna come lanciato per permettere il respawn
                    }
                }
            }

            // Gli oggetti cadranno naturalmente grazie alla gravità
            attractedObjects.Clear();
            originalGravityState.Clear();
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
                // Ripristina la gravità
                if (originalGravityState.ContainsKey(closest))
                {
                    closest.useGravity = originalGravityState[closest];
                    originalGravityState.Remove(closest);
                }

                // Notifica l'oggetto che è stato lanciato
                MagneticObject magObj = closest.GetComponent<MagneticObject>();
                if (magObj != null)
                {
                    magObj.SetThrown();
                }

                // Aggiungi immunità all'attrazione per questo oggetto
                thrownObjectsImmunity[closest] = throwImmunityTime;

                // Rimuovi l'oggetto dalla lista degli attratti
                attractedObjects.Remove(closest);

                // Lancia l'oggetto in avanti con forza configurabile
                Vector3 throwDirection = (transform.forward + Vector3.up * throwUpwardAngle).normalized;
                closest.linearVelocity = Vector3.zero;
                closest.AddForce(throwDirection * throwForce, ForceMode.Impulse);
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
