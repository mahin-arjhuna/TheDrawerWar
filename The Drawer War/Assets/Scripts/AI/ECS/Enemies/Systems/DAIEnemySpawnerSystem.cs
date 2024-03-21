using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct DAIEnemySpawnerSystem : ISystem
{
    public enum SpawnMode
    {
        Instant,
        Iterative
    }

    int count;
    Unity.Mathematics.Random rand;

    public void OnCreate(ref SystemState state)
    {
        count = 0;
        rand = new((uint)System.DateTime.Now.Ticks);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<DAIEnemySpawner> spawner in SystemAPI.Query<RefRW<DAIEnemySpawner>>())
        {
            if (spawner.ValueRO.spawnMode == SpawnMode.Instant && !spawner.ValueRO.done)
            {
                InstantSpawn(ref state, spawner);
            }
            else if (spawner.ValueRO.spawnMode == SpawnMode.Iterative)
            {
                if (count >= spawner.ValueRO.limit)
                    continue;

                if (SystemAPI.Time.ElapsedTime >= spawner.ValueRO.nextSpawnTime)
                {
                    // Spawn enemy
                    Entity entity = state.EntityManager.Instantiate(spawner.ValueRO.prefab);

                    // Set enemy type
                    DAIEnemy enemy = state.EntityManager.GetComponentData<DAIEnemy>(entity);
                    enemy.type = spawner.ValueRO.type;
                    state.EntityManager.SetComponentData(entity, enemy);

                    // Set transform
                    LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                    state.EntityManager.SetComponentData(entity, transform);
                    spawner.ValueRW.nextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.spawnRate;

                    ++count;
                }
            }
        }
    }

    private void InstantSpawn(ref SystemState state, RefRW<DAIEnemySpawner> spawner)
    {
        IterativeSpawn(ref state, spawner);
    }

    private void IterativeSpawn(ref SystemState state, RefRW<DAIEnemySpawner> spawner)
    {
        for (int i = 0; i < spawner.ValueRO.instantSpawnCount; ++i)
        {
            const float range = 5.0f;
            float randomX = rand.NextFloat(-range, range);
            float randomY = rand.NextFloat(-range, range);
            Entity entity = state.EntityManager.Instantiate(spawner.ValueRO.prefab);

            LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
            float3 position = transform.Position;
            position.x += randomX;
            position.y += randomY;
            quaternion rotation = transform.Rotation;
            float scale = transform.Scale;
            state.EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, scale));
        }
        spawner.ValueRW.done = true;
        count = spawner.ValueRO.instantSpawnCount;
    }
}
