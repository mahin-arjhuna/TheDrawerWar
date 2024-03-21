using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class DAIEnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject[] prefab;
    public float spawnRate;
    public int spawnLimit;
    public bool instantSpawn;
    public int instantSpawnCount;
    public EnemyType type;
}

class DAIEnemySpawnerBaker : Baker<DAIEnemySpawnerAuthoring>
{
    public override void Bake(DAIEnemySpawnerAuthoring authoring)
    {
        DAIEnemySpawnerSystem.SpawnMode mode = DAIEnemySpawnerSystem.SpawnMode.Iterative;
        if (authoring.instantSpawn)
            mode = DAIEnemySpawnerSystem.SpawnMode.Instant;

        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemySpawner
        {
            prefab = GetEntity(authoring.prefab[(int)authoring.type], TransformUsageFlags.Dynamic),
            spawnPosition = new float2(authoring.transform.position.x, authoring.transform.position.y),
            spawnRate = authoring.spawnRate,
            nextSpawnTime = 0.0f,
            limit = authoring.spawnLimit,
            type = authoring.type,

            spawnMode = mode,
            instantSpawnCount = authoring.instantSpawnCount,
            done = false
        });
    }
}
