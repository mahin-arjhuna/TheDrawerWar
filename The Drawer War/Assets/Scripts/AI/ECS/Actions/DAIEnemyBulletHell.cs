using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public partial struct DAIEnemyBossSystem : ISystem
{
    public enum AttackPatterns
    {
        Line,
        Wave,
        Scatter
    }

    public void BulletHellAttack(ref SystemState state,
                                 RefRW<DAIEnemy> enemy,
                                 RefRW<DAIEnemyBoss> bossEnemy,
                                 RefRW<LocalTransform> transform)
    {
        if (pattern != nextPattern)
        {
            pattern = nextPattern;
        }

        switch (pattern)
        {
            case AttackPatterns.Line:
                Line(ref state, enemy, bossEnemy, transform);
                break;
            case AttackPatterns.Wave:
                Wave(ref state, enemy, bossEnemy, transform);
                break;
            case AttackPatterns.Scatter:
                Scatter(ref state, enemy, bossEnemy, transform);
                break;
        }

        // Easy switching between attack modes
        if (Input.GetKeyDown(KeyCode.Alpha1))
            nextPattern = AttackPatterns.Line;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            nextPattern = AttackPatterns.Wave;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            nextPattern = AttackPatterns.Scatter;
    }

    private void Shoot(ref SystemState state,
                       RefRW<DAIEnemyBoss> bossEnemy,
                       RefRW<LocalTransform> transform,
                       float2 normDirection)
    {
        // Spawn stapler and set transform
        Entity staple = state.EntityManager.Instantiate(bossEnemy.ValueRO.staplesPrefab);
        LocalTransform stapleTransform = state.EntityManager.GetComponentData<LocalTransform>(staple);
        float3 staplePosition = transform.ValueRO.Position + new float3(normDirection.x, normDirection.y, 0.0f);
        float angle = HelperFunctions.GetAngleTowardsVector(normDirection);
        // we want antiparallel rotation so that pointy parts of staple are facing player
        quaternion stapleRotation = Quaternion.Euler(new(0.0f, 0.0f, angle + 180.0f));
        float stapleScale = stapleTransform.Scale;
        state.EntityManager.SetComponentData(staple, LocalTransform.FromPositionRotationScale(staplePosition, stapleRotation, stapleScale));

        // Set stapler component data
        DAIStaple stapleComponent = state.EntityManager.GetComponentData<DAIStaple>(staple);
        DrawerPhysics staplePhysicsComponent = state.EntityManager.GetComponentData<DrawerPhysics>(staple);
        staplePhysicsComponent.velocity = stapleComponent.speed * normDirection;
        state.EntityManager.SetComponentData(staple, staplePhysicsComponent);
    }

    private void Line(ref SystemState state,
                      RefRW<DAIEnemy> enemy,
                      RefRW<DAIEnemyBoss> bossEnemy,
                      RefRW<LocalTransform> transform)
    {
        enemy.ValueRW.timer += Time.deltaTime;
        if (enemy.ValueRO.timer >= bossEnemy.ValueRO.lineShotDelay)
        {
            enemy.ValueRW.timer = 0.0f;

            float2 mousePos = enemy.ValueRO.playerPosition;
            float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
            float2 attackDirection = mousePos - position;

            Shoot(ref state, bossEnemy, transform, HelperFunctions.Normalize(attackDirection));
        }
    }

    private void Wave(ref SystemState state,
                      RefRW<DAIEnemy> enemy,
                      RefRW<DAIEnemyBoss> bossEnemy,
                      RefRW<LocalTransform> transform)
    {
        enemy.ValueRW.timer += Time.deltaTime;
        if (enemy.ValueRO.timer >= bossEnemy.ValueRO.waveShotDelay)
        {
            enemy.ValueRW.timer = 0.0f;

            float2 mousePos = enemy.ValueRO.playerPosition;
            float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
            float2 attackDirection = mousePos - position;

            float deltaAngle = bossEnemy.ValueRO.waveConeAngle / bossEnemy.ValueRO.streaksPerWave;
            float baseAngle = HelperFunctions.GetAngleTowardsVector(attackDirection);
            float waveConeAngleMax = bossEnemy.ValueRO.waveConeAngle * 0.5f;
            for (float i = -waveConeAngleMax; i <= waveConeAngleMax; i += deltaAngle)
            {
                float angle = baseAngle + i;
                float2 direction = HelperFunctions.GetVectorFromAngle(angle);
                Shoot(ref state, bossEnemy, transform, direction);
            }
        }
    }

    private void Scatter(ref SystemState state,
                         RefRW<DAIEnemy> enemy,
                         RefRW<DAIEnemyBoss> bossEnemy,
                         RefRW<LocalTransform> transform)
    {
        enemy.ValueRW.timer += Time.deltaTime;
        if (enemy.ValueRO.timer >= bossEnemy.ValueRO.scatterShotDelay)
        {
            enemy.ValueRW.timer = 0.0f;

            float2 mousePos = enemy.ValueRO.playerPosition;
            float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
            float2 attackDirection = mousePos - position;

            float baseAngle = HelperFunctions.GetAngleTowardsVector(attackDirection);
            float randomAngleMax = bossEnemy.ValueRO.scatterConeAngle * 0.5f;
            float angle = baseAngle + random.NextFloat(-randomAngleMax, randomAngleMax);
            float2 direction = HelperFunctions.GetVectorFromAngle(angle);
            Shoot(ref state, bossEnemy, transform, direction);
        }
    }
}
