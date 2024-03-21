using UnityEngine;
using Unity.Entities;

class DAIEnemyLongRangeAuthoring : MonoBehaviour
{
    public int shots;
    public float shotDelay;
    public GameObject staplesPrefab;
}

class DAIEnemyLongRangeBaker : Baker<DAIEnemyLongRangeAuthoring>
{
    public override void Bake(DAIEnemyLongRangeAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemyLongRange
        {
            shots = authoring.shots,
            shotDelay = authoring.shotDelay,
            staplesPrefab = GetEntity(authoring.staplesPrefab, TransformUsageFlags.Dynamic),
            numShots = 0
        });
    }
}