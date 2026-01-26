using UnityEngine;

namespace Masks
{
    /// <summary>
    /// Maschera di Pulcinella: rivela le piattaforme invisibili e permette di camminarci sopra.
    /// Le piattaforme invisibili devono avere il tag "InvisiblePlatform".
    /// </summary>
    public class PulcinellaMask : MaskAbility
    {
        [Header("Pulcinella Settings")]
        [SerializeField] private string invisiblePlatformTag = "InvisiblePlatform";
        [SerializeField] private float revealRange = 15f;

        private GameObject[] invisiblePlatforms;

        private void Awake()
        {
            maskType = MaskType.Pulcinella;
        }

        private void Start()
        {
            // All'avvio del gioco, nascondi tutte le piattaforme invisibili
            HideAllPlatforms();
        }

        private void HideAllPlatforms()
        {
            invisiblePlatforms = GameObject.FindGameObjectsWithTag(invisiblePlatformTag);

            foreach (var platform in invisiblePlatforms)
            {
                SetPlatformVisible(platform, false);
            }
        }

        protected override void OnActivate()
        {
            // Trova tutte le piattaforme invisibili e le rende visibili
            invisiblePlatforms = GameObject.FindGameObjectsWithTag(invisiblePlatformTag);

            foreach (var platform in invisiblePlatforms)
            {
                SetPlatformVisible(platform, true);
            }

            Debug.Log($"Pulcinella attivato - {invisiblePlatforms.Length} piattaforme rivelate");
        }

        protected override void OnDeactivate()
        {
            // Nasconde nuovamente le piattaforme
            if (invisiblePlatforms != null)
            {
                foreach (var platform in invisiblePlatforms)
                {
                    if (platform != null)
                    {
                        SetPlatformVisible(platform, false);
                    }
                }
            }

            Debug.Log("Pulcinella disattivato - piattaforme nascoste");
        }

        private void SetPlatformVisible(GameObject platform, bool visible)
        {
            // Mostra/nasconde il renderer
            Renderer renderer = platform.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
            }

            // Puoi anche cambiare il layer per il ground check
            // platform.layer = visible ? LayerMask.NameToLayer("Ground") : LayerMask.NameToLayer("InvisiblePlatform");
        }

        public override void UpdateAbility()
        {
            // Opzionale: rivela solo piattaforme entro un certo range
            // Utile per performance su mappe grandi
        }
    }
}
