using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cerrado.AI
{
    /// <summary>
    /// Mantém as patas dos animais 100% apoiadas na malha real do terreno_NOVO via Raycast contínuo
    /// (compensando a folga vertical da NavMesh em ladeiras), alinha suavemente a inclinação do corpo
    /// à normal do solo (Vector3.Slerp) e elimina qualquer balanço lateral rígido ("pinguim") ou quique em Y.
    /// </summary>
    public class ProceduralAnimalMotion : MonoBehaviour
    {
        [Header("Alvo Visual (Filho 3D)")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private bool hasSkeletalClips;

        [Header("Contato com o Solo e Alinhamento ao Relevo (terreno_NOVO)")]
        [SerializeField] private float groundSnapSpeed = 14f;
        [SerializeField] private float pawGroundSinkMargin = 0.025f;
        [SerializeField] private float normalSlerpSpeed = 6.0f;
        [SerializeField] private float maxSlopeTiltDegrees = 32f;

        [Header("Respiração Sutil em Idle (Modelos Sem Rig)")]
        [SerializeField] private float breathingFrequency = 1.6f;
        [SerializeField] private float breathingScaleAmount = 0.006f;
        [SerializeField] private float idleLookYawDegrees = 1.6f;

        private NavMeshAgent agent;
        private Vector3 baseLocalPos;
        private Quaternion baseLocalRot;
        private Vector3 baseLocalScale;
        private Vector3 smoothedGroundNormal = Vector3.up;
        private Quaternion smoothedSlopeRotation = Quaternion.identity;
        private float smoothedGroundOffsetY;
        private float timeOffset;
        private bool initialized;

        private static bool runtimeNavMeshChecked;
        private static NavMeshDataInstance runtimeNavMeshInstance;
        private static Collider cachedTerrainCollider;

        private void Awake()
        {
            EnsureRuntimeNavMeshExists();
            NeutralizeAnyRigidbodies();

            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.baseOffset = 0f;
                agent.angularSpeed = 125f;
                agent.acceleration = 3.6f;
                agent.autoBraking = true;
            }

            if (visualModel == null)
            {
                Transform child = transform.Find("Modelo_3D");
                if (child != null)
                {
                    visualModel = child;
                }
                else if (transform.childCount > 0)
                {
                    visualModel = transform.GetChild(0);
                }
            }

            if (visualModel != null)
            {
                baseLocalPos = visualModel.localPosition;
                baseLocalRot = visualModel.localRotation;
                baseLocalScale = visualModel.localScale;
                initialized = true;
            }

            timeOffset = Random.Range(0f, 12f);
        }

        private void Start()
        {
            NeutralizeAnyRigidbodies();

            if (agent != null)
            {
                agent.baseOffset = 0f;
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
            }

            // Inicializa o contato exato das patas no primeiro frame
            if (initialized && SampleTerrainHeightAndNormal(out float groundY, out Vector3 groundNormal))
            {
                smoothedGroundOffsetY = Mathf.Clamp((groundY - transform.position.y) - pawGroundSinkMargin, -0.45f, 0.25f);
                smoothedGroundNormal = groundNormal;
            }
        }

        private void NeutralizeAnyRigidbodies()
        {
            var rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }

        private static void EnsureRuntimeNavMeshExists()
        {
            if (runtimeNavMeshChecked) return;
            runtimeNavMeshChecked = true;

            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices != null && tri.vertices.Length > 0)
            {
                return;
            }

            GameObject ground = GameObject.Find("Chao_Cerrado_Terreno_NOVO");
            if (ground == null) ground = GameObject.Find("Chao_Cerrado_Superficie_Unica");
            if (ground == null) ground = GameObject.Find("--- TERRENO_PLACEHOLDER ---");
            if (ground == null) return;

            var mf = ground.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix,
                    area = 0
                }
            };

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentSlope = 46f;
            settings.agentClimb = 0.5f;
            Bounds bounds = new Bounds(new Vector3(0f, 4f, 6.5f), new Vector3(88f, 28f, 82f));
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data != null)
            {
                runtimeNavMeshInstance = NavMesh.AddNavMeshData(data);
            }
        }

        /// <summary>
        /// Faz Raycast direto contra o MeshCollider do terreno_NOVO para obter a altura Y exata e a normal do relevo.
        /// </summary>
        private bool SampleTerrainHeightAndNormal(out float hitY, out Vector3 hitNormal)
        {
            if (cachedTerrainCollider == null)
            {
                GameObject terrainObj = GameObject.Find("Chao_Cerrado_Terreno_NOVO");
                if (terrainObj == null) terrainObj = GameObject.Find("--- TERRENO_PLACEHOLDER ---");
                if (terrainObj != null)
                {
                    cachedTerrainCollider = terrainObj.GetComponentInChildren<Collider>();
                }
            }

            Ray ray = new Ray(transform.position + Vector3.up * 3.0f, Vector3.down);

            if (cachedTerrainCollider != null && cachedTerrainCollider.Raycast(ray, out RaycastHit terrainHit, 10.0f))
            {
                hitY = terrainHit.point.y;
                hitNormal = terrainHit.normal;
                return true;
            }

            var hits = Physics.RaycastAll(ray, 10.0f);
            foreach (var h in hits)
            {
                if (h.collider != null && !h.transform.IsChildOf(transform) &&
                    (h.collider.name.Contains("Terreno") || h.collider.name.Contains("Chao")))
                {
                    hitY = h.point.y;
                    hitNormal = h.normal;
                    return true;
                }
            }

            hitY = transform.position.y;
            hitNormal = Vector3.up;
            return false;
        }

        private void LateUpdate()
        {
            if (!initialized || visualModel == null) return;

            // 1. Raycast contínuo para colar as patas exatamente na superfície do terreno_NOVO
            // (elimina o vão de 5cm a 18cm entre a malha simplificada da NavMesh e a malha real do terreno inclinado)
            if (SampleTerrainHeightAndNormal(out float groundY, out Vector3 rawNormal))
            {
                float targetOffsetY = Mathf.Clamp((groundY - transform.position.y) - pawGroundSinkMargin, -0.45f, 0.25f);
                smoothedGroundOffsetY = Mathf.Lerp(smoothedGroundOffsetY, targetOffsetY, Time.deltaTime * groundSnapSpeed);

                float angle = Vector3.Angle(Vector3.up, rawNormal);
                if (angle > maxSlopeTiltDegrees && angle > 0.01f)
                {
                    rawNormal = Vector3.Slerp(Vector3.up, rawNormal, maxSlopeTiltDegrees / angle).normalized;
                }
                smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, rawNormal, Time.deltaTime * normalSlerpSpeed).normalized;
            }

            // Posição Y das patas cravada no relevo (sem Math.Sin / sem quique!)
            visualModel.localPosition = new Vector3(baseLocalPos.x, baseLocalPos.y + smoothedGroundOffsetY, baseLocalPos.z);

            // 2. Alinhamento suave do corpo à inclinação do terreno (sem Z-roll / sem balanço de pinguim!)
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, smoothedGroundNormal);
            if (forwardOnSlope.sqrMagnitude > 0.001f)
            {
                Quaternion worldSlopeRot = Quaternion.LookRotation(forwardOnSlope.normalized, smoothedGroundNormal);
                Quaternion targetLocalSlope = Quaternion.Inverse(transform.rotation) * worldSlopeRot;
                smoothedSlopeRotation = Quaternion.Slerp(smoothedSlopeRotation, targetLocalSlope, Time.deltaTime * normalSlerpSpeed);
            }

            if (hasSkeletalClips)
            {
                visualModel.localRotation = smoothedSlopeRotation * baseLocalRot;
                visualModel.localScale = baseLocalScale;
                return;
            }

            // 3. Para modelos sem rig (ex.: Suçuarana e NPC): corpo estável e fluido ao caminhar (zero pinguim),
            // e respiração quase imperceptível quando parado em Idle:
            float speed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            if (speed > 0.12f)
            {
                visualModel.localRotation = smoothedSlopeRotation * baseLocalRot;
                visualModel.localScale = baseLocalScale;
            }
            else
            {
                float t = Time.time + timeOffset;
                float breath = Mathf.Sin(t * breathingFrequency);
                float scaleMod = 1f + (breath * breathingScaleAmount);
                float lookYaw = Mathf.Sin(t * 0.45f) * idleLookYawDegrees;

                visualModel.localRotation = smoothedSlopeRotation * Quaternion.Euler(0f, lookYaw, 0f) * baseLocalRot;
                visualModel.localScale = new Vector3(
                    baseLocalScale.x * scaleMod,
                    baseLocalScale.y * (1f + Mathf.Max(0f, breath) * breathingScaleAmount * 0.5f),
                    baseLocalScale.z
                );
            }
        }
    }
}
