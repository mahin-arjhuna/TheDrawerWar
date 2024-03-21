using Unity.Entities;
using Unity.Mathematics;

public struct DAIEnemy : IComponentData
{
    // Property Variables (all enemies have these)
    public ulong health;
    public ulong damage;
    public ulong moneyDrop;
    public float maxSpeed;
    public float acceleration;
    public float attackRadius;
    public float attackCooldown;
    // Internal Variables (for calculations and logic)
    public EnemyType type;
    public States currentState;
    public States nextState;
    public float2 velocity;
    public float2 currAcceleration;
    public float timer;

    public float2 playerPosition;

    public int turnMultiplier; // for Chase behavior (and short range attack)

    public float currRoamTimer; // for Roam behavior
    public float roamTimer;     // for Roam behavior
    public float2 roamVector;   // for Roam behavior
    public bool turning;        // for Roam behavior
}
