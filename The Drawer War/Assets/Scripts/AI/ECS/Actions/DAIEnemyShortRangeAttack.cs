using Unity.Transforms;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public partial struct DAIEnemyShortRangeSystem : ISystem
{
    public void ShortRangeAttack(RefRW<DAIEnemy> enemy,
                                 RefRW<DAIEnemyShortRange> shortRangeEnemy,
                                 RefRW<LocalTransform> transform,
                                 RefRW<DrawerPhysics> physics)
    {
        float attackDelay = shortRangeEnemy.ValueRO.preAttackDelay;

        // Get position
        float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);

        // Delay by attackDelay amount of time before attacking
        if (enemy.ValueRO.timer < attackDelay)
        {
            enemy.ValueRW.timer += Time.deltaTime;

            float2 attackDirection = enemy.ValueRO.playerPosition - position;

            // Update acceleration (for speed of rotation)
            enemy.ValueRW.currAcceleration = enemy.ValueRO.acceleration * HelperFunctions.Normalize(attackDirection - shortRangeEnemy.ValueRO.angleVelocity);
            if (Vector2.Dot(enemy.ValueRO.currAcceleration, shortRangeEnemy.ValueRO.angleVelocity) < 0)
                enemy.ValueRW.currAcceleration *= enemy.ValueRO.turnMultiplier;
            // Update velocity
            shortRangeEnemy.ValueRW.angleVelocity += Time.deltaTime * enemy.ValueRO.currAcceleration;
            // Update rotation
            float angle = HelperFunctions.GetAngleTowardsVector(shortRangeEnemy.ValueRO.angleVelocity);
            transform.ValueRW.Rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Once timer is up, set attacking speed
            if (enemy.ValueRO.timer >= attackDelay)
            {
                // Set attacking speed
                physics.ValueRW.velocity = shortRangeEnemy.ValueRO.attackingSpeed * HelperFunctions.GetVectorFromAngle(angle);
                physics.ValueRW.angularVelocity = 0.0f;
            }
        }
        else
        {
            enemy.ValueRW.timer += Time.deltaTime;

            // Decelerate
            float2 decelerate = shortRangeEnemy.ValueRO.postAttackDeceleration * Time.deltaTime * physics.ValueRO.velocity;
            physics.ValueRW.velocity -= decelerate;

            // Once speed is approximately 0, reset and return to chasing state
            float length = HelperFunctions.Length(physics.ValueRO.velocity);
            if (length <= 0.5f)
            {
                physics.ValueRW.velocity = float2.zero;
                if (enemy.ValueRO.timer >= enemy.ValueRO.attackCooldown)
                {
                    // Set velocity to scaled up normalized attackDirection
                    physics.ValueRW.velocity = enemy.ValueRO.maxSpeed * HelperFunctions.Normalize(shortRangeEnemy.ValueRO.angleVelocity);
                    // Reset timer
                    enemy.ValueRW.timer = 0.0f;
                    enemy.ValueRW.nextState = States.Chase;
                }
            }
        }
    }
}