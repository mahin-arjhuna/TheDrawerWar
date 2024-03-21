using Unity.Collections;
using Unity.Entities;

public struct EnemySpawner : IComponentData
{
    public Entity enemyPrefab;
    public float spawnTimer;
    public float spawnRate;
}
