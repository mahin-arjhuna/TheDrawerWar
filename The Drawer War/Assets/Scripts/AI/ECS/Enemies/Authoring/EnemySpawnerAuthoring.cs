using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnRate;
}

class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        // Convert GameObject Component into Entity Component
        Entity enemySpawner = GetEntity(TransformUsageFlags.None);
        AddComponent(enemySpawner, new EnemySpawner
        {
            enemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
            spawnTimer = authoring.spawnRate,
            spawnRate = authoring.spawnRate
        });

        Debug.Log("Initialized enemy prefab entity [" + authoring.enemyPrefab.name + "]");
    }
}
