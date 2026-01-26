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

        [Header("Debug")]
        [SerializeField] private MaskType currentMaskType = MaskType.None;

        private Dictionary<MaskType, MaskAbility> maskDictionary = new Dictionary<MaskType, MaskAbility>();
        private MaskAbility currentAbility;
        private HashSet<MaskType> unlockedMasks = new HashSet<MaskType>();

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

            // Attiva la nuova maschera
            if (type != MaskType.None && maskDictionary.TryGetValue(type, out MaskAbility newAbility))
            {
                currentAbility = newAbility;
                currentAbility.Activate();
                currentMaskType = type;
                Debug.Log($"Maschera equipaggiata: {type}");
            }
            else
            {
                currentAbility = null;
                currentMaskType = MaskType.None;
                Debug.Log("Nessuna maschera equipaggiata");
            }

            OnMaskChanged?.Invoke(currentMaskType);
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
