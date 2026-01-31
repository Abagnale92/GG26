using UnityEngine;
using Enemies;

namespace Masks
{
    /// <summary>
    /// Maschera di Pulcinella: rivela le piattaforme e i nemici invisibili.
    /// Le piattaforme invisibili devono avere il tag "InvisiblePlatform".
    /// I nemici invisibili devono avere il tag "InvisibleEnemy".
    /// </summary>
    public class PulcinellaMask : MaskAbility
    {
        [Header("Pulcinella Settings")]
        [SerializeField] private string invisiblePlatformTag = "InvisiblePlatform";
        [SerializeField] private string invisibleEnemyTag = "InvisibleEnemy";
        [SerializeField] private float revealRange = 15f;

        [Header("Reveal Mode")]
        [Tooltip("Se attivo, le piattaforme e i nemici si vedono solo mentre si tiene premuto il tasto")]
        [SerializeField] private bool requireKeyToReveal = false;
        [SerializeField] private KeyCode revealKey = KeyCode.E;

        private GameObject[] invisiblePlatforms;
        private InvisibleEnemy[] invisibleEnemies;
        private bool isActive = false;
        private Transform playerTransform;

        private void Awake()
        {
            maskType = MaskType.Pulcinella;
            playerTransform = transform; // La maschera è sul player
        }

        private void Start()
        {
            // All'avvio del gioco, nascondi tutto
            HideAllInvisibles();
        }

        private void HideAllInvisibles()
        {
            // Nascondi piattaforme
            invisiblePlatforms = GameObject.FindGameObjectsWithTag(invisiblePlatformTag);
            foreach (var platform in invisiblePlatforms)
            {
                SetPlatformVisible(platform, false);
            }

            // Nascondi nemici (i nemici si nascondono da soli nello Start, ma per sicurezza)
            invisibleEnemies = FindObjectsByType<InvisibleEnemy>(FindObjectsSortMode.None);
        }

        protected override void OnActivate()
        {
            // Trova tutte le piattaforme e nemici invisibili
            invisiblePlatforms = GameObject.FindGameObjectsWithTag(invisiblePlatformTag);
            invisibleEnemies = FindObjectsByType<InvisibleEnemy>(FindObjectsSortMode.None);

            isActive = true;

            Debug.Log($"Pulcinella attivato - {invisiblePlatforms.Length} piattaforme, {invisibleEnemies.Length} nemici trovati");
        }

        protected override void OnDeactivate()
        {
            isActive = false;

            // Nasconde tutto quando si toglie la maschera
            HideAll();
            Debug.Log("Pulcinella disattivato - oggetti invisibili nascosti");
        }

        private void UpdateVisibilityByRange(bool forceReveal)
        {
            Vector3 playerPos = playerTransform.position;

            // Aggiorna visibilità piattaforme in base alla distanza
            if (invisiblePlatforms != null)
            {
                foreach (var platform in invisiblePlatforms)
                {
                    if (platform != null)
                    {
                        float distance = Vector3.Distance(playerPos, platform.transform.position);
                        bool shouldBeVisible = forceReveal && distance <= revealRange;
                        SetPlatformVisible(platform, shouldBeVisible);
                    }
                }
            }

            // Aggiorna visibilità nemici in base alla distanza
            if (invisibleEnemies != null)
            {
                foreach (var enemy in invisibleEnemies)
                {
                    if (enemy != null)
                    {
                        float distance = Vector3.Distance(playerPos, enemy.transform.position);
                        bool shouldBeVisible = forceReveal && distance <= revealRange;
                        enemy.SetVisible(shouldBeVisible);
                    }
                }
            }
        }

        private void HideAll()
        {
            // Nascondi piattaforme
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

            // Nascondi nemici
            if (invisibleEnemies != null)
            {
                foreach (var enemy in invisibleEnemies)
                {
                    if (enemy != null)
                    {
                        enemy.SetVisible(false);
                    }
                }
            }
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
            if (!isActive) return;

            // Determina se deve rivelare gli oggetti
            bool shouldReveal;

            if (requireKeyToReveal)
            {
                // Richiede il tasto: rivela solo se premuto
                shouldReveal = Input.GetKey(revealKey);
            }
            else
            {
                // Non richiede il tasto: rivela sempre mentre la maschera è attiva
                shouldReveal = true;
            }

            // Aggiorna la visibilità in base al range (ogni frame)
            UpdateVisibilityByRange(shouldReveal);
        }
    }
}
