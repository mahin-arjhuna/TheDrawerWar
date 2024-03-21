using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct DAIEnemyLongRangeSystem : ISystem
{
    public void LongRangeAttack(ref SystemState state,
                                RefRW<DAIEnemy> enemy,
                                RefRW<DAIEnemyLongRange> longRangeEnemy,
                                RefRW<LocalTransform> transform)
    {
        enemy.ValueRW.timer += Time.deltaTime;
        if (enemy.ValueRO.timer >= longRangeEnemy.ValueRO.shotDelay)
        {
            enemy.ValueRW.timer = 0.0f;

            float2 mousePos = enemy.ValueRO.playerPosition;
            float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
            float2 attackDirection = mousePos - position;
            float2 normDirection = HelperFunctions.Normalize(attackDirection);

            Entity staple = state.EntityManager.Instantiate(longRangeEnemy.ValueRO.staplesPrefab);
            LocalTransform stapleTransform = state.EntityManager.GetComponentData<LocalTransform>(staple);
            float3 staplePosition = transform.ValueRO.Position + new float3(normDirection.x, normDirection.y, 0.0f);
            float angle = HelperFunctions.GetAngleTowardsVector(attackDirection);
            // we want antiparallel rotation so that pointy parts of staple are facing player
            quaternion stapleRotation = Quaternion.Euler(new(0.0f, 0.0f, angle + 180.0f));
            float stapleScale = stapleTransform.Scale;
            state.EntityManager.SetComponentData(staple, LocalTransform.FromPositionRotationScale(staplePosition, stapleRotation, stapleScale));

            // Set stapler data
            DAIStaple stapleComponent = state.EntityManager.GetComponentData<DAIStaple>(staple);
            DrawerPhysics staplePhysicsComponent = state.EntityManager.GetComponentData<DrawerPhysics>(staple);
            staplePhysicsComponent.velocity = stapleComponent.speed * normDirection;
            state.EntityManager.SetComponentData(staple, staplePhysicsComponent);

            ++longRangeEnemy.ValueRW.numShots;

            if (longRangeEnemy.ValueRO.numShots == longRangeEnemy.ValueRO.shots)
            {
                longRangeEnemy.ValueRW.numShots = 0;
                enemy.ValueRW.nextState = States.Roam;
            }
        }
    }
}
