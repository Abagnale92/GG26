using UnityEngine;
using System;

namespace Interaction
{
    /// <summary>
    /// Oggetto con cui il player può interagire per visualizzare testo.
    /// Usato per NPC, cartelli, dialoghi pre-boss, ecc.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        [Header("Dialogue")]
        [SerializeField][TextArea(3, 10)] private string[] dialogueLines;
        [SerializeField] private float textDisplayTime = 3f; // Se 0, avanza con tasto

        [Header("Item Reward")]
        [SerializeField] private bool givesItem = false;
        [SerializeField] private GameObject itemToGive;
        [SerializeField] private Vector3 itemSpawnOffset = new Vector3(1f, 0f, 0f);

        [Header("NPC Settings")]
        [SerializeField] private string npcName; // Nome del personaggio (opzionale)
        [SerializeField] private AudioClip voiceSound; // Suono della voce quando parla

        [Header("Visual Feedback")]
        [SerializeField] private GameObject interactionPrompt; // UI che mostra "Premi F"
        [SerializeField] private GameObject dialogueUI; // UI per il testo
        [SerializeField] private TMPro.TextMeshProUGUI dialogueText; // Riferimento al testo dialogo
        [SerializeField] private TMPro.TextMeshProUGUI npcNameText; // Riferimento al testo nome NPC

        private Transform player;
        private bool isPlayerInRange = false;
        private bool isInteracting = false;
        private int currentLineIndex = 0;
        private bool hasGivenItem = false;
        private float lineTimer = 0f;
        private AudioSource audioSource;

        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action OnItemGiven;

        private void Start()
        {
            // Trova il player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }

            // Nascondi le UI all'inizio
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            if (dialogueUI != null)
            {
                dialogueUI.SetActive(false);
            }

            // Configura AudioSource per la voce
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && voiceSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= interactionRange;

            // Mostra/nascondi prompt di interazione
            if (isPlayerInRange && !wasInRange && !isInteracting)
            {
                ShowInteractionPrompt(true);
            }
            else if (!isPlayerInRange && wasInRange && !isInteracting)
            {
                ShowInteractionPrompt(false);
            }

            // Gestisci input
            if (isPlayerInRange && Input.GetKeyDown(interactKey))
            {
                if (!isInteracting)
                {
                    StartDialogue();
                }
                else if (textDisplayTime <= 0) // Avanza solo se non c'è timer automatico
                {
                    NextLine();
                }
            }

            // Timer per avanzamento automatico
            if (isInteracting && textDisplayTime > 0)
            {
                lineTimer -= Time.deltaTime;
                if (lineTimer <= 0)
                {
                    NextLine();
                }
            }
        }

        private void ShowInteractionPrompt(bool show)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
            }
            else if (show)
            {
                Debug.Log($"Premi {interactKey} per interagire con {gameObject.name}");
            }
        }

        private void StartDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name}: Nessun dialogo configurato!");
                return;
            }

            isInteracting = true;
            currentLineIndex = 0;
            ShowInteractionPrompt(false);

            if (dialogueUI != null)
            {
                dialogueUI.SetActive(true);
            }

            // Mostra il nome dell'NPC se configurato
            if (npcNameText != null)
            {
                npcNameText.text = !string.IsNullOrEmpty(npcName) ? npcName : "";
            }

            // Riproduci suono della voce
            PlayVoiceSound();

            OnDialogueStarted?.Invoke();
            ShowCurrentLine();
        }

        private void PlayVoiceSound()
        {
            if (voiceSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(voiceSound);
            }
        }

        private void ShowCurrentLine()
        {
            if (currentLineIndex < dialogueLines.Length)
            {
                string line = dialogueLines[currentLineIndex];

                if (dialogueText != null)
                {
                    dialogueText.text = line;
                }
                else
                {
                    Debug.Log($"[{gameObject.name}]: {line}");
                }

                lineTimer = textDisplayTime;
            }
        }

        private void NextLine()
        {
            currentLineIndex++;

            if (currentLineIndex >= dialogueLines.Length)
            {
                EndDialogue();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        private void EndDialogue()
        {
            isInteracting = false;

            if (dialogueUI != null)
            {
                dialogueUI.SetActive(false);
            }

            // Dai l'oggetto se configurato e non ancora dato
            if (givesItem && !hasGivenItem && itemToGive != null)
            {
                GiveItem();
            }

            OnDialogueEnded?.Invoke();

            // Mostra di nuovo il prompt se il player è ancora nel range
            if (isPlayerInRange)
            {
                ShowInteractionPrompt(true);
            }
        }

        private void GiveItem()
        {
            Vector3 spawnPos = transform.position + itemSpawnOffset;
            Instantiate(itemToGive, spawnPos, Quaternion.identity);
            hasGivenItem = true;

            Debug.Log($"{gameObject.name} ha donato un oggetto!");
            OnItemGiven?.Invoke();
        }

        /// <summary>
        /// Forza la fine del dialogo
        /// </summary>
        public void ForceEndDialogue()
        {
            if (isInteracting)
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Resetta lo stato (utile per respawn o nuovo gioco)
        /// </summary>
        public void ResetState()
        {
            hasGivenItem = false;
            isInteracting = false;
            currentLineIndex = 0;
        }

        private void OnDrawGizmosSelected()
        {
            // Range di interazione
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Posizione spawn oggetto
            if (givesItem)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + itemSpawnOffset, 0.3f);
            }
        }
    }
}
