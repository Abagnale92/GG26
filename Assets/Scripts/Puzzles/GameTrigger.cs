using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Puzzles
{
    /// <summary>
    /// Trigger generico che attiva eventi quando si verificano determinate condizioni.
    /// Supporta: passaggio del player, sconfitta nemici, collezionamento oggetti, timer, ecc.
    /// </summary>
    public class GameTrigger : MonoBehaviour
    {
        #region Enums

        public enum TriggerCondition
        {
            PlayerEnter,        // Il player entra nell'area trigger
            PlayerExit,         // Il player esce dall'area trigger
            PlayerStay,         // Il player rimane nell'area per X secondi
            EnemiesDefeated,    // Tutti i nemici specificati sono sconfitti
            ObjectsCollected,   // Tutti gli oggetti specificati sono raccolti/disattivati
            CustomCondition,    // Condizione custom controllata via script
            OnStart,            // Si attiva all'avvio della scena
            OnDelay,            // Si attiva dopo X secondi dall'avvio
            ButtonPressed,      // Si attiva quando si preme un tasto
            AllTriggersActive   // Si attiva quando altri GameTrigger sono tutti attivi
        }

        public enum TriggerAction
        {
            ActivateObject,     // Attiva un GameObject
            DeactivateObject,   // Disattiva un GameObject
            DestroyObject,      // Distrugge un GameObject
            PlayAnimation,      // Avvia un'animazione
            PlaySound,          // Riproduce un suono
            SpawnPrefab,        // Spawna un prefab
            LoadScene,          // Carica una scena
            CustomEvent         // Evento Unity personalizzato
        }

        #endregion

        #region Inspector Fields

        [Header("Trigger Settings")]
        [SerializeField] private TriggerCondition condition = TriggerCondition.PlayerEnter;
        [SerializeField] private bool triggerOnce = true; // Se true, si attiva solo una volta
        [SerializeField] private bool isActive = true; // Se false, il trigger è disabilitato
        [SerializeField] private float activationDelay = 0f; // Delay prima dell'attivazione

        [Header("Player Enter/Exit/Stay")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float stayDuration = 3f; // Per PlayerStay

        [Header("Enemies Defeated")]
        [SerializeField] private List<GameObject> enemiesToDefeat = new List<GameObject>();
        [SerializeField] private bool useEnemyTag = false;
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private bool checkEnemiesInArea = false; // Se true, controlla solo nemici nell'area del collider

        [Header("Objects Collected/Destroyed")]
        [SerializeField] private List<GameObject> objectsToCollect = new List<GameObject>();

        [Header("On Delay")]
        [SerializeField] private float startDelay = 5f;

        [Header("Button Pressed")]
        [SerializeField] private KeyCode triggerKey = KeyCode.E;
        [SerializeField] private bool requirePlayerInRange = true;

        [Header("All Triggers Active")]
        [SerializeField] private List<GameTrigger> requiredTriggers = new List<GameTrigger>();

        [Header("Action")]
        [SerializeField] private TriggerAction action = TriggerAction.CustomEvent;
        [SerializeField] private GameObject targetObject;
        [SerializeField] private string animationTrigger = "Activate";
        [SerializeField] private AudioClip soundToPlay;
        [SerializeField] private GameObject prefabToSpawn;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private string sceneToLoad;

        [Header("Events")]
        [SerializeField] private UnityEvent onTriggered;
        [SerializeField] private UnityEvent onConditionMet; // Quando la condizione diventa vera
        [SerializeField] private UnityEvent onConditionLost; // Quando la condizione diventa falsa (se applicabile)

        [Header("Trigger Audio")]
        [Tooltip("Suono riprodotto quando il trigger viene attivato (indipendente dall'azione)")]
        [SerializeField] private AudioClip triggerActivationSound;
        [SerializeField][Range(0f, 1f)] private float triggerSoundVolume = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        #endregion

        #region Private Fields

        private bool hasTriggered = false;
        private bool isPlayerInArea = false;
        private float stayTimer = 0f;
        private float delayTimer = 0f;
        private bool conditionMet = false;
        private AudioSource audioSource;
        private List<GameObject> trackedEnemies = new List<GameObject>();

        #endregion

        #region Properties

        public bool HasTriggered => hasTriggered;
        public bool IsConditionMet => conditionMet;
        public bool IsActiveAndEnabled => isActive && !hasTriggered;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (soundToPlay != null || triggerActivationSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Inizializza in base alla condizione
            switch (condition)
            {
                case TriggerCondition.OnStart:
                    StartCoroutine(TriggerWithDelay(activationDelay));
                    break;

                case TriggerCondition.OnDelay:
                    delayTimer = startDelay;
                    break;

                case TriggerCondition.EnemiesDefeated:
                    InitializeEnemyTracking();
                    break;
            }
        }

        private void Update()
        {
            if (!isActive || (triggerOnce && hasTriggered)) return;

            switch (condition)
            {
                case TriggerCondition.PlayerStay:
                    UpdatePlayerStay();
                    break;

                case TriggerCondition.EnemiesDefeated:
                    UpdateEnemiesDefeated();
                    break;

                case TriggerCondition.ObjectsCollected:
                    UpdateObjectsCollected();
                    break;

                case TriggerCondition.OnDelay:
                    UpdateDelay();
                    break;

                case TriggerCondition.ButtonPressed:
                    UpdateButtonPressed();
                    break;

                case TriggerCondition.AllTriggersActive:
                    UpdateAllTriggersActive();
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || (triggerOnce && hasTriggered)) return;

            if (other.CompareTag(playerTag))
            {
                isPlayerInArea = true;
                DebugLog($"Player entrato nell'area trigger");

                if (condition == TriggerCondition.PlayerEnter)
                {
                    StartCoroutine(TriggerWithDelay(activationDelay));
                }
            }

            // Traccia nemici che entrano nell'area (se checkEnemiesInArea)
            if (checkEnemiesInArea && condition == TriggerCondition.EnemiesDefeated)
            {
                if (useEnemyTag && other.CompareTag(enemyTag))
                {
                    if (!trackedEnemies.Contains(other.gameObject))
                    {
                        trackedEnemies.Add(other.gameObject);
                        DebugLog($"Nemico aggiunto al tracking: {other.gameObject.name}");
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isActive) return;

            if (other.CompareTag(playerTag))
            {
                isPlayerInArea = false;
                stayTimer = 0f;
                DebugLog($"Player uscito dall'area trigger");

                if (condition == TriggerCondition.PlayerExit && !(triggerOnce && hasTriggered))
                {
                    StartCoroutine(TriggerWithDelay(activationDelay));
                }
            }
        }

        #endregion

        #region Condition Updates

        private void UpdatePlayerStay()
        {
            if (isPlayerInArea)
            {
                stayTimer += Time.deltaTime;

                if (stayTimer >= stayDuration && !conditionMet)
                {
                    conditionMet = true;
                    onConditionMet?.Invoke();
                    StartCoroutine(TriggerWithDelay(activationDelay));
                }
            }
            else
            {
                if (conditionMet)
                {
                    conditionMet = false;
                    onConditionLost?.Invoke();
                }
                stayTimer = 0f;
            }
        }

        private void InitializeEnemyTracking()
        {
            // Se non usiamo il controllo area, traccia i nemici dalla lista
            if (!checkEnemiesInArea)
            {
                trackedEnemies = new List<GameObject>(enemiesToDefeat);
            }
            // Se usiamo tag e non area, trova tutti i nemici con quel tag
            else if (useEnemyTag && !checkEnemiesInArea)
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
                trackedEnemies = new List<GameObject>(enemies);
            }
        }

        private void UpdateEnemiesDefeated()
        {
            // Rimuovi nemici nulli (sconfitti/distrutti)
            trackedEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);

            // Se usiamo la lista specifica, controlla quella
            if (!checkEnemiesInArea && !useEnemyTag)
            {
                int aliveCount = 0;
                foreach (var enemy in enemiesToDefeat)
                {
                    if (enemy != null && enemy.activeInHierarchy)
                    {
                        aliveCount++;
                    }
                }

                if (aliveCount == 0 && enemiesToDefeat.Count > 0)
                {
                    if (!conditionMet)
                    {
                        conditionMet = true;
                        DebugLog("Tutti i nemici specificati sono stati sconfitti!");
                        onConditionMet?.Invoke();
                        StartCoroutine(TriggerWithDelay(activationDelay));
                    }
                }
            }
            else
            {
                // Usa i nemici tracciati
                if (trackedEnemies.Count == 0 && conditionMet == false)
                {
                    // Aspetta almeno un frame per assicurarsi che i nemici siano stati aggiunti
                    if (Time.frameCount > 2)
                    {
                        conditionMet = true;
                        DebugLog("Tutti i nemici nell'area sono stati sconfitti!");
                        onConditionMet?.Invoke();
                        StartCoroutine(TriggerWithDelay(activationDelay));
                    }
                }
            }
        }

        private void UpdateObjectsCollected()
        {
            int activeCount = 0;
            foreach (var obj in objectsToCollect)
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    activeCount++;
                }
            }

            if (activeCount == 0 && objectsToCollect.Count > 0 && !conditionMet)
            {
                conditionMet = true;
                DebugLog("Tutti gli oggetti sono stati raccolti!");
                onConditionMet?.Invoke();
                StartCoroutine(TriggerWithDelay(activationDelay));
            }
        }

        private void UpdateDelay()
        {
            delayTimer -= Time.deltaTime;

            if (delayTimer <= 0f && !hasTriggered)
            {
                StartCoroutine(TriggerWithDelay(0f));
            }
        }

        private void UpdateButtonPressed()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                if (!requirePlayerInRange || isPlayerInArea)
                {
                    StartCoroutine(TriggerWithDelay(activationDelay));
                }
            }
        }

        private void UpdateAllTriggersActive()
        {
            bool allActive = true;

            foreach (var trigger in requiredTriggers)
            {
                if (trigger == null || !trigger.HasTriggered)
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive && requiredTriggers.Count > 0 && !conditionMet)
            {
                conditionMet = true;
                DebugLog("Tutti i trigger richiesti sono attivi!");
                onConditionMet?.Invoke();
                StartCoroutine(TriggerWithDelay(activationDelay));
            }
        }

        #endregion

        #region Trigger Execution

        private System.Collections.IEnumerator TriggerWithDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (triggerOnce && hasTriggered) yield break;

            ExecuteTrigger();
        }

        private void ExecuteTrigger()
        {
            hasTriggered = true;
            DebugLog($"Trigger attivato! Azione: {action}");

            // Riproduci il suono di attivazione del trigger
            PlayTriggerSound();

            // Esegui l'azione
            switch (action)
            {
                case TriggerAction.ActivateObject:
                    if (targetObject != null)
                    {
                        targetObject.SetActive(true);
                    }
                    break;

                case TriggerAction.DeactivateObject:
                    if (targetObject != null)
                    {
                        targetObject.SetActive(false);
                    }
                    break;

                case TriggerAction.DestroyObject:
                    if (targetObject != null)
                    {
                        Destroy(targetObject);
                    }
                    break;

                case TriggerAction.PlayAnimation:
                    if (targetObject != null)
                    {
                        Animator animator = targetObject.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger(animationTrigger);
                        }
                    }
                    break;

                case TriggerAction.PlaySound:
                    if (soundToPlay != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(soundToPlay);
                    }
                    break;

                case TriggerAction.SpawnPrefab:
                    if (prefabToSpawn != null)
                    {
                        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
                        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
                        Instantiate(prefabToSpawn, pos, rot);
                    }
                    break;

                case TriggerAction.LoadScene:
                    if (!string.IsNullOrEmpty(sceneToLoad))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                    }
                    break;

                case TriggerAction.CustomEvent:
                    // Solo l'evento Unity
                    break;
            }

            // Invoca sempre l'evento custom
            onTriggered?.Invoke();
        }

        /// <summary>
        /// Riproduce il suono di attivazione del trigger
        /// </summary>
        private void PlayTriggerSound()
        {
            if (triggerActivationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(triggerActivationSound, triggerSoundVolume);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attiva manualmente il trigger (per CustomCondition o da altri script)
        /// </summary>
        public void Activate()
        {
            if (!isActive || (triggerOnce && hasTriggered)) return;

            StartCoroutine(TriggerWithDelay(activationDelay));
        }

        /// <summary>
        /// Resetta il trigger per poterlo riattivare
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            conditionMet = false;
            stayTimer = 0f;
            delayTimer = startDelay;

            if (condition == TriggerCondition.EnemiesDefeated)
            {
                InitializeEnemyTracking();
            }
        }

        /// <summary>
        /// Abilita/disabilita il trigger
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }

        /// <summary>
        /// Aggiunge un nemico alla lista da tracciare (runtime)
        /// </summary>
        public void AddEnemyToTrack(GameObject enemy)
        {
            if (!enemiesToDefeat.Contains(enemy))
            {
                enemiesToDefeat.Add(enemy);
            }
            if (!trackedEnemies.Contains(enemy))
            {
                trackedEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// Notifica che un nemico è stato sconfitto (alternativa al check automatico)
        /// </summary>
        public void NotifyEnemyDefeated(GameObject enemy)
        {
            trackedEnemies.Remove(enemy);
            enemiesToDefeat.Remove(enemy);
            DebugLog($"Nemico notificato come sconfitto: {enemy?.name}");
        }

        #endregion

        #region Debug

        private void DebugLog(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[GameTrigger - {gameObject.name}] {message}");
            }
        }

        private void OnDrawGizmos()
        {
            // Colore in base allo stato
            if (hasTriggered)
            {
                Gizmos.color = Color.green;
            }
            else if (conditionMet)
            {
                Gizmos.color = Color.yellow;
            }
            else
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            }

            // Disegna il collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }

            // Linee verso i nemici tracciati
            if (condition == TriggerCondition.EnemiesDefeated)
            {
                Gizmos.color = Color.red;
                foreach (var enemy in enemiesToDefeat)
                {
                    if (enemy != null)
                    {
                        Gizmos.DrawLine(transform.position, enemy.transform.position);
                        Gizmos.DrawWireSphere(enemy.transform.position, 0.5f);
                    }
                }
            }

            // Linee verso gli oggetti da raccogliere
            if (condition == TriggerCondition.ObjectsCollected)
            {
                Gizmos.color = Color.cyan;
                foreach (var obj in objectsToCollect)
                {
                    if (obj != null)
                    {
                        Gizmos.DrawLine(transform.position, obj.transform.position);
                        Gizmos.DrawWireSphere(obj.transform.position, 0.3f);
                    }
                }
            }

            // Linee verso trigger richiesti
            if (condition == TriggerCondition.AllTriggersActive)
            {
                Gizmos.color = Color.magenta;
                foreach (var trigger in requiredTriggers)
                {
                    if (trigger != null)
                    {
                        Gizmos.DrawLine(transform.position, trigger.transform.position);
                    }
                }
            }

            // Spawn point
            if (action == TriggerAction.SpawnPrefab && spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Mostra info più dettagliate quando selezionato
            Gizmos.color = Color.white;

            if (targetObject != null)
            {
                Gizmos.DrawLine(transform.position, targetObject.transform.position);
                Gizmos.DrawWireCube(targetObject.transform.position, Vector3.one * 0.5f);
            }
        }

        #endregion
    }
}
