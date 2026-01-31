using UnityEngine;
using System.Collections.Generic;
using Masks;

namespace Player
{
    /// <summary>
    /// Gestisce l'inventario delle maschere e il cambio tra di esse.
    /// </summary>
    public class MaskManager : MonoBehaviour
    {
        [Header("Maschere Disponibili")]
        [SerializeField] private List<MaskAbility> availableMasks = new List<MaskAbility>();

        [Header("Visual Masks (GameObject sul viso)")]
        [SerializeField] private Transform maskAttachPoint; // Punto dove attaccare la maschera (es. testa)
        [SerializeField] private GameObject pulcinellaMaskPrefab;
        [SerializeField] private GameObject arlecchinoMaskPrefab;
        [SerializeField] private GameObject colombinaMaskPrefab;
        [SerializeField] private GameObject capitanoMaskPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip pulcinellaMaskSound;
        [SerializeField] private AudioClip arlecchinoMaskSound;
        [SerializeField] private AudioClip colombinaMaskSound;
        [SerializeField] private AudioClip capitanoMaskSound;
        [SerializeField] private AudioClip removeMaskSound;

        [Header("Debug")]
        [SerializeField] private MaskType currentMaskType = MaskType.None;

        private Dictionary<MaskType, MaskAbility> maskDictionary = new Dictionary<MaskType, MaskAbility>();
        private MaskAbility currentAbility;
        private HashSet<MaskType> unlockedMasks = new HashSet<MaskType>();
        private GameObject currentVisualMask; // La maschera visuale attualmente indossata
        private AudioSource audioSource;

        public MaskAbility CurrentAbility => currentAbility;
        public MaskType CurrentMaskType => currentMaskType;

        public event System.Action<MaskType> OnMaskChanged;

        private void Awake()
        {
            // Popola il dizionario con le maschere configurate
            foreach (var mask in availableMasks)
            {
                if (mask != null && !maskDictionary.ContainsKey(mask.Type))
                {
                    maskDictionary[mask.Type] = mask;
                    mask.Deactivate();
                }
            }

            // Ottieni o crea AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            // Aggiorna l'abilità corrente ogni frame
            currentAbility?.UpdateAbility();
        }

        /// <summary>
        /// Sblocca una maschera (chiamato quando il player raccoglie una maschera)
        /// </summary>
        public void UnlockMask(MaskType type)
        {
            if (type == MaskType.None) return;

            unlockedMasks.Add(type);
            Debug.Log($"Maschera sbloccata: {type}");
        }

        /// <summary>
        /// Controlla se una maschera è stata sbloccata
        /// </summary>
        public bool IsMaskUnlocked(MaskType type)
        {
            return unlockedMasks.Contains(type);
        }

        /// <summary>
        /// Equipaggia una maschera specifica
        /// </summary>
        public void EquipMask(MaskType type)
        {
            // Non fare nulla se è la stessa maschera
            if (type == currentMaskType) return;

            // Controlla se la maschera è sbloccata (skip per debug o se vuoi permettere sempre)
            if (type != MaskType.None && !unlockedMasks.Contains(type))
            {
                Debug.Log($"Maschera {type} non ancora sbloccata!");
                return;
            }

            // Disattiva la maschera corrente
            currentAbility?.Deactivate();

            // Rimuovi la maschera visuale corrente
            RemoveVisualMask();

            // Attiva la nuova maschera
            if (type != MaskType.None && maskDictionary.TryGetValue(type, out MaskAbility newAbility))
            {
                currentAbility = newAbility;
                currentAbility.Activate();
                currentMaskType = type;

                // Mostra la maschera visuale
                ShowVisualMask(type);

                // Riproduci suono specifico della maschera
                PlayMaskSound(type);

                Debug.Log($"Maschera equipaggiata: {type}");
            }
            else
            {
                currentAbility = null;
                currentMaskType = MaskType.None;

                // Riproduci suono rimozione (se presente)
                PlaySound(removeMaskSound);

                Debug.Log("Nessuna maschera equipaggiata");
            }

            OnMaskChanged?.Invoke(currentMaskType);
        }

        /// <summary>
        /// Mostra la maschera visuale sul viso del player
        /// </summary>
        private void ShowVisualMask(MaskType type)
        {
            GameObject prefab = GetMaskPrefab(type);
            if (prefab == null) return;

            Transform parent = maskAttachPoint != null ? maskAttachPoint : transform;
            currentVisualMask = Instantiate(prefab, parent);
            currentVisualMask.transform.localPosition = Vector3.zero;
            currentVisualMask.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Rimuove la maschera visuale corrente
        /// </summary>
        private void RemoveVisualMask()
        {
            if (currentVisualMask != null)
            {
                Destroy(currentVisualMask);
                currentVisualMask = null;
            }
        }

        /// <summary>
        /// Ottiene il prefab della maschera per il tipo specificato
        /// </summary>
        private GameObject GetMaskPrefab(MaskType type)
        {
            switch (type)
            {
                case MaskType.Pulcinella:
                    return pulcinellaMaskPrefab;
                case MaskType.Arlecchino:
                    return arlecchinoMaskPrefab;
                case MaskType.Colombina:
                    return colombinaMaskPrefab;
                case MaskType.Capitano:
                    return capitanoMaskPrefab;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Riproduce un suono
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Riproduce il suono specifico della maschera
        /// </summary>
        private void PlayMaskSound(MaskType type)
        {
            AudioClip clip = GetMaskSound(type);
            PlaySound(clip);
        }

        /// <summary>
        /// Ottiene il suono della maschera per il tipo specificato
        /// </summary>
        private AudioClip GetMaskSound(MaskType type)
        {
            switch (type)
            {
                case MaskType.Pulcinella:
                    return pulcinellaMaskSound;
                case MaskType.Arlecchino:
                    return arlecchinoMaskSound;
                case MaskType.Colombina:
                    return colombinaMaskSound;
                case MaskType.Capitano:
                    return capitanoMaskSound;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Rimuove la maschera corrente
        /// </summary>
        public void RemoveMask()
        {
            EquipMask(MaskType.None);
        }

        /// <summary>
        /// Cicla alla prossima maschera sbloccata
        /// </summary>
        public void CycleNextMask()
        {
            if (unlockedMasks.Count == 0)
            {
                RemoveMask();
                return;
            }

            // Crea lista ordinata delle maschere sbloccate
            List<MaskType> unlockedList = new List<MaskType>(unlockedMasks);
            unlockedList.Sort();

            // Trova l'indice corrente e passa al prossimo
            int currentIndex = unlockedList.IndexOf(currentMaskType);
            int nextIndex = (currentIndex + 1) % (unlockedList.Count + 1); // +1 per includere "None"

            if (nextIndex == unlockedList.Count)
            {
                RemoveMask();
            }
            else
            {
                EquipMask(unlockedList[nextIndex]);
            }
        }

        /// <summary>
        /// Registra una nuova abilità maschera a runtime
        /// </summary>
        public void RegisterMask(MaskAbility ability)
        {
            if (ability != null && !maskDictionary.ContainsKey(ability.Type))
            {
                maskDictionary[ability.Type] = ability;
                availableMasks.Add(ability);
            }
        }
    }
}
