using UnityEngine;
using Cerrado.Data;

namespace Cerrado.Photography
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PhotographableTarget : MonoBehaviour
    {
        [Header("Dados da Espécie")]
        [Tooltip("ScriptableObject com as informações desta espécie")]
        [SerializeField] private SpeciesData speciesData;

        [Header("Pontos Focais")]
        [Tooltip("Ponto de foco para a câmera (se vazio, usa a posição deste objeto)")]
        [SerializeField] private Transform focalPointOverride;
        [SerializeField] private Vector3 focalPointOffset = Vector3.up * 0.5f;

        [Header("Comportamento")]
        [Tooltip("Se falso, a espécie não pode ser fotografada no momento (ex: escondida)")]
        [SerializeField] private bool canBePhotographed = true;

        public SpeciesData Species => speciesData;
        public bool CanBePhotographed => canBePhotographed && speciesData != null;

        public Vector3 FocalPoint
        {
            get
            {
                if (focalPointOverride != null)
                {
                    return focalPointOverride.position;
                }
                return transform.position + focalPointOffset;
            }
        }

        public float TargetRadius => speciesData != null ? speciesData.TargetRadius : 1.0f;

        public void SetPhotographable(bool status)
        {
            canBePhotographed = status;
        }

        /// <summary>
        /// Verifica se o alvo tem linha direta de visão para a câmera (sem pedras/paredes no caminho).
        /// </summary>
        public bool HasLineOfSight(Vector3 cameraPosition, LayerMask occlusionLayers)
        {
            Vector3 origin = cameraPosition;
            Vector3 target = FocalPoint;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, occlusionLayers))
            {
                // Se acertou algo que não seja parte deste objeto ou de seus filhos, há oclusão
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(FocalPoint, TargetRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(FocalPoint, 0.08f);
        }
    }
}

