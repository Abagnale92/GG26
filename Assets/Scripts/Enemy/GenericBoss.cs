using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Player;

namespace Enemies
{
    /// <summary>
    /// Boss generico con dialogo iniziale e attacchi configurabili dall'inspector.
    /// Supporta attacchi melee, a distanza e in salto.
    /// </summary>
    public class GenericBoss : MonoBehaviour
    {
        #region Enums

        public enum BossState
        {
            Idle,
            Dialogue,
            Walking,
            Attacking,
            Dead
        }

        public enum AttackType
        {
            Melee,      // Attacco ravvicinato
            Ranged,     // Attacco a distanza (proiettili)
            Jump        // Attacco in salto con AOE
        }

        #endregion

        #region Attack Configuration Class

        [System.Serializable]
        public class BossAttack
        {
            [Header("Attack Info")]
            public string attackName = "Attack";
            public AttackType attackType = AttackType.Melee;
            public bool enabled = true;

            [Header("Stats")]
            public int damage = 1;
            public float cooldown = 2f;
            public float range = 3f; // Range in cui questo attacco può essere usato

            [Header("Animation")]
            public string animationTrigger = "Attack"; // Nome del trigger nell'Animator

            [Header("Timing")]
            public float damageDelay = 0.3f; // Tempo prima che il danno venga applicato
            public float attackDuration = 0.8f; // Durata totale dell'attacco

            [Header("Audio")]
            public AudioClip attackSound;

            [Header("Melee Settings")]
            public float meleeHitRadius = 2f; // Raggio del colpo melee

            [Header("Ranged Settings")]
            public GameObject projectilePrefab;
            public Transform firePoint; // Se null, usa il transform del boss
            public float projectileSpeed = 10f;
            public int projectileCount = 1; // Per attacchi a ventaglio
            public float spreadAngle = 0f; // Angolo del ventaglio (0 = singolo proiettile)

            [Header("Jump Settings")]
            public float jumpHeight = 5f;
            public float jumpDuration = 1f;
            public float aoeRadius = 4f;
            public float indicatorDuration = 0.8f; // Tempo che l'indicatore rimane visibile
            public GameObject aoeIndicatorPrefab;
            public Color indicatorColor = new Color(1f, 0f, 0f, 0.5f);
            public AudioClip landSound;

            // Runtime
            [HideInInspector] public float lastUseTime = -999f;
        }

        #endregion

        #region Inspector Fields

        [Header("Boss Info")]
        [SerializeField] private string bossName = "Boss";

        [Header("Stats")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private float detectionRange = 15f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Intro Dialogue")]
        [SerializeField] private bool hasIntroDialogue = true;
        [SerializeField][TextArea(2, 5)] private string[] dialogueLines;
        [SerializeField] private float dialogueLineDelay = 2f;
        [SerializeField] private AudioClip voiceSound;

        [Header("Dialogue UI")]
        [SerializeField] private GameObject dialogueUI;
        [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
        [SerializeField] private TMPro.TextMeshProUGUI bossNameText;

        [Header("Attacks")]
        [SerializeField] private List<BossAttack> attacks = new List<BossAttack>();

        [Header("Visual Feedback")]
        [SerializeField] private float hitFlashDuration = 0.1f;
        [SerializeField] private Color hitColor = Color.red;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Audio")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Death Settings")]
        [Tooltip("Tempo di attesa per l'animazione di morte prima di disattivare il boss")]
        [SerializeField] private float deathAnimationDuration = 3f;
        [Tooltip("Se true, distrugge il GameObject invece di disattivarlo")]
        [SerializeField] private bool destroyOnDeath = false;

        #endregion

        #region Private Fields

        // Animator hashes
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private int currentHealth;
        private BossState currentState = BossState.Idle;
        private Transform player;
        private PlayerHealth playerHealth;
        private AudioSource audioSource;
        private Renderer bossRenderer;
        private Color originalColor;

        private bool isAttacking = false;
        private bool dialogueCompleted = false;
        private bool combatStarted = false;
        private float initialGroundY;
        private GameObject currentIndicator;

        #endregion

