using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

[BurstCompile]
public partial struct DAIEnemyBossSystem : ISystem
{
    private Unity.Mathematics.Random random;
    AttackPatterns pattern;
    AttackPatterns nextPattern;

    public void OnCreate(ref SystemState state)
    {
        random = new((uint)System.DateTime.Now.Ticks);
        pattern = AttackPatterns.Line;
        nextPattern = AttackPatterns.Line;
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (enemy, bossEnemy, transform) in SystemAPI.Query<RefRW<DAIEnemy>, RefRW<DAIEnemyBoss>, RefRW<LocalTransform>>())
        {
            if (enemy.ValueRO.currentState == States.Idle)
            {
                enemy.ValueRW.nextState = States.Roam;
                enemy.ValueRW.currRoamTimer = enemy.ValueRO.roamTimer;
            }

            BulletHellAttack(ref state, enemy, bossEnemy, transform);
        }
    }
}
