using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

class DAIStapleAuthoring : MonoBehaviour
{
    public float speed;
}

class DAIStapleBaker : Baker<DAIStapleAuthoring>
{
    public override void Bake(DAIStapleAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIStaple
        {
            speed = authoring.speed
        });
    }
}