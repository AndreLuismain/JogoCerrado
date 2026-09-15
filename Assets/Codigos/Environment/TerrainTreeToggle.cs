using UnityEngine;

namespace Cerrado.Environment
{
    /// <summary>
    /// Utilitário para alternar a exibição de árvores e folhagem no terreno durante o desenvolvimento.
    /// Útil para aliviar a performance no editor ao esculpir o relevo.
    /// </summary>
    [ExecuteInEditMode]
    public class TerrainTreeToggle : MonoBehaviour
    {
        [Tooltip("Quando marcado, oculta as árvores e folhagens de todos os terrenos")]
        [SerializeField] private bool hideFoliage = false;

        private void OnValidate()
        {
            ApplyToggle(hideFoliage);
        }

        private void OnEnable()
        {
            ApplyToggle(hideFoliage);
        }

        private void OnDisable()
        {
            ApplyToggle(false);
        }

        public void ApplyToggle(bool hide)
        {
            var terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            foreach (Terrain terrain in terrains)
            {
                terrain.drawTreesAndFoliage = !hide;
            }
        }
    }
}
