using UnityEngine;
using UnityEngine.AI;

namespace Cerrado.AI
{
    /// <summary>
    /// Controla os 53 BlendShapes da malha OncaParda (Suçuarana) extraída,
    /// ciclando suavemente a caminhada/corrida conforme a velocidade real do NavMeshAgent
    /// e mantendo pose de descanso natural quando parada.
    /// </summary>
    public class OncaBlendShapeController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private int idleBlendShapeIndex = 28;
        [SerializeField] private float idleBlendShapeWeight = 42f;

        [Header("Cadência de Animação")]
        [SerializeField] private float minCycleFps = 16f;
        [SerializeField] private float maxCycleFps = 44f;
        [SerializeField] private float speedToFpsFactor = 12.5f;

        private NavMeshAgent navMeshAgent;
        private int blendShapeCount;
        private float cyclePosition;
        private int lastPrimaryIndex = -1;
        private int lastSecondaryIndex = -1;
        private bool isIdle;
        private float breathPhase;

        private void Awake()
        {
            navMeshAgent = GetComponentInParent<NavMeshAgent>() ?? GetComponent<NavMeshAgent>();
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            }

            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                blendShapeCount = skinnedMeshRenderer.sharedMesh.blendShapeCount;
                idleBlendShapeIndex = Mathf.Clamp(idleBlendShapeIndex, 0, Mathf.Max(0, blendShapeCount - 1));
                ClearAllWeights();
                if (blendShapeCount > 0)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(idleBlendShapeIndex, idleBlendShapeWeight);
                    lastPrimaryIndex = idleBlendShapeIndex;
                    isIdle = true;
                }
            }

            breathPhase = Random.Range(0f, 6.28f);
        }

        private void ClearAllWeights()
        {
            if (skinnedMeshRenderer == null || blendShapeCount <= 0) return;
            for (int i = 0; i < blendShapeCount; i++)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
            }
            lastPrimaryIndex = -1;
            lastSecondaryIndex = -1;
        }

        private void Update()
        {
            if (skinnedMeshRenderer == null || blendShapeCount <= 0) return;

            float speed = (navMeshAgent != null && navMeshAgent.isOnNavMesh) ? navMeshAgent.velocity.magnitude : 0f;

            if (speed <= 0.12f)
            {
                if (!isIdle)
                {
                    ClearAllWeights();
                    isIdle = true;
                }

                // Respiração sutil em Idle ao redor da pose natural (shape 28)
                breathPhase += Time.deltaTime * 1.6f;
                float weight = Mathf.Clamp(idleBlendShapeWeight + Mathf.Sin(breathPhase) * 3.5f, 25f, 60f);
                skinnedMeshRenderer.SetBlendShapeWeight(idleBlendShapeIndex, weight);
                lastPrimaryIndex = idleBlendShapeIndex;
                return;
            }

            if (isIdle)
            {
                if (lastPrimaryIndex >= 0 && lastPrimaryIndex < blendShapeCount)
                    skinnedMeshRenderer.SetBlendShapeWeight(lastPrimaryIndex, 0f);
                isIdle = false;
            }

            // Zera apenas os 2 índices usados no frame anterior (O(1) sem alocações)
            if (lastPrimaryIndex >= 0 && lastPrimaryIndex < blendShapeCount)
                skinnedMeshRenderer.SetBlendShapeWeight(lastPrimaryIndex, 0f);
            if (lastSecondaryIndex >= 0 && lastSecondaryIndex < blendShapeCount)
                skinnedMeshRenderer.SetBlendShapeWeight(lastSecondaryIndex, 0f);

            float fps = Mathf.Clamp(speed * speedToFpsFactor, minCycleFps, maxCycleFps);
            cyclePosition = (cyclePosition + Time.deltaTime * fps) % blendShapeCount;

            int idxA = Mathf.FloorToInt(cyclePosition) % blendShapeCount;
            int idxB = (idxA + 1) % blendShapeCount;
            float frac = cyclePosition - Mathf.Floor(cyclePosition);

            skinnedMeshRenderer.SetBlendShapeWeight(idxA, (1f - frac) * 100f);
            skinnedMeshRenderer.SetBlendShapeWeight(idxB, frac * 100f);
            lastPrimaryIndex = idxA;
            lastSecondaryIndex = idxB;
        }
    }
}
