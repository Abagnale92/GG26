using UnityEngine;
using System.Collections;
using Player;

namespace Enemies
{
    /// <summary>
    /// Boss principale del gioco.
    /// Attacchi: Normale, Salto con AOE, Sferzata a ventaglio con proiettili.
    /// Sceglie gli attacchi in base alla distanza dal player.
    /// </summary>
    public class Boss : MonoBehaviour
    {
        public enum BossState
        {
            Idle,
            Dialogue,
            Walking,
            Attacking,
            JumpAttacking,
            SpreadAttacking,
            Dead
        }

        [Header("Stats")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float meleeRange = 3f;
        [SerializeField] private float midRange = 8f; // Range per attacco salto

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Normal Attack")]
        [SerializeField] private int normalAttackDamage = 1;
        [SerializeField] private float normalAttackCooldown = 2f;
        [SerializeField] private float normalAttackRange = 2.5f;

        [Header("Jump Attack")]
        [SerializeField] private float jumpHeight = 5f;
        [SerializeField] private float jumpDuration = 1f;
        [SerializeField] private float jumpAttackRadius = 4f;
        [SerializeField] private int jumpAttackDamage = 2;
        [SerializeField] private float jumpAttackCooldown = 5f;

        [Header("Spread Attack (Sferzata)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private int projectileCount = 5;
        [SerializeField] private float spreadAngle = 60f; // Angolo totale del ventaglio
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float spreadAttackCooldown = 4f;

        [Header("Visual Feedback")]
        [SerializeField] private float hitFlashDuration = 0.1f;
        [SerializeField] private Color hitColor = Color.red;

        [Header("Jump Attack Indicator")]
        [SerializeField] private GameObject aoeIndicatorPrefab; // Prefab opzionale (es. un cilindro rosso)
        [SerializeField] private float indicatorDuration = 0.8f; // Tempo che l'indicatore rimane visibile prima del salto
        [SerializeField] private Color indicatorColor = new Color(1f, 0f, 0f, 0.5f); // Rosso semi-trasparente

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Audio")]
        [SerializeField] private AudioClip idleSound;
        [SerializeField] private AudioClip walkSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip jumpAttackSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField] private AudioClip spreadAttackSound;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Intro Dialogue (Opzionale)")]
        [SerializeField] private bool hasIntroDialogue = false;
        [SerializeField] private string bossName = "Boss";
        [SerializeField][TextArea(2, 5)] private string[] dialogueLines;
        [SerializeField] private float dialogueLineDelay = 2f;
        [SerializeField] private AudioClip voiceSound;

        [Header("Dialogue UI")]
        [SerializeField] private GameObject dialogueUI;
        [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
        [SerializeField] private TMPro.TextMeshProUGUI bossNameText;

        // Animator hashes
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int JumpAttackHash = Animator.StringToHash("JumpAttack");
        private static readonly int SAttackHash = Animator.StringToHash("SAttack");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private int currentHealth;
        private BossState currentState = BossState.Idle;
        private Transform player;
        private PlayerHealth playerHealth;
        private AudioSource audioSource;
        private Renderer bossRenderer;
        private Color originalColor;

        private float lastNormalAttackTime;
        private float lastJumpAttackTime;
        private float lastSpreadAttackTime;
        private bool isAttacking = false;

        private Vector3 jumpStartPos;
        private Vector3 jumpTargetPos;
        private float initialGroundY; // Altezza Y iniziale del boss (terreno)
        private GameObject currentIndicator; // Indicatore AOE attivo

        // Dialogue
        private bool dialogueCompleted = false;
        private bool combatStarted = false;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentState == BossState.Dead;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnBossDeath;
        public event System.Action OnDialogueStarted;
        public event System.Action OnDialogueEnded;
        public event System.Action OnCombatStarted;

        private void Start()
        {
            currentHealth = maxHealth;

            // Salva l'altezza iniziale del boss (il terreno dove si trova)
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

            if (firePoint == null)
                firePoint = transform;

            // Inizializza cooldown per permettere attacchi subito
            lastNormalAttackTime = -normalAttackCooldown;
            lastJumpAttackTime = -jumpAttackCooldown;
            lastSpreadAttackTime = -spreadAttackCooldown;

            // Nascondi UI dialogo all'inizio
            if (dialogueUI != null)
            {
                dialogueUI.SetActive(false);
            }

            // Se non c'è dialogo, il combattimento può iniziare subito
            if (!hasIntroDialogue)
            {
                dialogueCompleted = true;
            }
        }

        private void Update()
        {
            if (currentState == BossState.Dead || player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Controlla se iniziare l'incontro con il dialogo
            if (!combatStarted && distanceToPlayer <= detectionRange)
            {
                StartEncounter();
                return;
            }

            // Se sta parlando, non fare nulla
            if (currentState == BossState.Dialogue) return;

            // Se il combattimento non è ancora iniziato, aspetta
            if (!combatStarted) return;

            // Fuori dal range di detection
            if (distanceToPlayer > detectionRange)
            {
                SetState(BossState.Idle);
                return;
            }

            // Se sta già attaccando, non fare nulla
            if (isAttacking) return;

            // Decidi cosa fare in base alla distanza
            DecideAction(distanceToPlayer);
        }

        private void DecideAction(float distanceToPlayer)
        {
            // Sempre guarda verso il player quando è nel range
            LookAtPlayer();

            // Se è molto vicino, attacco normale
            if (distanceToPlayer <= meleeRange)
            {
                if (CanUseNormalAttack())
                {
                    StartCoroutine(NormalAttack());
                }
                else
                {
                    // Aspetta in idle se l'attacco è in cooldown
                    SetState(BossState.Idle);
                }
            }
            // Range medio: sceglie tra salto e avvicinarsi
            else if (distanceToPlayer <= midRange)
            {
                // Probabilità di fare jump attack se disponibile
                if (CanUseJumpAttack() && Random.value < 0.4f)
                {
                    StartCoroutine(JumpAttack());
                }
                // Altrimenti avvicinati o fai spread attack
                else if (CanUseSpreadAttack() && Random.value < 0.3f)
                {
                    StartCoroutine(SpreadAttack());
                }
                else
                {
                    MoveTowardsPlayer();
                }
            }
            // Range lontano: spread attack o avvicinati
            else if (distanceToPlayer <= detectionRange)
            {
                if (CanUseSpreadAttack() && Random.value < 0.5f)
                {
                    StartCoroutine(SpreadAttack());
                }
                else if (CanUseJumpAttack() && Random.value < 0.3f)
                {
                    StartCoroutine(JumpAttack());
                }
                else
                {
                    MoveTowardsPlayer();
                }
            }
        }

        #region Dialogue

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
                // Reset parametri
                animator.SetBool(IsWalkingHash, false);

                switch (newState)
                {
                    case BossState.Walking:
                        animator.SetBool(IsWalkingHash, true);
                        break;
                }
            }
        }

        #region Normal Attack

        private bool CanUseNormalAttack()
        {
            return Time.time - lastNormalAttackTime >= normalAttackCooldown;
        }

        private IEnumerator NormalAttack()
        {
            isAttacking = true;
            SetState(BossState.Attacking);
            lastNormalAttackTime = Time.time;

            // Animazione e suono
            if (animator != null)
                animator.SetTrigger(AttackHash);
            PlaySound(attackSound);

            // Aspetta il momento del colpo (metà animazione circa)
            yield return new WaitForSeconds(0.3f);

            // Controlla se il player è ancora nel range
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= normalAttackRange && playerHealth != null)
            {
                playerHealth.TakeDamage(normalAttackDamage);
            }

            // Aspetta fine animazione
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }

        #endregion

        #region Jump Attack

        private bool CanUseJumpAttack()
        {
            return Time.time - lastJumpAttackTime >= jumpAttackCooldown;
        }

        private IEnumerator JumpAttack()
        {
            isAttacking = true;
            SetState(BossState.JumpAttacking);
            lastJumpAttackTime = Time.time;

            jumpStartPos = transform.position;

            // Posizione target: X e Z del player, Y uguale all'altezza iniziale del boss
            jumpTargetPos = new Vector3(player.position.x, initialGroundY, player.position.z);

            // Mostra l'indicatore a terra PRIMA del salto
            ShowAOEIndicator(jumpTargetPos);

            // Aspetta che il player veda l'indicatore
            yield return new WaitForSeconds(indicatorDuration);

            // Animazione e suono salto
            if (animator != null)
                animator.SetTrigger(JumpAttackHash);
            PlaySound(jumpAttackSound);

            // Fase di salto - un unico arco parabolico
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpDuration;

                // Movimento orizzontale lineare
                float x = Mathf.Lerp(jumpStartPos.x, jumpTargetPos.x, t);
                float z = Mathf.Lerp(jumpStartPos.z, jumpTargetPos.z, t);

                // Movimento verticale con arco parabolico (sin va da 0 a 0 passando per 1 al centro)
                float y = initialGroundY + Mathf.Sin(t * Mathf.PI) * jumpHeight;

                transform.position = new Vector3(x, y, z);

                yield return null;
            }

            // Atterraggio - posizione finale esatta (sempre all'altezza iniziale)
            transform.position = new Vector3(jumpTargetPos.x, initialGroundY, jumpTargetPos.z);
            PlaySound(landSound);

            // Rimuovi l'indicatore
            HideAOEIndicator();

            // Danno AOE
            DealAOEDamage();

            // Aspetta fine animazione
            yield return new WaitForSeconds(0.5f);

            isAttacking = false;
        }

