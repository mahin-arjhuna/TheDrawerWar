using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

[BurstCompile]
public partial struct DAIEnemyLongRangeSystem : ISystem
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
        foreach (var (enemy, longRangeEnemy, transform) in SystemAPI.Query<RefRW<DAIEnemy>, RefRW<DAIEnemyLongRange>, RefRW<LocalTransform>>())
        {
            if (enemy.ValueRO.currentState == States.Idle)
            {
                enemy.ValueRW.nextState = States.Roam;
                enemy.ValueRW.currRoamTimer = enemy.ValueRO.roamTimer;
            }
            else if (enemy.ValueRO.currentState == States.Attack)
                enemy.ValueRW.currentState = States.LongRangeAttack;

            switch (enemy.ValueRO.currentState)
            {
                case States.LongRangeAttack:
                    LongRangeAttack(ref state, enemy, longRangeEnemy, transform);
                    break;
            }
        }
    }
}
