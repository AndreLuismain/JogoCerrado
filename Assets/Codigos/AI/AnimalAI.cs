using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Cerrado.Player;

namespace Cerrado.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AnimalAI : MonoBehaviour
    {
        [Header("Temperamento e Velocidade")]
        [SerializeField] private AnimalTemperament temperament = AnimalTemperament.Docil;
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float fleeSpeed = 4.2f;
        [SerializeField] private float locomotionSpeedScale = 0.90f;

        [Header("Comportamento de Exploração (Wander)")]
        [SerializeField] private float wanderRadius = 15f;
        [SerializeField] private float minIdleDuration = 1.5f;
        [SerializeField] private float maxIdleDuration = 3.8f;

        [Header("Percepção do Jogador (Sensibilidade)")]
        [SerializeField] private float baseDetectionRadius = 12f;
        [SerializeField] private float alertDuration = 2.0f;
        [SerializeField] private float fleeDistance = 22f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Animações (Opcional)")]
        [SerializeField] private Animator animator;

        [Header("Ciclo Natural de Borda (Atravessar Cerca -> Árvores -> Outro Lado)")]
        [SerializeField] private bool enableBorderLifecycle = true;
        [SerializeField] private float fenceMinX = -44f;
        [SerializeField] private float fenceMaxX = 44f;
        [SerializeField] private float fenceMinZ = -34f;
        [SerializeField] private float fenceMaxZ = 47f;
        [SerializeField] private float recycleDistancePastFence = 5.0f;
        [SerializeField] private float respawnDistancePastFence = 6.5f;

        // Hashes de animação para otimização de performance
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int NormSpeedHash = Animator.StringToHash("NormSpeed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");
        private static readonly int IsFleeingHash = Animator.StringToHash("IsFleeing");

        // Componentes internos
        private NavMeshAgent agent;
        private Transform playerTransform;
        private PlayerController playerController;
        private Camera cachedCamera;

        // Estados
        private AnimalState currentState = AnimalState.Idle;
        private Vector3 spawnOrigin;
        private float stateTimer;
        private float effectiveDetectionRadius;
        private bool isRecycling;
        private float borderGraceTimer;

        public AnimalState CurrentState => currentState;
        public AnimalTemperament Temperament => temperament;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Evita qualquer conflito entre Rigidbody e NavMeshAgent que cause tremedeira ou quique
            var rbs = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }

            AnimalSpawner.ConfigureFenceLayerCollision();

            int animalLayer = LayerMask.NameToLayer("Animal");
            if (animalLayer >= 0)
            {
                gameObject.layer = animalLayer;
            }

            spawnOrigin = transform.position;
            cachedCamera = Camera.main;
            if (agent != null)
            {
                agent.baseOffset = 0f;
                agent.angularSpeed = 150f;
                agent.acceleration = 4.5f;
                agent.autoBraking = true;
                agent.speed = walkSpeed;
            }

            // Ajusta velocidades e percepções baseadas no temperamento
            ConfigureTemperament();
        }

        private void Start()
        {
            LocatePlayer();
            if (agent != null) agent.enabled = true;
            EnterState(Random.value > 0.30f ? AnimalState.Wander : AnimalState.Idle);
        }

        private void ConfigureTemperament()
        {
            float fallbackWalk = 1.5f;
            float fallbackFlee = 4.2f;

            switch (temperament)
            {
                case AnimalTemperament.Docil:
                    baseDetectionRadius = 8f;
                    fallbackWalk = 1.35f;
                    fallbackFlee = 3.2f;
                    break;
                case AnimalTemperament.Arisco:
                    baseDetectionRadius = 16f;
                    fallbackWalk = 2.0f;
                    fallbackFlee = 5.6f;
                    break;
                case AnimalTemperament.Cauteloso:
                    baseDetectionRadius = 14f;
                    fallbackWalk = 1.7f;
                    fallbackFlee = 4.5f;
                    break;
            }

            // Preserva as velocidades específicas de cada espécie (reduzidas para sincronizar passo e animação)
            if (walkSpeed <= 0.1f) walkSpeed = fallbackWalk;
            if (fleeSpeed <= 0.1f) fleeSpeed = fallbackFlee;

            float scale = Mathf.Clamp(locomotionSpeedScale, 0.4f, 1.0f);
            walkSpeed *= scale;
            fleeSpeed *= scale;

            if (agent != null)
            {
                agent.speed = walkSpeed;
            }
        }

        private void LocatePlayer()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    playerController = playerObj.GetComponent<PlayerController>();
                }
            }
        }

        private void Update()
        {
            if (isRecycling) return;

            if (borderGraceTimer > 0f)
            {
                borderGraceTimer -= Time.deltaTime;
            }

            if (playerTransform == null)
            {
                LocatePlayer();
            }

            CalculateSensoryPerception();
            UpdateStateMachine();
            UpdateAnimations();

            if (enableBorderLifecycle && borderGraceTimer <= 0f)
            {
                CheckBorderLifecycle();
            }
        }

        /// <summary>
        /// Calcula o alcance dinâmico de percepção do animal baseado nos passos do explorador
        /// </summary>
        private void CalculateSensoryPerception()
        {
            if (playerController == null)
            {
                effectiveDetectionRadius = baseDetectionRadius;
                return;
            }

            // Jogador correndo: passos pesados aumentam o raio do animal (+60%)
            if (playerController.IsSprinting)
            {
                effectiveDetectionRadius = baseDetectionRadius * 1.6f;
            }
            // Jogador agachado: passos furtivos diminuem muito o raio (-55%)
            else if (playerController.IsCrouching)
            {
                effectiveDetectionRadius = baseDetectionRadius * 0.45f;
            }
            else
            {
                effectiveDetectionRadius = baseDetectionRadius;
            }
        }

        private void UpdateStateMachine()
        {
            float distToPlayer = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;
            bool playerInSensoryRange = distToPlayer <= effectiveDetectionRadius && HasSightToPlayer();

            switch (currentState)
            {
                case AnimalState.Idle:
                    if (playerInSensoryRange)
                    {
                        EnterState(temperament == AnimalTemperament.Arisco ? AnimalState.Flee : AnimalState.Alert);
                        return;
                    }

                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        EnterState(AnimalState.Wander);
                    }
                    break;

                case AnimalState.Wander:
                    if (playerInSensoryRange)
                    {
                        EnterState(temperament == AnimalTemperament.Arisco ? AnimalState.Flee : AnimalState.Alert);
                        return;
                    }

                    // Chegou ao destino (somente se estiver posicionado em uma NavMesh ativa)
                    if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                    {
                        EnterState(AnimalState.Idle);
                    }
                    break;

                case AnimalState.Alert:
                    // Olha na direção do jogador
                    if (playerTransform != null)
                    {
                        Vector3 lookDir = (playerTransform.position - transform.position);
                        lookDir.y = 0;
                        if (lookDir.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                        }
                    }

                    // Se o jogador continuar se aproximando ou começar a correr, foge
                    if (playerInSensoryRange && (distToPlayer < (effectiveDetectionRadius * 0.7f) || (playerController != null && playerController.IsSprinting)))
                    {
                        EnterState(AnimalState.Flee);
                        return;
                    }

                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f || !playerInSensoryRange)
                    {
                        // O perigo passou, volta a andar
                        EnterState(AnimalState.Wander);
                    }
                    break;

                case AnimalState.Flee:
                    // Continua correndo até o timer de fuga terminar e estiver seguro
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f && distToPlayer > baseDetectionRadius * 1.5f)
                    {
                        EnterState(AnimalState.Idle);
                    }
                    break;
            }
        }

        private void EnterState(AnimalState newState)
        {
            currentState = newState;

            bool canNavigate = agent != null && agent.isOnNavMesh;

            switch (newState)
            {
                case AnimalState.Idle:
                    if (canNavigate) agent.isStopped = true;
                    if (agent != null) agent.speed = walkSpeed;
                    stateTimer = Random.Range(minIdleDuration, maxIdleDuration);
                    break;

                case AnimalState.Wander:
                    if (canNavigate)
                    {
                        agent.isStopped = false;
                        agent.speed = walkSpeed;
                        SetRandomWanderDestination();
                    }
                    break;

                case AnimalState.Alert:
                    if (canNavigate) agent.isStopped = true;
                    stateTimer = alertDuration;
                    break;

                case AnimalState.Flee:
                    if (canNavigate)
                    {
                        agent.isStopped = false;
                        agent.speed = fleeSpeed;
                        SetFleeDestination();
                    }
                    stateTimer = 4.5f; // Foge por 4.5 segundos
                    break;
            }
        }

        private void SetRandomWanderDestination()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnOrigin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        private void SetFleeDestination()
        {
            if (agent == null || !agent.isOnNavMesh || playerTransform == null) return;

            // Direção oposta ao jogador (permite atravessar a cerca e sumir nas árvores da borda)
            Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
            fleeDirection.y = 0f;
            if (fleeDirection.sqrMagnitude < 0.01f) fleeDirection = transform.forward;
            Vector3 fleeTarget = transform.position + (fleeDirection * fleeDistance);

            // Adiciona leve variação angular para a fuga parecer mais natural
            fleeTarget += Random.insideUnitSphere * 4f;
            fleeTarget.y = transform.position.y;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                SetRandomWanderDestination();
            }
        }

        private float GetDistanceOutsideFence(Vector3 pos)
        {
            float dx = Mathf.Max(0f, Mathf.Max(fenceMinX - pos.x, pos.x - fenceMaxX));
            float dz = Mathf.Max(0f, Mathf.Max(fenceMinZ - pos.z, pos.z - fenceMaxZ));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void CheckBorderLifecycle()
        {
            float pastFence = GetDistanceOutsideFence(transform.position);
            if (pastFence < recycleDistancePastFence) return;

            // Espera estar bem oculto entre as árvores ou fora da visão próxima do jogador
            bool farOrHidden = pastFence >= (recycleDistancePastFence + 3.5f);
            if (!farOrHidden && playerTransform != null)
            {
                float distPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distPlayer > 18f)
                {
                    farOrHidden = true;
                }
                else
                {
                    if (cachedCamera == null) cachedCamera = Camera.main;
                    if (cachedCamera != null)
                    {
                        Vector3 vp = cachedCamera.WorldToViewportPoint(transform.position + Vector3.up * 0.6f);
                        if (vp.z < 0f || vp.x < -0.1f || vp.x > 1.1f)
                            farOrHidden = true;
                    }
                }
            }

            if (farOrHidden)
            {
                StartCoroutine(RecycleToOppositeSideRoutine());
            }
        }

        private IEnumerator RecycleToOppositeSideRoutine()
        {
            isRecycling = true;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            var cols = GetComponentsInChildren<Collider>(true);
            foreach (var r in renderers) if (r != null) r.enabled = false;
            foreach (var c in cols) if (c != null) c.enabled = false;

            yield return new WaitForSeconds(Random.Range(0.75f, 2.0f));

            // Calcula ponto espelhado no cinturão de árvores do lado OPOSTO do mapa
            float midX = (fenceMinX + fenceMaxX) * 0.5f;
            float midZ = (fenceMinZ + fenceMaxZ) * 0.5f;
            Vector3 fromCenter = transform.position - new Vector3(midX, 0f, midZ);
            Vector2 oppDir = new Vector2(-fromCenter.x, -fromCenter.z);
            if (oppDir.sqrMagnitude < 1f) oppDir = Random.insideUnitCircle.normalized;
            oppDir.Normalize();

            float halfW = (fenceMaxX - fenceMinX) * 0.5f + respawnDistancePastFence;
            float halfH = (fenceMaxZ - fenceMinZ) * 0.5f + respawnDistancePastFence;
            float jitter = Random.Range(-10f, 10f);

            Vector3 oppositeEdge;
            if (Mathf.Abs(oppDir.x) > Mathf.Abs(oppDir.y))
            {
                oppositeEdge = new Vector3(
                    midX + Mathf.Sign(oppDir.x) * halfW,
                    transform.position.y,
                    Mathf.Clamp(midZ + oppDir.y * halfH + jitter, fenceMinZ + 4f, fenceMaxZ - 4f));
            }
            else
            {
                oppositeEdge = new Vector3(
                    Mathf.Clamp(midX + oppDir.x * halfW + jitter, fenceMinX + 4f, fenceMaxX - 4f),
                    transform.position.y,
                    midZ + Mathf.Sign(oppDir.y) * halfH);
            }

            if (NavMesh.SamplePosition(oppositeEdge, out NavMeshHit entryHit, 16f, NavMesh.AllAreas))
            {
                if (agent != null)
                {
                    agent.Warp(entryHit.position);
                }
                else
                {
                    transform.position = entryHit.position;
                }
            }

            // Novo ponto de retorno para dentro do campo aberto
            Vector3 inwardPoint = new Vector3(
                Mathf.Clamp(midX + oppDir.x * ((fenceMaxX - fenceMinX) * 0.30f) + Random.Range(-8f, 8f), fenceMinX + 8f, fenceMaxX - 8f),
                transform.position.y,
                Mathf.Clamp(midZ + oppDir.y * ((fenceMaxZ - fenceMinZ) * 0.30f) + Random.Range(-8f, 8f), fenceMinZ + 8f, fenceMaxZ - 8f));
            spawnOrigin = inwardPoint;

            foreach (var r in renderers) if (r != null) r.enabled = true;
            foreach (var c in cols) if (c != null) c.enabled = true;

            borderGraceTimer = 10.0f; // Garante tempo de entrada no campo antes de checar borda novamente
            isRecycling = false;

            currentState = AnimalState.Wander;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                if (NavMesh.SamplePosition(inwardPoint, out NavMeshHit destHit, 14f, NavMesh.AllAreas))
                {
                    agent.SetDestination(destHit.position);
                }
            }
        }

        private bool HasSightToPlayer()
        {
            if (playerTransform == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * 0.8f;
            Vector3 playerEyePos = playerTransform.position + Vector3.up * 1.2f;
            Vector3 dir = playerEyePos - eyePos;

            if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleLayers))
            {
                if (hit.transform != playerTransform && !hit.transform.IsChildOf(playerTransform))
                {
                    return false; // Obstruído por parede/rocha/morro
                }
            }
            return true;
        }

        private void UpdateAnimations()
        {
            if (animator == null || agent == null) return;

            float currentSpeed = agent.velocity.magnitude;
            bool isMoving = currentSpeed > 0.08f;
            float refWalk = Mathf.Max(0.45f, walkSpeed);
            float normSpeed = isMoving ? Mathf.Clamp(currentSpeed / refWalk, 0.72f, 1.35f) : 1.0f;

            animator.SetFloat(SpeedHash, currentSpeed, 0.08f, Time.deltaTime);
            animator.SetFloat(NormSpeedHash, normSpeed);
            animator.SetBool(IsMovingHash, isMoving);
            animator.SetBool(IsAlertHash, currentState == AnimalState.Alert);
            animator.SetBool(IsFleeingHash, currentState == AnimalState.Flee);
        }

        private void OnDrawGizmosSelected()
        {
            // Raio de percepção base
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, baseDetectionRadius);

            // Raio de exploração ao redor do ponto de origem
            Gizmos.color = Color.blue;
            Vector3 center = Application.isPlaying ? spawnOrigin : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);

            // Raio de fuga
            if (currentState == AnimalState.Flee)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, agent.destination);
            }
        }
    }
}

