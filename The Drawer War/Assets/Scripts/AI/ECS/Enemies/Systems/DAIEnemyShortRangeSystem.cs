using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public partial struct DAIEnemyShortRangeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (enemy, shortRangeEnemy, transform, physics)
            in SystemAPI.Query<RefRW<DAIEnemy>, RefRW<DAIEnemyShortRange>, RefRW<LocalTransform>, RefRW<DrawerPhysics>>())
        {
            if (enemy.ValueRO.currentState == States.Idle)
                enemy.ValueRW.nextState = States.Chase;
            else if (enemy.ValueRO.currentState == States.Attack)
            {
                enemy.ValueRW.currentState = States.ShortRangeAttack;
                shortRangeEnemy.ValueRW.angleVelocity = physics.ValueRO.velocity;
            }

            switch (enemy.ValueRO.currentState)
            {
                case States.ShortRangeAttack:
                    ShortRangeAttack(enemy, shortRangeEnemy, transform, physics);
                    break;
            }
        }
    }
}