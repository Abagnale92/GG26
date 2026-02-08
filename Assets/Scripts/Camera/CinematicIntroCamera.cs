using UnityEngine;
using Player;

namespace CameraSystem
{
    /// <summary>
    /// Camera con inquadratura cinematica iniziale che torna alla posizione normale quando il player si muove.
    /// Da usare in scene specifiche (es. boss room, intro livello).
    /// </summary>
    public class CinematicIntroCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool findPlayerOnStart = true;

        [Header("Normal Camera Position")]
        [SerializeField] private float normalDistance = 12f;
        [SerializeField] private float normalHeight = 8f;
        [SerializeField] private float normalAngle = 90f;
        [SerializeField] private float normalPitch = 9f;

        [Header("Intro Camera Position")]
        [SerializeField] private float introDistance = 12f;
        [SerializeField] private float introHeight = 10f;
        [SerializeField] private float introAngle = 90f;
        [SerializeField] private float introPitch = 40f;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 1.5f; // Durata della transizione in secondi
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 6f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = true;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmooth = 3f;

        // Stato interno
        private bool isInIntroMode = true;
        private bool isTransitioning = false;
        private float transitionProgress = 0f;
        private Vector3 playerStartPosition;
        private float movementThreshold = 0.5f; // Distanza minima per considerare che il player si è mosso

        private Vector3 lookAheadOffset;
        private Vector3 lastTargetPosition;

        // Valori correnti (interpolati durante la transizione)
        private float currentDistance;
        private float currentHeight;
        private float currentAngle;
        private float currentPitch;

        private void Start()
        {
            if (findPlayerOnStart && target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                lastTargetPosition = target.position;
                playerStartPosition = target.position;

                // Inizia con l'inquadratura intro
                currentDistance = introDistance;
                currentHeight = introHeight;
                currentAngle = introAngle;
                currentPitch = introPitch;

                // Posiziona immediatamente la camera
                SetCameraPositionImmediate();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Controlla se il player si è mosso (solo se siamo in intro mode)
            if (isInIntroMode && !isTransitioning)
            {
                CheckPlayerMovement();
            }

            // Aggiorna la transizione se attiva
            if (isTransitioning)
            {
                UpdateTransition();
            }

            UpdateLookAhead();
            UpdateCameraPosition();
        }

        private void CheckPlayerMovement()
        {
            float distanceFromStart = Vector3.Distance(target.position, playerStartPosition);

            if (distanceFromStart > movementThreshold)
            {
                // Il player si è mosso, inizia la transizione
                StartTransitionToNormal();
            }
        }

        private void StartTransitionToNormal()
        {
            isTransitioning = true;
            transitionProgress = 0f;
            Debug.Log("CinematicIntroCamera: Transizione verso inquadratura normale");
        }

        private void UpdateTransition()
        {
            transitionProgress += Time.deltaTime / transitionDuration;

            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
                isInIntroMode = false;
                Debug.Log("CinematicIntroCamera: Transizione completata");
            }

            // Usa la curva per un'animazione più fluida
            float t = transitionCurve.Evaluate(transitionProgress);

            // Interpola tutti i valori
            currentDistance = Mathf.Lerp(introDistance, normalDistance, t);
            currentHeight = Mathf.Lerp(introHeight, normalHeight, t);
            currentAngle = Mathf.Lerp(introAngle, normalAngle, t);
            currentPitch = Mathf.Lerp(introPitch, normalPitch, t);
        }

        private void UpdateLookAhead()
        {
            if (!useLookAhead || isInIntroMode) return;

            Vector3 moveDirection = (target.position - lastTargetPosition).normalized;
            moveDirection.y = 0;

            Vector3 targetLookAhead = moveDirection * lookAheadDistance;
            lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSmooth * Time.deltaTime);

