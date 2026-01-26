using UnityEngine;

namespace Masks
{
    /// <summary>
    /// Classe base astratta per tutte le abilità delle maschere.
    /// Ogni maschera eredita da questa classe e implementa il proprio comportamento.
    /// </summary>
    public abstract class MaskAbility : MonoBehaviour
    {
        [Header("Mask Info")]
        [SerializeField] protected MaskType maskType;

        protected bool isActive = false;

        public MaskType Type => maskType;
        public bool IsActive => isActive;

        /// <summary>
        /// Chiamato quando la maschera viene equipaggiata
        /// </summary>
        public virtual void Activate()
        {
            isActive = true;
            OnActivate();
        }

        /// <summary>
        /// Chiamato quando la maschera viene rimossa
        /// </summary>
        public virtual void Deactivate()
        {
            isActive = false;
            OnDeactivate();
        }

        /// <summary>
        /// Override per logica specifica all'attivazione
        /// </summary>
        protected abstract void OnActivate();

        /// <summary>
        /// Override per logica specifica alla disattivazione
        /// </summary>
        protected abstract void OnDeactivate();

        /// <summary>
        /// Chiamato ogni frame quando la maschera è attiva.
        /// Override per comportamenti continui (es. magnete).
        /// </summary>
        public virtual void UpdateAbility() { }

        /// <summary>
        /// Chiamato quando il player preme il tasto abilità.
        /// Override per abilità attive (es. attacco del Capitano).
        /// </summary>
        public virtual void UseAbility() { }

        /// <summary>
        /// Per abilità di movimento (es. doppio salto di Arlecchino).
        /// Ritorna true se l'abilità ha gestito il salto.
        /// </summary>
        public virtual bool TryJump() => false;
    }
}
