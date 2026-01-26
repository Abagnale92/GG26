using UnityEngine;
using System.Collections.Generic;
using Enemies;

namespace Masks
{
    /// <summary>
    /// Componente da attaccare al prefab della spada.
    /// Gestisce la collisione con i nemici durante lo spin attack.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Sword : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string enemyTag = "Enemy";

        // Lista dei nemici già colpiti in questo attacco (evita colpi multipli)
        private HashSet<Enemy> hitEnemiesThisSwing = new HashSet<Enemy>();

        private void Awake()
        {
            // Assicurati che il collider sia trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            // Reset della lista quando la spada viene attivata per un nuovo attacco
            hitEnemiesThisSwing.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Controlla se è un nemico
            if (!other.CompareTag(enemyTag)) return;

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = other.GetComponentInParent<Enemy>();
            }

            if (enemy != null && !hitEnemiesThisSwing.Contains(enemy))
            {
                // Segna il nemico come colpito in questo swing
                hitEnemiesThisSwing.Add(enemy);

                // Infliggi il colpo
                enemy.TakeHit();
            }
        }

        /// <summary>
        /// Resetta la lista dei nemici colpiti (chiamato all'inizio di ogni attacco)
        /// </summary>
        public void ResetHitList()
        {
            hitEnemiesThisSwing.Clear();
        }
    }
}
