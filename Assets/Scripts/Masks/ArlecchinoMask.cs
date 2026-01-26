using UnityEngine;

namespace Masks
{
    /// <summary>
    /// Maschera di Arlecchino: permette di fare un doppio salto.
    /// La logica del doppio salto è gestita nel PlayerController tramite jumpCount.
    /// </summary>
    public class ArlecchinoMask : MaskAbility
    {
        private void Awake()
        {
            maskType = MaskType.Arlecchino;
        }

        protected override void OnActivate()
        {
            Debug.Log("Arlecchino attivato - Doppio salto disponibile!");
        }

        protected override void OnDeactivate()
        {
            Debug.Log("Arlecchino disattivato");
        }
    }
}
