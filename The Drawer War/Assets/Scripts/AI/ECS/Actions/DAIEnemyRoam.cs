using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct DAIEnemySystem : ISystem
{
    public void Roam(RefRW<DAIEnemy> enemy, RefRW<LocalTransform> transform, RefRW<DrawerPhysics> physics)
    {
        // Wander Parameters //
        float wanderWhiskersMaxAngle = 15.0f; // on each side
        float wanderDistance = 3.0f;

        // If enemy is boss, don't run attack cooldown logic
        if (enemy.ValueRO.type != EnemyType.Boss)
        {
            enemy.ValueRW.timer += Time.deltaTime;
            if (enemy.ValueRO.timer >= enemy.ValueRO.attackCooldown)
            {
                enemy.ValueRW.timer = 0.0f;
                enemy.ValueRW.nextState = States.Attack;
                return;
            }
        }

        enemy.ValueRW.currRoamTimer += Time.deltaTime;
        if (enemy.ValueRW.currRoamTimer >= enemy.ValueRO.roamTimer)
        {
            enemy.ValueRW.currRoamTimer = 0.0f;

            if (!enemy.ValueRO.turning)
            {
                // Wander //

                // Get new wander direction
                float2 forward = HelperFunctions.Normalize(physics.ValueRO.velocity);
                if (forward.Equals(float2.zero))
                {
                    forward = HelperFunctions.GetVectorFromAngle(HelperFunctions.ToEuler(transform.ValueRO.Rotation).z);
                }
                float rotation = HelperFunctions.GetAngleTowardsVector(forward);
                float randomAngle = rotation + random.NextFloat(-wanderWhiskersMaxAngle, wanderWhiskersMaxAngle);
                float2 wanderVector = wanderDistance * HelperFunctions.GetVectorFromAngle(randomAngle);
                enemy.ValueRW.roamVector = wanderVector;
            }
        }

        // Tether //
        enemy.ValueRW.turning = Tether(enemy, transform, physics, wanderDistance);

        // Update Acc, Vel, Rotation //
        // Acc
        float2 accelerationDirection = enemy.ValueRW.roamVector - physics.ValueRO.velocity;
        enemy.ValueRW.currAcceleration = enemy.ValueRO.acceleration * HelperFunctions.Normalize(accelerationDirection);
        // Vel
        physics.ValueRW.velocity += Time.deltaTime * enemy.ValueRO.currAcceleration;
        float length = HelperFunctions.Length(physics.ValueRO.velocity);
        float2 normalized = physics.ValueRO.velocity / length;
        if (length > enemy.ValueRO.maxSpeed)
            physics.ValueRW.velocity = normalized * enemy.ValueRO.maxSpeed;
        // Rotation
        float angle = HelperFunctions.GetAngleTowardsVector(normalized);
        transform.ValueRW.Rotation = Quaternion.Euler(new(0.0f, 0.0f, angle));
    }

    private bool Tether(RefRW<DAIEnemy> enemy, RefRW<LocalTransform> transform, RefRW<DrawerPhysics> physics, float wanderDistance)
    {
        // If no velocity return false
        if (physics.ValueRO.velocity.Equals(float2.zero))
            return false;

        // Tether Parameters //
        const float tetherAngle = 45.0f;
        const float returnAngle = 20.0f;
        const float turningThreshod = 2.0f;

        // Slight turns away from edge
        float2 position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
        float distanceToTop = worldHeight * 0.5f - position.y;
        float distanceToBottom = position.y - -worldHeight * 0.5f;
        float distanceToLeft = position.x - -worldWidth * 0.5f;
        float distanceToRight = worldWidth * 0.5f - position.x;

        float2 normalized = HelperFunctions.Normalize(physics.ValueRO.velocity);
        float upDot = Vector2.Dot(normalized, new float2(0.0f, 1.0f));
        float downDot = Vector2.Dot(normalized, new float2(0.0f, -1.0f));
        float leftDot = Vector2.Dot(normalized, new float2(-1.0f, 0.0f));
        float rightDot = Vector2.Dot(normalized, new float2(1.0f, 0.0f));

        bool turning = false;
        float turningAngle = 0.0f;

        // Vertical Bounds //
        if (distanceToTop < turningThreshod && Mathf.Acos(downDot) > returnAngle * Mathf.Deg2Rad) // top
        {
            turning = true;
            float ratio = (turningThreshod - distanceToTop) / turningThreshod;
            if (leftDot >= rightDot)
                turningAngle = tetherAngle * ratio * Time.deltaTime;
            else
                turningAngle = -tetherAngle * ratio * Time.deltaTime;
        }
        else if (distanceToBottom < turningThreshod && Mathf.Acos(upDot) > returnAngle * Mathf.Deg2Rad) // bottom
        {
            turning = true;
            float ratio = (turningThreshod - distanceToBottom) / turningThreshod;
            if (leftDot >= rightDot)
                turningAngle = -tetherAngle * ratio * Time.deltaTime;
            else
                turningAngle = tetherAngle * ratio * Time.deltaTime;
        }

        // Horizontal Bounds //
        if (distanceToLeft < turningThreshod && Mathf.Acos(rightDot) > returnAngle * Mathf.Deg2Rad) // left
        {
            turning = true;
            float ratio = (turningThreshod - distanceToLeft) / turningThreshod;
            if (upDot >= downDot)
                turningAngle = -tetherAngle * ratio * Time.deltaTime;
            else
                turningAngle = tetherAngle * ratio * Time.deltaTime;
        }
        else if (distanceToRight < turningThreshod && Mathf.Acos(leftDot) > returnAngle * Mathf.Deg2Rad) // right
        {
            turning = true;
            float ratio = (turningThreshod - distanceToRight) / turningThreshod;
            if (upDot >= downDot)
                turningAngle = tetherAngle * ratio * Time.deltaTime;
            else
                turningAngle = -tetherAngle * ratio * Time.deltaTime;
        }
        // Apply Tether //
        if (turning)
        {
            float angle = HelperFunctions.GetAngleTowardsVector(normalized);
            angle += turningAngle;
            physics.ValueRW.velocity = wanderDistance * HelperFunctions.GetVectorFromAngle(angle);
            enemy.ValueRW.roamVector = physics.ValueRO.velocity;
        }

        return turning;
    }
}
