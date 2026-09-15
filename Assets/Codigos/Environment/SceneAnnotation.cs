using System;
using UnityEngine;

namespace Cerrado.Environment
{
    /// <summary>
    /// Componente de anotação de cenário (Ponto de Interesse / Marcador) para o mapa do Cerrado.
    /// Permite registrar notas de desenvolvimento e navegar pela câmera no Scene View.
    /// </summary>
    [ExecuteInEditMode]
    public class SceneAnnotation : MonoBehaviour, IComparable<SceneAnnotation>
    {
        [Tooltip("Título do ponto de interesse (ex: Toca do Tamanduá, Mirante do Sol, etc.)")]
        public string headline = "Novo Ponto de Interesse";

        [Tooltip("Texto ou notas opcionais")]
        public TextAsset textAsset;

        [Tooltip("Ordem de navegação")]
        public int id;

        public void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var xform = transform;
            Gizmos.color = new Color(0.95f, 0.65f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(xform.position, 0.6f);
            Gizmos.matrix = xform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.forward, 45f, 1.5f, 0.2f, 1.33f);
#endif
        }

        public int CompareTo(SceneAnnotation other)
        {
            if (other == null) return 1;
            var idx = id.CompareTo(other.id);
            if (idx == 0) idx = string.Compare(headline, other.headline, StringComparison.Ordinal);
            return idx;
        }
    }
}
