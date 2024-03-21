using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

[BurstCompile]
public partial struct DAIEnemyMidRangeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        bladeBasePosition.x = 0.005f;
        bladeBasePosition.y = 0.6f;
        bladeBasePosition.z = 0.0f;
        attack = MidRangeAttacks.SlowExtend;
        nextAttack = MidRangeAttacks.SlowExtend;
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (enemy, midRangeEnemy, transform, entity) in
            SystemAPI.Query<RefRW<DAIEnemy>, RefRW<DAIEnemyMidRange>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (enemy.ValueRO.currentState == States.Idle)
                enemy.ValueRW.nextState = States.Chase;
            else if (enemy.ValueRO.currentState == States.Attack)
                enemy.ValueRW.currentState = States.MidRangeAttack;

            switch (enemy.ValueRO.currentState)
            {
                case States.MidRangeAttack:
                    MidRangeAttack(ref state, enemy, midRangeEnemy, transform, entity);
                    break;
            }
        }
    }
}