        #region Properties

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentState == BossState.Dead;
        public string BossName => bossName;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnBossDeath;
        public event System.Action OnDialogueStarted;
        public event System.Action OnDialogueEnded;
        public event System.Action OnCombatStarted;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            currentHealth = maxHealth;
            initialGroundY = transform.position.y;

            // Trova il player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }

            // Setup componenti
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            bossRenderer = GetComponentInChildren<Renderer>();
            if (bossRenderer != null)
            {
                originalColor = bossRenderer.material.color;
            }

            // Nascondi UI dialogo
            if (dialogueUI != null)
            {
                dialogueUI.SetActive(false);
            }

            // Inizializza cooldown attacchi
            foreach (var attack in attacks)
            {
                attack.lastUseTime = -attack.cooldown;
            }
        }

        private void Update()
        {
            if (currentState == BossState.Dead || player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Se il combattimento non è iniziato, controlla se il player è nel range
            if (!combatStarted)
            {
                if (distanceToPlayer <= detectionRange)
                {
                    StartEncounter();
                }
                return;
            }

            // Se sta parlando, non fare nulla
            if (currentState == BossState.Dialogue) return;

            // Se sta attaccando, non fare nulla
            if (isAttacking) return;

            // Combattimento attivo
            DecideAction(distanceToPlayer);
        }

        #endregion

        #region Encounter & Dialogue

        private void StartEncounter()
        {
            combatStarted = true;

            if (hasIntroDialogue && dialogueLines != null && dialogueLines.Length > 0)
            {
                StartCoroutine(PlayDialogue());
            }
            else
            {
                dialogueCompleted = true;
                OnCombatStarted?.Invoke();
            }
        }

        private IEnumerator PlayDialogue()
        {
            currentState = BossState.Dialogue;
            OnDialogueStarted?.Invoke();

            // Mostra UI dialogo
            if (dialogueUI != null)
            {
                dialogueUI.SetActive(true);
            }

            // Mostra nome boss
            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            // Riproduci suono voce
            PlaySound(voiceSound);

            // Mostra ogni linea di dialogo
            foreach (string line in dialogueLines)
            {
                if (dialogueText != null)
                {
                    dialogueText.text = line;
                }
                else
                {
                    Debug.Log($"[{bossName}]: {line}");
                }

                yield return new WaitForSeconds(dialogueLineDelay);
            }

            // Nascondi UI dialogo
            if (dialogueUI != null)
            {
                dialogueUI.SetActive(false);
            }

            dialogueCompleted = true;
            currentState = BossState.Idle;
            OnDialogueEnded?.Invoke();
            OnCombatStarted?.Invoke();

            Debug.Log($"{bossName}: Dialogo completato, inizia il combattimento!");
        }

        #endregion

        #region Combat Logic

        private void DecideAction(float distanceToPlayer)
        {
            // Guarda verso il player
            LookAtPlayer();

            // Trova l'attacco migliore da usare
            BossAttack bestAttack = FindBestAttack(distanceToPlayer);

            if (bestAttack != null)
            {
                StartCoroutine(ExecuteAttack(bestAttack));
            }
            else
            {
                // Nessun attacco disponibile, avvicinati
                MoveTowardsPlayer();
            }
        }

        private BossAttack FindBestAttack(float distanceToPlayer)
        {
            List<BossAttack> availableAttacks = new List<BossAttack>();

            foreach (var attack in attacks)
            {
                if (!attack.enabled) continue;
                if (Time.time - attack.lastUseTime < attack.cooldown) continue;

                // Controlla se il player è nel range dell'attacco
                if (distanceToPlayer <= attack.range)
                {
                    availableAttacks.Add(attack);
                }
            }

            if (availableAttacks.Count == 0) return null;

            // Scegli casualmente tra gli attacchi disponibili
            return availableAttacks[Random.Range(0, availableAttacks.Count)];
        }

        private void LookAtPlayer()
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void MoveTowardsPlayer()
        {
            SetState(BossState.Walking);

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;

            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        private void SetState(BossState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, newState == BossState.Walking);
            }
        }

        #endregion

        #region Attack Execution

