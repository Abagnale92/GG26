using UnityEngine;
using System.Collections;

namespace Masks
{
    /// <summary>
    /// Maschera del Capitano: fa apparire una spada che ruota intorno al player quando premi E.
    /// </summary>
    public class CapitanoMask : MaskAbility
    {
        [Header("Sword Settings")]
        [SerializeField] private GameObject swordPrefab;
        [SerializeField] private float swordDistance = 1.5f;
        [SerializeField] private float swordHeight = 0.5f; // Altezza rispetto al centro del player
        [SerializeField] private float spinSpeed = 720f; // Gradi al secondo
        [SerializeField] private float cooldown = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip swordSwingSound;

        private GameObject swordInstance;
        private AudioSource audioSource;
        private Sword swordScript;
        private Quaternion swordOriginalRotation;
        private bool isSpinning = false;
        private bool canAttack = true;
        private float currentAngle = 0f;

        private void Awake()
        {
            maskType = MaskType.Capitano;

            // Setup AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        protected override void OnActivate()
        {
            // Crea la spada (NON come figlio del player per evitare problemi di rotazione)
            if (swordPrefab != null && swordInstance == null)
            {
                swordInstance = Instantiate(swordPrefab);
                swordInstance.SetActive(true);

                // Salva la rotazione dell'istanza appena creata (copia dal prefab)
                swordOriginalRotation = swordInstance.transform.rotation;

                // Prendi il riferimento allo script Sword
                swordScript = swordInstance.GetComponent<Sword>();

                // Posiziona la spada davanti al player
                UpdateSwordPosition();

                // Disattiva il collider finché non attacca
                SetSwordCollider(false);
            }

            Debug.Log("Capitano attivato - Spada pronta!");
        }

        protected override void OnDeactivate()
        {
            // Distruggi la spada
            if (swordInstance != null)
            {
                Destroy(swordInstance);
                swordInstance = null;
            }

            isSpinning = false;
            canAttack = true;

            Debug.Log("Capitano disattivato");
        }

        public override void UpdateAbility()
        {
            if (swordInstance == null) return;

            // Se non sta girando, mantieni la spada davanti al player
            if (!isSpinning)
            {
                UpdateSwordPosition();
            }
        }

        private void UpdateSwordPosition()
        {
            if (swordInstance == null) return;

            // Posiziona la spada davanti al player a metà altezza
            Vector3 offset = transform.forward * swordDistance;
            Vector3 swordPos = transform.position + offset + Vector3.up * swordHeight;
            swordInstance.transform.position = swordPos;

            // Orienta la spada in modo che punti verso l'esterno (lontano dal player)
            Vector3 directionFromPlayer = (swordPos - (transform.position + Vector3.up * swordHeight)).normalized;
            PointSwordOutward(directionFromPlayer);
        }

        private void PointSwordOutward(Vector3 outwardDirection)
        {
            // Il cilindro in Unity ha l'asse Y come asse lungo
            // Vogliamo che l'asse Y del cilindro punti verso l'esterno
            swordInstance.transform.rotation = Quaternion.LookRotation(outwardDirection) * Quaternion.Euler(90f, 0f, 0f);
        }

        public override void UseAbility()
        {
            if (!canAttack || isSpinning || swordInstance == null)
            {
                return;
            }

            // Suono spada
            PlaySound(swordSwingSound);

            StartCoroutine(SpinAttack());
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator SpinAttack()
        {
            isSpinning = true;
            canAttack = false;

            // Controlla che la spada esista
            if (swordInstance == null)
            {
                isSpinning = false;
                canAttack = true;
                yield break;
            }

            // Resetta la lista dei nemici colpiti per questo attacco
            if (swordScript != null)
            {
                swordScript.ResetHitList();
            }

            // Attiva il collider della spada
            SetSwordCollider(true);

            // Scegli direzione random: 1 = orario, -1 = antiorario
            int direction = Random.Range(0, 2) == 0 ? 1 : -1;

            // Angolo iniziale basato sulla rotazione del player
            currentAngle = transform.eulerAngles.y;

            float totalRotation = 0f;
            float targetRotation = 360f; // Una rotazione completa

            while (totalRotation < targetRotation && swordInstance != null)
            {
                float rotationThisFrame = spinSpeed * Time.deltaTime;
                currentAngle += rotationThisFrame * direction;
                totalRotation += rotationThisFrame;

                // Calcola la nuova posizione della spada
                float angleRad = currentAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Sin(angleRad) * swordDistance,
                    swordHeight,
                    Mathf.Cos(angleRad) * swordDistance
                );

                Vector3 swordPos = transform.position + offset;
                swordInstance.transform.position = swordPos;

                // Orienta la spada verso l'esterno (lontano dal player)
                Vector3 directionFromPlayer = (swordPos - (transform.position + Vector3.up * swordHeight)).normalized;
                PointSwordOutward(directionFromPlayer);

                yield return null;
            }

            // Disattiva il collider
            SetSwordCollider(false);

            isSpinning = false;

            // Cooldown prima del prossimo attacco
            yield return new WaitForSeconds(cooldown);
            canAttack = true;
        }

        private void SetSwordCollider(bool enabled)
        {
            if (swordInstance == null) return;

            Collider col = swordInstance.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = enabled;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizza il raggio della spada
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * swordHeight, swordDistance);
        }
    }
}
