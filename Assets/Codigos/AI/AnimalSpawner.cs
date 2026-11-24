using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cerrado.AI
{
    /// <summary>
    /// Gerencia a população viva de fauna na reserva e garante que a camada dos animais
    /// atravesse livremente a cerca do perímetro (enquanto o jogador permanece contido).
    /// </summary>
    public class AnimalSpawner : MonoBehaviour
    {
        [Header("Prefabs de Animais (Pool de Reposição)")]
        [Tooltip("Prefabs com os scripts AnimalAI e PhotographableTarget configurados")]
        [SerializeField] private List<GameObject> animalPrefabs = new List<GameObject>();

        [Header("População da Zona")]
        [SerializeField] private int maxPopulation = 42;
        [SerializeField] private float spawnRadius = 36f;
        [SerializeField] private float respawnInterval = 18f;

        private readonly List<GameObject> activeAnimals = new List<GameObject>();
        private float respawnTimer;

        private void Awake()
        {
            ConfigureFenceLayerCollision();
        }

        private void Start()
        {
            // Registra animais já instanciados na cena (_Entidades) para evitar excesso de população
            var existing = FindObjectsByType<AnimalAI>(FindObjectsInactive.Include);
            foreach (var ai in existing)
            {
                if (ai != null && !activeAnimals.Contains(ai.gameObject))
                {
                    activeAnimals.Add(ai.gameObject);
                }
            }

            if (animalPrefabs != null && animalPrefabs.Count > 0 && activeAnimals.Count < maxPopulation)
            {
                InitialSpawn();
            }
            respawnTimer = respawnInterval;
        }

        public static void ConfigureFenceLayerCollision()
        {
            int animalLayer = LayerMask.NameToLayer("Animal");
            int fenceLayer = LayerMask.NameToLayer("FenceBoundary");
            if (animalLayer >= 0 && fenceLayer >= 0)
            {
                Physics.IgnoreLayerCollision(animalLayer, fenceLayer, true);
            }
        }

        private void Update()
        {
            activeAnimals.RemoveAll(a => a == null);

            if (animalPrefabs != null && animalPrefabs.Count > 0 && activeAnimals.Count < maxPopulation)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    SpawnOneAnimalFromForestEdge();
                    respawnTimer = respawnInterval;
                }
            }
        }

        private void InitialSpawn()
        {
            int toSpawn = Mathf.Min(maxPopulation - activeAnimals.Count, 8);
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnOneAnimalFromForestEdge();
            }
        }

        private void SpawnOneAnimalFromForestEdge()
        {
            if (animalPrefabs == null || animalPrefabs.Count == 0) return;

            GameObject prefab = animalPrefabs[Random.Range(0, animalPrefabs.Count)];
            if (prefab == null) return;

            // Spawna na borda da mata atrás da cerca para entrar naturalmente no campo
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 edgePoint = new Vector3(Mathf.Cos(angle) * 48f, 2f, 6.5f + Mathf.Sin(angle) * 44f);

            if (NavMesh.SamplePosition(edgePoint, out NavMeshHit hit, 14f, NavMesh.AllAreas))
            {
                GameObject spawned = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
                activeAnimals.Add(spawned);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
