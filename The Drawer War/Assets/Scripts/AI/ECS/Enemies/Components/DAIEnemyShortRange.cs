using Unity.Entities;
using Unity.Mathematics;

public struct DAIEnemyShortRange : IComponentData
{
    // Specific Variables (only short range enemies have these)
    public float attackingSpeed; // tack's movement speed when attacking (not rate of attack)
    public float preAttackDelay;
    public float postAttackDeceleration;
    // Internal Variables (for calculations and logic)
    public float2 angleVelocity;
}
