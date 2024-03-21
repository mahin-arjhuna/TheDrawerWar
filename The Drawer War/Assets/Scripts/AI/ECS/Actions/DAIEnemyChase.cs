using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public partial struct DAIEnemySystem : ISystem
{
    public void Chase(RefRW<DAIEnemy> enemy, RefRW<LocalTransform> transform, RefRW<DrawerPhysics> physics)
    {
        // Follow mouse position
        float2 playerPosition = enemy.ValueRO.playerPosition;
        float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
        float2 attackDirection = playerPosition - position;

        // Update acceleration (Seek behavior) [more acceleration if target is behind]
        enemy.ValueRW.currAcceleration = enemy.ValueRO.acceleration * HelperFunctions.Normalize(attackDirection - physics.ValueRO.velocity);
        if (Vector2.Dot(enemy.ValueRO.currAcceleration, physics.ValueRO.velocity) < 0)
            enemy.ValueRW.currAcceleration *= enemy.ValueRO.turnMultiplier;
        // Update velocity
        physics.ValueRW.velocity += Time.deltaTime * enemy.ValueRO.currAcceleration;
        if (HelperFunctions.LengthSquared(physics.ValueRO.velocity) > enemy.ValueRO.maxSpeed * enemy.ValueRO.maxSpeed)
            physics.ValueRW.velocity = HelperFunctions.Normalize(physics.ValueRO.velocity) * enemy.ValueRO.maxSpeed;
        // Update Rotation
        float angle = HelperFunctions.GetAngleTowardsVector(physics.ValueRO.velocity);
        transform.ValueRW.Rotation = Quaternion.Euler(new(0, 0, angle));

        DrawerPhysicsSystem.DrawVector(position, physics.ValueRO.velocity, Color.green);
        DrawerPhysicsSystem.DrawVector(position + physics.ValueRO.velocity, enemy.ValueRW.currAcceleration, Color.red);
        DrawerPhysicsSystem.DrawVector(position, attackDirection, Color.black);

        // Check if player is within attacking distance
        if (HelperFunctions.LengthSquared(attackDirection) <= enemy.ValueRO.attackRadius * enemy.ValueRO.attackRadius)
        {
            enemy.ValueRW.timer = 0.0f;
            enemy.ValueRW.nextState = States.Attack;
            physics.ValueRW.velocity = float2.zero;
        }
    }
}