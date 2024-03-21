using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class DAIEnemyShortRangeAuthoring : MonoBehaviour
{
    public float attackingSpeed;
    public float preAttackDelay;
    public float postAttackDeceleration;
}

class DAIEnemyShortRangeBaker : Baker<DAIEnemyShortRangeAuthoring>
{
    public override void Bake(DAIEnemyShortRangeAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemyShortRange
        {
            attackingSpeed = authoring.attackingSpeed,
            postAttackDeceleration = authoring.postAttackDeceleration,
            preAttackDelay = authoring.preAttackDelay,
            angleVelocity = float2.zero
        });
    }
}
