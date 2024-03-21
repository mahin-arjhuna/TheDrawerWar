using UnityEngine;
using Unity.Entities;

class DAIEnemyMidRangeAuthoring : MonoBehaviour
{
    public float attackSlowExtendSpeed;
    public float attackFastExtendSpeed;
    public float attackRetractSpeed;
}

class DAIEnemyMidRangeBaker : Baker<DAIEnemyMidRangeAuthoring>
{
    public override void Bake(DAIEnemyMidRangeAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemyMidRange
        {
            attackSlowExtendSpeed = authoring.attackSlowExtendSpeed,
            attackFastExtendSpeed = authoring.attackFastExtendSpeed,
            attackRetractSpeed = authoring.attackRetractSpeed,

            bodyJerkSpeed = 5.0f,
            bladeSlowExtendMaxDisplacement = 0.25f,
            bladeFastExtendMaxDisplacement = 1.1f
        });
    }
}
