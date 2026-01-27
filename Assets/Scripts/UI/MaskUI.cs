using UnityEngine;
using UnityEngine.UI;
using Player;
using Masks;

namespace UI
{
    /// <summary>
    /// Mostra la maschera attualmente equipaggiata.
    /// </summary>
    public class MaskUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MaskManager maskManager;
        [SerializeField] private Image currentMaskImage;

        [Header("Mask Sprites")]
        [SerializeField] private Sprite noMaskSprite;
        [SerializeField] private Sprite pulcinellaSprite;
        [SerializeField] private Sprite arlecchinoSprite;
        [SerializeField] private Sprite colombinaSprite;
        [SerializeField] private Sprite capitanoSprite;

        private void Start()
        {
            // Trova il MaskManager se non assegnato
            if (maskManager == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    maskManager = player.GetComponent<MaskManager>();
                }
            }

            if (maskManager != null)
            {
                // Iscriviti all'evento di cambio maschera
                maskManager.OnMaskChanged += UpdateMaskUI;

                // Aggiorna subito con la maschera corrente
                UpdateMaskUI(maskManager.CurrentMaskType);
            }
        }

        private void OnDestroy()
        {
            if (maskManager != null)
            {
                maskManager.OnMaskChanged -= UpdateMaskUI;
            }
        }

        private void UpdateMaskUI(MaskType maskType)
        {
            if (currentMaskImage == null) return;

            Sprite newSprite = GetSpriteForMask(maskType);

            // Se la maschera non ha sprite, mantieni l'immagine corrente
            if (newSprite != null)
            {
                currentMaskImage.sprite = newSprite;
                currentMaskImage.enabled = true;
            }
            else if (maskType == MaskType.None && noMaskSprite != null)
            {
                currentMaskImage.sprite = noMaskSprite;
                currentMaskImage.enabled = true;
            }
            // Se non c'è sprite per questa maschera, non cambiare l'immagine
        }

        private Sprite GetSpriteForMask(MaskType maskType)
        {
            switch (maskType)
            {
                case MaskType.Pulcinella:
                    return pulcinellaSprite;
                case MaskType.Arlecchino:
                    return arlecchinoSprite;
                case MaskType.Colombina:
                    return colombinaSprite;
                case MaskType.Capitano:
                    return capitanoSprite;
                case MaskType.None:
                    return noMaskSprite;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Imposta lo sprite per una maschera specifica (utile per assegnare a runtime)
        /// </summary>
        public void SetMaskSprite(MaskType maskType, Sprite sprite)
        {
            switch (maskType)
            {
                case MaskType.Pulcinella:
                    pulcinellaSprite = sprite;
                    break;
                case MaskType.Arlecchino:
                    arlecchinoSprite = sprite;
                    break;
                case MaskType.Colombina:
                    colombinaSprite = sprite;
                    break;
                case MaskType.Capitano:
                    capitanoSprite = sprite;
                    break;
                case MaskType.None:
                    noMaskSprite = sprite;
                    break;
            }

            // Aggiorna se è la maschera corrente
            if (maskManager != null && maskManager.CurrentMaskType == maskType)
            {
                UpdateMaskUI(maskType);
            }
        }
    }
}
