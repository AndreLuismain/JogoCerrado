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
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float fleeSpeed = 6.5f;

        [Header("Comportamento de Exploração (Wander)")]
        [SerializeField] private float wanderRadius = 15f;
        [SerializeField] private float minIdleDuration = 2.5f;
        [SerializeField] private float maxIdleDuration = 6.0f;

        [Header("Percepção do Jogador (Sensibilidade)")]
        [SerializeField] private float baseDetectionRadius = 12f;
        [SerializeField] private float alertDuration = 2.0f;
        [SerializeField] private float fleeDistance = 22f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Animações (Opcional)")]
        [SerializeField] private Animator animator;

        // Hashes de animação para otimização de performance
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");
        private static readonly int IsFleeingHash = Animator.StringToHash("IsFleeing");

        // Componentes internos
        private NavMeshAgent agent;
        private Transform playerTransform;
        private PlayerController playerController;

        // Estados
        private AnimalState currentState = AnimalState.Idle;
        private Vector3 spawnOrigin;
        private float stateTimer;
        private float effectiveDetectionRadius;

        public AnimalState CurrentState => currentState;
        public AnimalTemperament Temperament => temperament;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            spawnOrigin = transform.position;
            agent.speed = walkSpeed;

            // Ajusta velocidades e percepções baseadas no temperamento
            ConfigureTemperament();
        }

        private void Start()
        {
            LocatePlayer();
            EnterState(AnimalState.Idle);
        }

        private void ConfigureTemperament()
        {
            switch (temperament)
            {
                case AnimalTemperament.Docil:
                    baseDetectionRadius = 8f;
                    walkSpeed = 1.8f;
                    fleeSpeed = 4.5f;
                    break;
                case AnimalTemperament.Arisco:
                    baseDetectionRadius = 16f;
                    walkSpeed = 2.8f;
                    fleeSpeed = 8.5f;
                    break;
                case AnimalTemperament.Cauteloso:
                    baseDetectionRadius = 14f;
                    walkSpeed = 2.4f;
                    fleeSpeed = 6.5f;
                    break;
            }
            agent.speed = walkSpeed;
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
            if (playerTransform == null)
            {
                LocatePlayer();
            }

            CalculateSensoryPerception();
            UpdateStateMachine();
            UpdateAnimations();
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

                    // Chegou ao destino
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
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

            switch (newState)
            {
                case AnimalState.Idle:
                    agent.isStopped = true;
                    agent.speed = walkSpeed;
                    stateTimer = Random.Range(minIdleDuration, maxIdleDuration);
                    break;

                case AnimalState.Wander:
                    agent.isStopped = false;
                    agent.speed = walkSpeed;
                    SetRandomWanderDestination();
                    break;

                case AnimalState.Alert:
                    agent.isStopped = true;
                    stateTimer = alertDuration;
                    break;

                case AnimalState.Flee:
                    agent.isStopped = false;
                    agent.speed = fleeSpeed;
                    stateTimer = 4.5f; // Foge por 4.5 segundos
                    SetFleeDestination();
                    break;
            }
        }

        private void SetRandomWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnOrigin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        private void SetFleeDestination()
        {
            if (playerTransform == null) return;

            // Direção oposta ao jogador
            Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
            Vector3 fleeTarget = transform.position + (fleeDirection * fleeDistance);

            // Adiciona leve variação angular para a fuga parecer mais natural
            fleeTarget += Random.insideUnitSphere * 4f;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // Se a direção direta colidir com a borda, tenta o ponto válido mais próximo
                SetRandomWanderDestination();
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
            if (animator == null) return;

            float currentSpeed = agent.velocity.magnitude;
            animator.SetFloat(SpeedHash, currentSpeed);
            animator.SetBool(IsMovingHash, currentSpeed > 0.15f);
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

