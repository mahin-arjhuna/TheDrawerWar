using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public partial struct DAIEnemyMidRangeSystem : ISystem
{
    public enum MidRangeAttacks
    {
        SlowExtend,
        FastExtend,
        MediumRetract
    }

    float3 bladeBasePosition;
    MidRangeAttacks attack;
    MidRangeAttacks nextAttack;

    public void MidRangeAttack(ref SystemState             state,
                                   RefRW<DAIEnemy>         enemy,
                                   RefRW<DAIEnemyMidRange> midRangeEnemy,
                                   RefRW<LocalTransform>   transform,
                                   Entity                  entity)
    {
        // Mid range attack FSM
        if (attack != nextAttack)
        {
            attack = nextAttack;
        }

        // Get children entities
        DynamicBuffer<Child> children = state.EntityManager.GetBuffer<Child>(entity);

        switch (attack)
        {
            case MidRangeAttacks.SlowExtend:
                SlowExtend(ref state, enemy, midRangeEnemy, transform, children[0].Value);
                break;
            case MidRangeAttacks.FastExtend:
                FastExtend(ref state, enemy, midRangeEnemy, transform, entity, children[0].Value);
                break;
            case MidRangeAttacks.MediumRetract:
                MediumRetract(ref state, enemy, midRangeEnemy, children[0].Value);
                break;
        }
    }

    private void SlowExtend(ref SystemState state,
                            RefRW<DAIEnemy> enemy,
                            RefRW<DAIEnemyMidRange> midRangeEnemy,
                            RefRW<LocalTransform> transform,
                            Entity child)
    {
        enemy.ValueRW.timer += Time.deltaTime;

        // Get blade transform
        LocalTransform bladeTransform = state.EntityManager.GetComponentData<LocalTransform>(child);
        float3 bladePosition = bladeTransform.Position;

        // Slowly extend blade
        bladePosition.y += midRangeEnemy.ValueRO.attackSlowExtendSpeed * Time.deltaTime;
        state.EntityManager.SetComponentData(child, LocalTransform.FromPosition(bladePosition));

        // Update rotation
        float2 position = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
        float2 attackDirection = enemy.ValueRO.playerPosition - position;
        enemy.ValueRW.currAcceleration = enemy.ValueRO.acceleration * HelperFunctions.Normalize(attackDirection - enemy.ValueRO.velocity);
        if (Vector2.Dot(enemy.ValueRO.currAcceleration, enemy.ValueRO.velocity) < 0)
            enemy.ValueRW.currAcceleration *= enemy.ValueRO.turnMultiplier;
        enemy.ValueRW.velocity += Time.deltaTime * enemy.ValueRO.currAcceleration;

        float angle = HelperFunctions.GetAngleTowardsVector(enemy.ValueRO.velocity);
        transform.ValueRW.Rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // Once fully extended, start retracting
        if (bladePosition.y >= bladeBasePosition.y + midRangeEnemy.ValueRO.bladeSlowExtendMaxDisplacement)
            nextAttack = MidRangeAttacks.FastExtend;
    }

    private void FastExtend(ref SystemState state,
                            RefRW<DAIEnemy> enemy,
                            RefRW<DAIEnemyMidRange> midRangeEnemy,
                            RefRW<LocalTransform> transform,
                            Entity entity,
                            Entity child)
    {
        enemy.ValueRW.timer += Time.deltaTime;

        // Get blade transform
        LocalTransform bladeTransform = state.EntityManager.GetComponentData<LocalTransform>(child);
        float3 bladePosition = bladeTransform.Position;

        // Extend blade fast
        bladePosition.y += midRangeEnemy.ValueRO.attackFastExtendSpeed * Time.deltaTime;
        state.EntityManager.SetComponentData(child, LocalTransform.FromPosition(bladePosition));

        // Move entire body towards player
        float2 position = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
        float2 attackDirection = HelperFunctions.Normalize(enemy.ValueRO.velocity);
        float2 change = midRangeEnemy.ValueRO.bodyJerkSpeed * Time.deltaTime * attackDirection;
        float2 newPosition = position + change;
        float3 newPos = new (newPosition.x, newPosition.y, 0.0f);
        quaternion rotation = transform.ValueRO.Rotation;
        float scale = transform.ValueRO.Scale;
        state.EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(newPos, rotation, scale));

        // Once fully extended, start retracting
        if (bladePosition.y >= bladeBasePosition.y + midRangeEnemy.ValueRO.bladeFastExtendMaxDisplacement)
            nextAttack = MidRangeAttacks.MediumRetract;
    }

    private void MediumRetract(ref SystemState state,
                               RefRW<DAIEnemy> enemy,
                               RefRW<DAIEnemyMidRange> midRangeEnemy,
                               Entity child)
    {
        enemy.ValueRW.timer += Time.deltaTime;

        // Get blade transform
        LocalTransform bladeTransform = state.EntityManager.GetComponentData<LocalTransform>(child);
        float3 bladePosition = bladeTransform.Position;

        // Slowly retract blade
        bladePosition.y -= midRangeEnemy.ValueRO.attackRetractSpeed * Time.deltaTime;

        // Once fully retracted, set next state to chase
        if (bladePosition.y <= bladeBasePosition.y)
        {
            bladePosition.y = bladeBasePosition.y;
            if (enemy.ValueRO.timer >= enemy.ValueRO.attackCooldown)
            {
                enemy.ValueRW.timer = 0.0f;
                enemy.ValueRW.nextState = States.Chase;
                nextAttack = MidRangeAttacks.SlowExtend;
            }
        }

        state.EntityManager.SetComponentData(child, LocalTransform.FromPosition(bladePosition));
    }
}