        private IEnumerator ExecuteAttack(BossAttack attack)
        {
            isAttacking = true;
            SetState(BossState.Attacking);
            attack.lastUseTime = Time.time;

            Debug.Log($"{bossName} usa: {attack.attackName}");

            switch (attack.attackType)
            {
                case AttackType.Melee:
                    yield return StartCoroutine(ExecuteMeleeAttack(attack));
                    break;
                case AttackType.Ranged:
                    yield return StartCoroutine(ExecuteRangedAttack(attack));
                    break;
                case AttackType.Jump:
                    yield return StartCoroutine(ExecuteJumpAttack(attack));
                    break;
            }

            isAttacking = false;
        }

        private IEnumerator ExecuteMeleeAttack(BossAttack attack)
        {
            // Animazione e suono
            if (animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                animator.SetTrigger(attack.animationTrigger);
            }
            PlaySound(attack.attackSound);

            // Aspetta il momento del colpo
            yield return new WaitForSeconds(attack.damageDelay);

            // Applica danno se il player è nel raggio
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attack.meleeHitRadius && playerHealth != null)
            {
                playerHealth.TakeDamage(attack.damage);
                Debug.Log($"{bossName} ha colpito il player con {attack.attackName}!");
            }

            // Aspetta fine animazione
            yield return new WaitForSeconds(attack.attackDuration - attack.damageDelay);
        }

        private IEnumerator ExecuteRangedAttack(BossAttack attack)
        {
            // Guarda verso il player
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(dirToPlayer);

            // Animazione e suono
            if (animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                animator.SetTrigger(attack.animationTrigger);
            }
            PlaySound(attack.attackSound);

            // Aspetta il momento dello sparo
            yield return new WaitForSeconds(attack.damageDelay);

            // Spara proiettili
            FireProjectiles(attack);

            // Aspetta fine animazione
            yield return new WaitForSeconds(attack.attackDuration - attack.damageDelay);
        }

        private void FireProjectiles(BossAttack attack)
        {
            if (attack.projectilePrefab == null) return;

            Transform spawnPoint = attack.firePoint != null ? attack.firePoint : transform;

            if (attack.projectileCount <= 1 || attack.spreadAngle <= 0)
            {
                // Singolo proiettile
                SpawnProjectile(attack, spawnPoint.position, transform.forward);
            }
            else
            {
                // Ventaglio di proiettili
                float startAngle = -attack.spreadAngle / 2f;
                float angleStep = attack.spreadAngle / (attack.projectileCount - 1);

                for (int i = 0; i < attack.projectileCount; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);
                    Quaternion rotation = Quaternion.Euler(0f, currentAngle, 0f);
                    Vector3 direction = rotation * transform.forward;

                    SpawnProjectile(attack, spawnPoint.position, direction);
                }
            }
        }

        private void SpawnProjectile(BossAttack attack, Vector3 position, Vector3 direction)
        {
            GameObject projectile = Instantiate(attack.projectilePrefab, position, Quaternion.LookRotation(direction));

            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                projScript.Initialize(direction, attack.projectileSpeed);
            }
            else
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * attack.projectileSpeed;
                }
            }
        }

        private IEnumerator ExecuteJumpAttack(BossAttack attack)
        {
            Vector3 jumpStartPos = transform.position;
            Vector3 jumpTargetPos = new Vector3(player.position.x, initialGroundY, player.position.z);

            // Mostra indicatore AOE
            ShowAOEIndicator(jumpTargetPos, attack);

            yield return new WaitForSeconds(attack.indicatorDuration);

            // Animazione e suono
            if (animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                animator.SetTrigger(attack.animationTrigger);
            }
            PlaySound(attack.attackSound);

            // Fase di salto
            float elapsed = 0f;
            while (elapsed < attack.jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / attack.jumpDuration;

                float x = Mathf.Lerp(jumpStartPos.x, jumpTargetPos.x, t);
                float z = Mathf.Lerp(jumpStartPos.z, jumpTargetPos.z, t);
                float y = initialGroundY + Mathf.Sin(t * Mathf.PI) * attack.jumpHeight;

                transform.position = new Vector3(x, y, z);

                yield return null;
            }

            // Atterraggio
            transform.position = new Vector3(jumpTargetPos.x, initialGroundY, jumpTargetPos.z);
            PlaySound(attack.landSound);
            HideAOEIndicator();

            // Danno AOE
            DealAOEDamage(attack);

            yield return new WaitForSeconds(0.5f);
        }

        private void DealAOEDamage(BossAttack attack)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attack.aoeRadius);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerHealth ph = hitCollider.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(attack.damage);
                        Debug.Log($"{bossName} {attack.attackName} ha colpito il player!");
                    }
                }
            }
        }

        private void ShowAOEIndicator(Vector3 position, BossAttack attack)
        {
            HideAOEIndicator();

            if (attack.aoeIndicatorPrefab != null)
            {
                currentIndicator = Instantiate(attack.aoeIndicatorPrefab, position, Quaternion.identity);
                currentIndicator.transform.localScale = new Vector3(attack.aoeRadius * 2f, 0.1f, attack.aoeRadius * 2f);
            }
            else
            {
                currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                currentIndicator.name = "AOE_Indicator";
                currentIndicator.transform.position = new Vector3(position.x, position.y + 0.05f, position.z);
                currentIndicator.transform.localScale = new Vector3(attack.aoeRadius * 2f, 0.02f, attack.aoeRadius * 2f);

                Collider col = currentIndicator.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = currentIndicator.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = attack.indicatorColor;
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }
        }

        private void HideAOEIndicator()
        {
            if (currentIndicator != null)
            {
                Destroy(currentIndicator);
                currentIndicator = null;
            }
        }

        #endregion

        #region Damage & Death

        public void TakeHit(int damage = 1)
        {
            if (currentState == BossState.Dead) return;

            // Invulnerabile durante il dialogo
            if (currentState == BossState.Dialogue) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"{bossName} colpito! Vita: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);

            PlaySound(hurtSound);
            if (bossRenderer != null)
            {
                StartCoroutine(HitFlash());
            }

            if (currentHealth <= 0)
            {
                StartCoroutine(Die());
            }
        }

        private IEnumerator HitFlash()
        {
            if (bossRenderer != null)
            {
                bossRenderer.material.color = hitColor;
                yield return new WaitForSeconds(hitFlashDuration);

                if (bossRenderer != null)
                {
                    bossRenderer.material.color = originalColor;
                }
            }
        }

        private IEnumerator Die()
        {
            currentState = BossState.Dead;
            isAttacking = false;

            Debug.Log($"{bossName} sconfitto!");

            if (animator != null)
                animator.SetTrigger(DieHash);
            PlaySound(deathSound);

            OnBossDeath?.Invoke();

            // Aspetta la durata configurata per l'animazione di morte
            yield return new WaitForSeconds(deathAnimationDuration);

            // Disattiva o distruggi il boss
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forza l'inizio del combattimento (salta il dialogo)
        /// </summary>
        public void ForceStartCombat()
        {
            combatStarted = true;
            dialogueCompleted = true;
            currentState = BossState.Idle;
            OnCombatStarted?.Invoke();
        }

        /// <summary>
        /// Resetta il boss
        /// </summary>
        public void ResetBoss()
        {
            currentHealth = maxHealth;
            currentState = BossState.Idle;
            combatStarted = false;
            dialogueCompleted = false;
            isAttacking = false;
            gameObject.SetActive(true);

            foreach (var attack in attacks)
            {
                attack.lastUseTime = -attack.cooldown;
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Range di detection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Range degli attacchi
            foreach (var attack in attacks)
            {
                if (!attack.enabled) continue;

                switch (attack.attackType)
                {
                    case AttackType.Melee:
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(transform.position, attack.range);
                        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                        Gizmos.DrawWireSphere(transform.position, attack.meleeHitRadius);
                        break;
                    case AttackType.Ranged:
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(transform.position, attack.range);
                        break;
                    case AttackType.Jump:
                        Gizmos.color = new Color(1f, 0.5f, 0f);
                        Gizmos.DrawWireSphere(transform.position, attack.range);
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                        Gizmos.DrawSphere(transform.position, attack.aoeRadius);
                        break;
                }
            }
        }

        #endregion
    }
}
