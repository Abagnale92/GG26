using UnityEngine;
using Masks;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private MaskManager maskManager;
        private Vector3 velocity;
        private bool isGrounded;
        private int jumpCount = 0;

        public bool IsGrounded => isGrounded;
        public Vector3 Velocity => velocity;
        public int JumpCount => jumpCount;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            maskManager = GetComponent<MaskManager>();

            // Se non assegnata, trova la camera principale
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }
        }

        private void Update()
        {
            CheckGround();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            HandleMaskSwitch();
            HandleAbilityInput();
        }

        private void CheckGround()
        {
            // Usa sempre controller.isGrounded che è più affidabile
            isGrounded = controller.isGrounded;

            if (isGrounded)
            {
                jumpCount = 0; // Reset quando tocca terra

                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // Calcola la direzione relativa alla camera
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;

                // Appiattisci sul piano orizzontale (ignora inclinazione camera)
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();

                // Direzione finale = input trasformato rispetto alla camera
                Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

                // Ruota il player nella direzione del movimento
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Muovi il player
                controller.Move(moveDirection * moveSpeed * Time.deltaTime);
            }
        }

        private void HandleJump()
        {
            if (Input.GetButtonDown("Jump"))
            {
                int maxJumps = GetMaxJumps();

                if (jumpCount < maxJumps)
                {
                    velocity.y = jumpForce;
                    jumpCount++;
                }
            }
        }

        /// <summary>
        /// Ritorna il numero massimo di salti (1 normale, 2 con Arlecchino)
        /// </summary>
        private int GetMaxJumps()
        {
            if (maskManager != null && maskManager.CurrentMaskType == MaskType.Arlecchino)
            {
                return 2; // Doppio salto con Arlecchino
            }
            return 1; // Salto singolo normale
        }

        private void ApplyGravity()
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleMaskSwitch()
        {
            if (maskManager == null) return;

            // Tasti 1-4 per cambiare maschera, 0 per nessuna
            if (Input.GetKeyDown(KeyCode.Alpha1))
                maskManager.EquipMask(MaskType.Pulcinella);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                maskManager.EquipMask(MaskType.Arlecchino);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                maskManager.EquipMask(MaskType.Colombina);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                maskManager.EquipMask(MaskType.Capitano);
            else if (Input.GetKeyDown(KeyCode.Alpha0))
                maskManager.RemoveMask();

            // Tab per ciclare tra le maschere
            if (Input.GetKeyDown(KeyCode.Tab))
                maskManager.CycleNextMask();
        }

        private void HandleAbilityInput()
        {
            // Tasto E per usare l'abilità attiva
            if (Input.GetKeyDown(KeyCode.E))
            {
                maskManager?.CurrentAbility?.UseAbility();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
    }
}