            lastTargetPosition = target.position;
        }

        private void UpdateCameraPosition()
        {
            Vector3 targetPosition = target.position + targetOffset;

            // Aggiungi look ahead solo se non siamo in intro mode
            if (!isInIntroMode)
            {
                targetPosition += lookAheadOffset;
            }

            // Calcola l'offset della camera
            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector3 cameraOffset = new Vector3(
                Mathf.Sin(angleRad) * currentDistance,
                currentHeight,
                Mathf.Cos(angleRad) * currentDistance
            );

            Vector3 desiredPosition = targetPosition + cameraOffset;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Guarda verso il target
            transform.LookAt(targetPosition);

            // Applica il pitch
            Vector3 euler = transform.eulerAngles;
            euler.x = currentPitch;
            transform.eulerAngles = euler;
        }

        public void SetCameraPositionImmediate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + targetOffset;
            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector3 cameraOffset = new Vector3(
                Mathf.Sin(angleRad) * currentDistance,
                currentHeight,
                Mathf.Cos(angleRad) * currentDistance
            );

            transform.position = targetPosition + cameraOffset;
            transform.LookAt(targetPosition);

            Vector3 euler = transform.eulerAngles;
            euler.x = currentPitch;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// Forza il ritorno all'inquadratura intro
        /// </summary>
        public void ResetToIntro()
        {
            isInIntroMode = true;
            isTransitioning = false;
            transitionProgress = 0f;

            currentDistance = introDistance;
            currentHeight = introHeight;
            currentAngle = introAngle;
            currentPitch = introPitch;

            if (target != null)
            {
                playerStartPosition = target.position;
            }
        }

        /// <summary>
        /// Forza il passaggio immediato all'inquadratura normale
        /// </summary>
        public void SkipToNormal()
        {
            isInIntroMode = false;
            isTransitioning = false;
            transitionProgress = 1f;

            currentDistance = normalDistance;
            currentHeight = normalHeight;
            currentAngle = normalAngle;
            currentPitch = normalPitch;
        }

        /// <summary>
        /// Imposta i valori dall'inspector corrente come valori "Intro"
        /// </summary>
        [ContextMenu("Set Current as Intro Values")]
        public void SetCurrentAsIntroValues()
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                // Salva i valori correnti del transform come intro values
                Debug.Log("Imposta i valori manualmente nell'inspector basandoti sulla posizione corrente della camera");
            }
        }

        /// <summary>
        /// Imposta i valori dall'inspector corrente come valori "Normal"
        /// </summary>
        [ContextMenu("Set Current as Normal Values")]
        public void SetCurrentAsNormalValues()
        {
            Debug.Log("Imposta i valori manualmente nell'inspector basandoti sulla posizione corrente della camera");
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
                playerStartPosition = target.position;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                // Visualizza posizione intro
                Gizmos.color = Color.yellow;
                float introAngleRad = introAngle * Mathf.Deg2Rad;
                Vector3 introOffset = new Vector3(
                    Mathf.Sin(introAngleRad) * introDistance,
                    introHeight,
                    Mathf.Cos(introAngleRad) * introDistance
                );
                Vector3 introPos = target.position + targetOffset + introOffset;
                Gizmos.DrawWireSphere(introPos, 0.5f);
                Gizmos.DrawLine(introPos, target.position + targetOffset);

                // Visualizza posizione normale
                Gizmos.color = Color.green;
                float normalAngleRad = normalAngle * Mathf.Deg2Rad;
                Vector3 normalOffset = new Vector3(
                    Mathf.Sin(normalAngleRad) * normalDistance,
                    normalHeight,
                    Mathf.Cos(normalAngleRad) * normalDistance
                );
                Vector3 normalPos = target.position + targetOffset + normalOffset;
                Gizmos.DrawWireSphere(normalPos, 0.5f);
                Gizmos.DrawLine(normalPos, target.position + targetOffset);

                // Linea tra le due posizioni
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(introPos, normalPos);
            }
        }
    }
}
