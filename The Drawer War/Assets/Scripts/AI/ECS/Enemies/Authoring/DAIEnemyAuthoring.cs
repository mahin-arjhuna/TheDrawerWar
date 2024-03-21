using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class DAIEnemyAuthoring : MonoBehaviour
{
    public ulong health;
    public ulong damage;
    public ulong moneyDrop;
    public float maxSpeed;
    public float acceleration;
    public float attackRadius;
    public float attackCooldown;
}

class DAIEnemyBaker : Baker<DAIEnemyAuthoring>
{
    public override void Bake(DAIEnemyAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemy
        {
            health = authoring.health,
            damage = authoring.damage,
            moneyDrop = authoring.moneyDrop,
            maxSpeed = authoring.maxSpeed,
            acceleration = authoring.acceleration,
            attackRadius = authoring.attackRadius,
            attackCooldown = authoring.attackCooldown,

            type = EnemyType.LowRange, // default to low range (if add Invalid, spawner can see Invalid in list of types)
            currentState = States.Idle,
            nextState = States.Idle,
            velocity = float2.zero,
            currAcceleration = float2.zero,
            timer = 0.0f,

            playerPosition = new Vector2(0.0f, 0.0f),

            turnMultiplier = 3,

            currRoamTimer = 0.0f,
            roamTimer = 1.0f,
            roamVector = float2.zero,
            turning = false,
        });
    }
}