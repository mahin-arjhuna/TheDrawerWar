using Unity.Entities;
using Unity.Mathematics;

public struct DAIEnemySpawner : IComponentData
{
    public Entity prefab;
    public float2 spawnPosition;
    public float spawnRate;
    public float nextSpawnTime;
    public int limit;
    public EnemyType type;

    public DAIEnemySpawnerSystem.SpawnMode spawnMode;
    public int instantSpawnCount;
    public bool done;
}
