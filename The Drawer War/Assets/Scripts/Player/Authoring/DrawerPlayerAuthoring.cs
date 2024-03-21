using UnityEngine;
using Unity.Entities;

class DrawerPlayerAuthoring : MonoBehaviour
{
}

class DrawerPlayerBaker : Baker<DrawerPlayerAuthoring>
{
    public override void Bake(DrawerPlayerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DrawerPlayer
        {
            position = new Vector2(0.0f, 0.0f),
        });
    }
}
