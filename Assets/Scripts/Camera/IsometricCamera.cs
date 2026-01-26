using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Camera isometrica stile Death's Door che segue il player.
    /// Posiziona la camera con angolazione fissa e segue il target con smooth follow.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool findPlayerOnStart = true;

        [Header("Camera Position")]
        [SerializeField] private float distance = 15f;
        [SerializeField] private float height = 12f;
        [SerializeField] private float angle = 45f; // Angolo orizzontale (rotazione Y)

        [Header("Camera Angle")]
        [SerializeField] private float pitch = 45f; // Inclinazione verso il basso (tipico isometrico: 30-50)

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = true;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmooth = 3f;

        private Vector3 currentVelocity;
        private Vector3 lookAheadOffset;
        private Vector3 lastTargetPosition;

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
                // Posiziona immediatamente la camera all'avvio
                SetCameraPositionImmediate();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateLookAhead();
            UpdateCameraPosition();
        }

        private void UpdateLookAhead()
        {
            if (!useLookAhead) return;

            // Calcola la direzione del movimento del target
            Vector3 moveDirection = (target.position - lastTargetPosition).normalized;
            moveDirection.y = 0; // Ignora movimento verticale

            // Smooth del look ahead
            Vector3 targetLookAhead = moveDirection * lookAheadDistance;
            lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead, lookAheadSmooth * Time.deltaTime);

            lastTargetPosition = target.position;
        }

        private void UpdateCameraPosition()
        {
            // Calcola la posizione target della camera
            Vector3 targetPosition = target.position + targetOffset + lookAheadOffset;

            // Calcola l'offset della camera basato su angolo e distanza
            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 cameraOffset = new Vector3(
                Mathf.Sin(angleRad) * distance,
                height,
                Mathf.Cos(angleRad) * distance
            );

            Vector3 desiredPosition = targetPosition + cameraOffset;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Guarda verso il target
            Vector3 lookTarget = targetPosition;
            transform.LookAt(lookTarget);

            // Applica il pitch (inclinazione)
            Vector3 euler = transform.eulerAngles;
            euler.x = pitch;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// Posiziona immediatamente la camera senza smooth
        /// </summary>
        public void SetCameraPositionImmediate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + targetOffset;
            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 cameraOffset = new Vector3(
                Mathf.Sin(angleRad) * distance,
                height,
                Mathf.Cos(angleRad) * distance
            );

            transform.position = targetPosition + cameraOffset;
            transform.LookAt(targetPosition);

            Vector3 euler = transform.eulerAngles;
            euler.x = pitch;
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// Cambia il target della camera
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// Preset per stile Death's Door
        /// </summary>
        [ContextMenu("Apply Death's Door Style")]
        public void ApplyDeathsDoorStyle()
        {
            distance = 12f;
            height = 10f;
            angle = 45f;
            pitch = 40f;
            smoothSpeed = 6f;
            useLookAhead = true;
            lookAheadDistance = 1.5f;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizza la posizione target
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position + targetOffset, 0.5f);

                // Linea dalla camera al target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position + targetOffset);
            }

            // Visualizza il frustum della camera
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, 60f, 50f, 0.1f, 1.78f);
        }
    }
}