        private void DealAOEDamage()
        {
            // Trova tutti i collider nel raggio
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, jumpAttackRadius);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerHealth ph = hitCollider.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(jumpAttackDamage);
                        Debug.Log("Boss Jump Attack ha colpito il player!");
                    }
                }
            }
        }

        private void ShowAOEIndicator(Vector3 position)
        {
            // Se c'è già un indicatore, rimuovilo
            HideAOEIndicator();

            if (aoeIndicatorPrefab != null)
            {
                // Usa il prefab personalizzato
                currentIndicator = Instantiate(aoeIndicatorPrefab, position, Quaternion.identity);
                // Scala in base al raggio AOE
                currentIndicator.transform.localScale = new Vector3(jumpAttackRadius * 2f, 0.1f, jumpAttackRadius * 2f);
            }
            else
            {
                // Crea un indicatore di default (cilindro appiattito)
                currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                currentIndicator.name = "AOE_Indicator";

                // Posiziona leggermente sopra il terreno per evitare z-fighting
                currentIndicator.transform.position = new Vector3(position.x, position.y + 0.05f, position.z);

                // Scala: diametro = raggio * 2, altezza molto bassa
                currentIndicator.transform.localScale = new Vector3(jumpAttackRadius * 2f, 0.02f, jumpAttackRadius * 2f);

                // Rimuovi il collider (non deve bloccare nulla)
                Collider col = currentIndicator.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Applica materiale rosso semi-trasparente
                Renderer rend = currentIndicator.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Crea un nuovo materiale trasparente
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = indicatorColor;

                    // Abilita trasparenza
                    mat.SetFloat("_Mode", 3); // Transparent mode
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

        #region Spread Attack

        private bool CanUseSpreadAttack()
        {
            return Time.time - lastSpreadAttackTime >= spreadAttackCooldown && projectilePrefab != null;
        }

        private IEnumerator SpreadAttack()
        {
            isAttacking = true;
            SetState(BossState.SpreadAttacking);
            lastSpreadAttackTime = Time.time;

            // Guarda verso il player
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            transform.rotation = Quaternion.LookRotation(dirToPlayer);

            // Animazione e suono
            if (animator != null)
                animator.SetTrigger(SAttackHash);
            PlaySound(spreadAttackSound);

            // Aspetta il momento dello sparo
            yield return new WaitForSeconds(0.4f);

            // Spara i proiettili a ventaglio
            FireSpreadProjectiles();

            // Aspetta fine animazione
            yield return new WaitForSeconds(0.6f);

            isAttacking = false;
        }

        private void FireSpreadProjectiles()
        {
            if (projectilePrefab == null) return;

            float startAngle = -spreadAngle / 2f;
            float angleStep = spreadAngle / (projectileCount - 1);

            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);

                // Calcola direzione ruotata
                Quaternion rotation = Quaternion.Euler(0f, currentAngle, 0f);
                Vector3 direction = rotation * transform.forward;

                // Crea proiettile
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

                // Configura il proiettile
                Projectile projScript = projectile.GetComponent<Projectile>();
                if (projScript != null)
                {
                    projScript.Initialize(direction, projectileSpeed);
                }
                else
                {
                    // Fallback: usa Rigidbody
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = direction * projectileSpeed;
                    }
                }
            }

            Debug.Log($"Boss ha sparato {projectileCount} proiettili a ventaglio!");
        }

        #endregion

        #region Damage & Death

        /// <summary>
        /// Il boss subisce un colpo
        /// </summary>
        public void TakeHit(int damage = 1)
        {
            if (currentState == BossState.Dead) return;

            // Invulnerabile durante il dialogo
            if (currentState == BossState.Dialogue) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"Boss colpito! Vita: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);

            // Feedback visivo e audio
            PlaySound(hurtSound);
            if (bossRenderer != null)
            {
                StartCoroutine(HitFlash());
            }

            // Morte
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

            Debug.Log("Boss sconfitto!");

            // Animazione e suono morte
            if (animator != null)
                animator.SetTrigger(DieHash);
            PlaySound(deathSound);

            OnBossDeath?.Invoke();

            // Aspetta animazione morte
            yield return new WaitForSeconds(2f);

            // Disabilita il boss
            gameObject.SetActive(false);
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

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Range di detection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Range melee
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            // Range medio (per jump attack)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Arancione
            Gizmos.DrawWireSphere(transform.position, midRange);

            // Range attacco normale
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, normalAttackRange);

            // Raggio AOE jump attack
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, jumpAttackRadius);

            // Visualizza il ventaglio dello spread attack
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 forward = transform.forward * 5f;

                float startAngle = -spreadAngle / 2f;
                Quaternion leftRot = Quaternion.Euler(0f, startAngle, 0f);
                Quaternion rightRot = Quaternion.Euler(0f, -startAngle, 0f);

                Gizmos.DrawLine(firePoint.position, firePoint.position + leftRot * forward);
                Gizmos.DrawLine(firePoint.position, firePoint.position + rightRot * forward);
            }
        }

        #endregion
    }
}
