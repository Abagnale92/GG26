using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Nemico melee che insegue il player quando è nel range e lo attacca.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyMelee : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackDamage = 1f;

        private Transform player;
        private float lastAttackTime;
        private bool isChasing = false;
        private Enemy enemyComponent;
        private Animator animator;

        // Animator parameter hashes
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

        private void Start()
        {
            // Trova il player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }

            // Ottieni riferimento al componente Enemy
            enemyComponent = GetComponent<Enemy>();

            // Ottieni l'Animator (può essere su questo oggetto o su un figlio)
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (player == null) return;

            // Non fare nulla se il nemico è morto
            if (enemyComponent != null && enemyComponent.IsDead) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Controlla se il player è nel range di detection
            if (distanceToPlayer <= detectionRange)
            {
                isChasing = true;

                // Se nel range d'attacco, attacca
                if (distanceToPlayer <= attackRange)
                {
                    // Fermo mentre attacca
                    SetWalking(false);
                    SetAttacking(true);
                    TryAttack();
                }
                else
                {
                    // Altrimenti insegui
                    SetAttacking(false);
                    SetWalking(true);
                    ChasePlayer();
                }
            }
            else
            {
                isChasing = false;
                SetWalking(false);
                SetAttacking(false);
            }
        }

        private void SetWalking(bool walking)
        {
            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, walking);
            }
        }

        private void SetAttacking(bool attacking)
        {
            if (animator != null)
            {
                animator.SetBool(IsAttackingHash, attacking);
            }
        }

        private void ChasePlayer()
        {
            // Direzione verso il player
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Mantieni sul piano orizzontale

            // Ruota verso il player
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Muovi verso il player
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                Attack();
            }
        }

        private void Attack()
        {
            Debug.Log($"{gameObject.name} attacca il player!");

            // Cerca il PlayerHealth sul player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Range di detection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Range di attacco
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
