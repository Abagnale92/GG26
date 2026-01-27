using UnityEngine;
using UnityEngine.UI;
using Player;

namespace UI
{
    /// <summary>
    /// Mostra la vita del player con immagini (cuori).
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Transform heartsContainer; // Parent degli oggetti cuore

        [Header("Heart Settings")]
        [SerializeField] private GameObject heartPrefab; // Prefab con Image del cuore
        [SerializeField] private Sprite heartFull;
        [SerializeField] private Sprite heartEmpty; // Opzionale: cuore vuoto

        private Image[] heartImages;

        private void Start()
        {
            // Trova il PlayerHealth se non assegnato
            if (playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
            }

            if (playerHealth != null)
            {
                // Iscriviti all'evento di cambio vita
                playerHealth.OnHealthChanged += UpdateHealthUI;

                // Crea i cuori iniziali
                CreateHearts();
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthUI;
            }
        }

        private void CreateHearts()
        {
            // Rimuovi cuori esistenti
            if (heartsContainer != null)
            {
                foreach (Transform child in heartsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Crea nuovi cuori
            int maxHealth = playerHealth.MaxHealth;
            heartImages = new Image[maxHealth];

            for (int i = 0; i < maxHealth; i++)
            {
                GameObject heart;

                if (heartPrefab != null && heartsContainer != null)
                {
                    heart = Instantiate(heartPrefab, heartsContainer);
                }
                else if (heartsContainer != null)
                {
                    // Crea un cuore semplice se non c'è il prefab
                    heart = new GameObject($"Heart_{i}");
                    heart.transform.SetParent(heartsContainer);
                    Image img = heart.AddComponent<Image>();
                    img.sprite = heartFull;

                    RectTransform rt = heart.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(40, 40);
                }
                else
                {
                    continue;
                }

                heartImages[i] = heart.GetComponent<Image>();
            }

            // Aggiorna la visualizzazione
            UpdateHealthUI(playerHealth.CurrentHealth);
        }

        private void UpdateHealthUI(int currentHealth)
        {
            if (heartImages == null) return;

            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;

                if (i < currentHealth)
                {
                    // Cuore pieno
                    heartImages[i].sprite = heartFull;
                    heartImages[i].enabled = true;

                    if (heartEmpty == null)
                    {
                        heartImages[i].color = Color.white;
                    }
                }
                else
                {
                    // Cuore vuoto o nascosto
                    if (heartEmpty != null)
                    {
                        heartImages[i].sprite = heartEmpty;
                        heartImages[i].enabled = true;
                    }
                    else
                    {
                        // Se non c'è sprite vuoto, rendi semi-trasparente
                        heartImages[i].color = new Color(1, 1, 1, 0.3f);
                    }
                }
            }
        }
    }
}
