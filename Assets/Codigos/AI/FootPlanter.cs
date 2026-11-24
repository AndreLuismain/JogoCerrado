using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cerrado.AI
{
    /// <summary>
    /// Alinha os animais e o NPC ao relevo real do terreno_NOVO em LateUpdate() via múltiplos Raycasts:
    /// ajusta suavemente a inclinação do corpo à normal do solo (Quaternion.Slerp) e fixa a altura Y
    /// das patas na superfície sem quique vertical nem balanço lateral rígido ("pinguim").
    /// </summary>
    public class FootPlanter : MonoBehaviour
    {
        [Header("Alvo Visual e Rigs")]
        [SerializeField] private Transform visualModel;
        [SerializeField] private bool hasSkeletalClips = true;

        [Header("Raycast e Alinhamento ao Terreno")]
        [SerializeField] private float rayOriginHeight = 2.5f;
        [SerializeField] private float rayDistance = 8.0f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundSnapSpeed = 16f;
        [SerializeField] private float normalSlerpSpeed = 6.5f;
        [SerializeField] private float maxSlopeTiltDegrees = 32f;
        [SerializeField] private float pawGroundSinkMargin = 0.015f;
        [SerializeField] private float footProbeSpread = 0.35f;

        [Header("Respiração Sutil em Idle (Apenas Sem Clips)")]
        [SerializeField] private float idleBreathingFrequency = 1.5f;
        [SerializeField] private float idleBreathingAmount = 0.005f;

        private NavMeshAgent agent;
        private Vector3 baseLocalPos;
        private Quaternion baseLocalRot;
        private Vector3 baseLocalScale;
        private Vector3 smoothedGroundNormal = Vector3.up;
        private float smoothedGroundOffsetY;
        private float timeOffset;
        private bool initialized;

        private static bool runtimeNavMeshChecked;
        private static NavMeshDataInstance runtimeNavMeshInstance;
        private static Collider cachedTerrainCollider;

        private void Awake()
        {
            EnsureRuntimeNavMeshExists();
            NeutralizeRigidbodies();

            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.baseOffset = 0f;
                agent.angularSpeed = 140f;
                agent.acceleration = 4.2f;
                agent.autoBraking = true;
            }

            ResolveVisualModel();

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
            NeutralizeRigidbodies();

            if (agent != null)
            {
                agent.baseOffset = 0f;
                if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            if (initialized && SampleMultiPointGround(out float groundY, out Vector3 groundNormal))
            {
                smoothedGroundOffsetY = Mathf.Clamp((groundY - transform.position.y) - pawGroundSinkMargin, -0.5f, 0.3f);
                smoothedGroundNormal = ClampSlopeNormal(groundNormal);
            }
        }

        private void ResolveVisualModel()
        {
            if (visualModel != null) return;

            Transform child = transform.Find("Modelo_3D");
            if (child != null)
            {
                visualModel = child;
                return;
            }

            if (transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }
        }

        private void NeutralizeRigidbodies()
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
            if (tri.vertices != null && tri.vertices.Length > 0) return;

            GameObject ground = GameObject.Find("Chao_Cerrado_Terreno_NOVO")
                ?? GameObject.Find("Chao_Cerrado_Superficie_Unica")
                ?? GameObject.Find("--- TERRENO_PLACEHOLDER ---");
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
            Bounds bounds = new Bounds(new Vector3(0f, 4f, 6.5f), new Vector3(126f, 28f, 120f));
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data != null)
            {
                runtimeNavMeshInstance = NavMesh.AddNavMeshData(data);
            }
        }

        private bool SampleSingleGroundPoint(Vector3 worldPos, out float hitY, out Vector3 hitNormal)
        {
            if (cachedTerrainCollider == null)
            {
                GameObject terrainObj = GameObject.Find("Chao_Cerrado_Terreno_NOVO") ?? GameObject.Find("--- TERRENO_PLACEHOLDER ---");
                if (terrainObj != null)
                {
                    cachedTerrainCollider = terrainObj.GetComponentInChildren<Collider>();
                }
            }

            Ray ray = new Ray(worldPos + Vector3.up * rayOriginHeight, Vector3.down);
            if (cachedTerrainCollider != null && cachedTerrainCollider.Raycast(ray, out RaycastHit directHit, rayDistance))
            {
                hitY = directHit.point.y;
                hitNormal = directHit.normal;
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            hitY = worldPos.y;
            hitNormal = Vector3.up;
            bool found = false;

            foreach (var h in hits)
            {
                if (h.collider == null || h.transform.IsChildOf(transform)) continue;
                string colName = h.collider.name;
                if (colName.Contains("Terreno") || colName.Contains("Chao") || colName.Contains("Ground"))
                {
                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        hitY = h.point.y;
                        hitNormal = h.normal;
                        found = true;
                    }
                }
            }

            return found;
        }

        private bool SampleMultiPointGround(out float avgY, out Vector3 avgNormal)
        {
            Vector3 center = transform.position;
            float spread = Mathf.Max(0.15f, footProbeSpread);

            Vector3[] probes =
            {
                center,
                center + transform.forward * spread,
                center - transform.forward * spread,
                center + transform.right * (spread * 0.55f),
                center - transform.right * (spread * 0.55f)
            };

            float ySum = 0f;
            Vector3 nSum = Vector3.zero;
            int validCount = 0;

            for (int i = 0; i < probes.Length; i++)
            {
                if (SampleSingleGroundPoint(probes[i], out float y, out Vector3 n))
                {
                    ySum += y;
                    nSum += n;
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                avgY = ySum / validCount;
                avgNormal = nSum.sqrMagnitude > 0.0001f ? nSum.normalized : Vector3.up;
                return true;
            }

            avgY = center.y;
            avgNormal = Vector3.up;
            return false;
        }

        private Vector3 ClampSlopeNormal(Vector3 rawNormal)
        {
            float angle = Vector3.Angle(Vector3.up, rawNormal);
            if (angle > maxSlopeTiltDegrees && angle > 0.01f)
            {
                return Vector3.Slerp(Vector3.up, rawNormal, maxSlopeTiltDegrees / angle).normalized;
            }
            return rawNormal.normalized;
        }

        private void LateUpdate()
        {
            if (!initialized || visualModel == null) return;

            if (SampleMultiPointGround(out float groundY, out Vector3 rawNormal))
            {
                float targetOffsetY = Mathf.Clamp((groundY - transform.position.y) - pawGroundSinkMargin, -0.5f, 0.3f);
                smoothedGroundOffsetY = Mathf.Lerp(smoothedGroundOffsetY, targetOffsetY, Time.deltaTime * groundSnapSpeed);

                Vector3 clampedNormal = ClampSlopeNormal(rawNormal);
                smoothedGroundNormal = Vector3.Slerp(smoothedGroundNormal, clampedNormal, Time.deltaTime * normalSlerpSpeed).normalized;
            }

            // Altura exata das patas cravada no terreno (zero quique em Y)
            visualModel.localPosition = new Vector3(baseLocalPos.x, baseLocalPos.y + smoothedGroundOffsetY, baseLocalPos.z);

            // Alinhamento suave à normal do terreno usando Quaternion.Slerp (zero balanço rígido "pinguim")
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, smoothedGroundNormal);
            if (forwardOnSlope.sqrMagnitude > 0.001f)
            {
                Quaternion targetWorldRot = Quaternion.LookRotation(forwardOnSlope.normalized, smoothedGroundNormal);
                Quaternion targetLocalSlope = Quaternion.Inverse(transform.rotation) * targetWorldRot;
                Quaternion targetVisualRot = targetLocalSlope * baseLocalRot;
                visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetVisualRot, Time.deltaTime * normalSlerpSpeed);
            }
            else
            {
                visualModel.localRotation = baseLocalRot;
            }

            if (hasSkeletalClips)
            {
                visualModel.localScale = baseLocalScale;
                return;
            }

            // Apenas para modelos sem AnimationClips esqueléticos (ex: Onça-parda estática leve),
            // aplica respiração sutil em Idle e extensão felina suave na passada (zero quique em Y e zero pinguim!)
            float speed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            if (speed <= 0.1f)
            {
                float breath = Mathf.Sin((Time.time + timeOffset) * idleBreathingFrequency);
                float scaleMod = 1f + (breath * idleBreathingAmount);
                visualModel.localScale = new Vector3(baseLocalScale.x, baseLocalScale.y * scaleMod, baseLocalScale.z);
            }
            else
            {
                float stridePhase = (Time.time + timeOffset) * Mathf.Clamp(speed * 2.8f, 4.0f, 11.0f);
                float stretchZ = 1f + Mathf.Sin(stridePhase) * 0.018f;
                float compressY = 1f - Mathf.Sin(stridePhase) * 0.009f;
                visualModel.localScale = new Vector3(baseLocalScale.x, baseLocalScale.y * compressY, baseLocalScale.z * stretchZ);
            }
        }
    }
}
