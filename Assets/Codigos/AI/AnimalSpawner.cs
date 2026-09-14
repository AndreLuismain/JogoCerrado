using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cerrado.AI
{
    public class AnimalSpawner : MonoBehaviour
    {
        [Header("Prefabs de Animais")]
        [Tooltip("Prefabs com os scripts AnimalAI e PhotographableTarget configurados")]
        [SerializeField] private List<GameObject> animalPrefabs = new List<GameObject>();

        [Header("População da Zona")]
        [SerializeField] private int maxPopulation = 4;
        [SerializeField] private float spawnRadius = 25f;
        [SerializeField] private float respawnInterval = 15f;

        private readonly List<GameObject> activeAnimals = new List<GameObject>();
        private float respawnTimer;

        private void Start()
        {
            InitialSpawn();
        }

        private void Update()
        {
            // Limpa referências nulas de animais que possam ter sido destruídos
            activeAnimals.RemoveAll(a => a == null);

            if (activeAnimals.Count < maxPopulation)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    SpawnOneAnimal();
                    respawnTimer = respawnInterval;
                }
            }
        }

        private void InitialSpawn()
        {
            int toSpawn = maxPopulation - activeAnimals.Count;
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnOneAnimal();
            }
            respawnTimer = respawnInterval;
        }

        private void SpawnOneAnimal()
        {
            if (animalPrefabs == null || animalPrefabs.Count == 0) return;

            GameObject prefab = animalPrefabs[Random.Range(0, animalPrefabs.Count)];
            if (prefab == null) return;

            Vector3 randomPoint = transform.position + (Random.insideUnitSphere * spawnRadius);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            {
                GameObject spawned = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
                activeAnimals.Add(spawned);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}

