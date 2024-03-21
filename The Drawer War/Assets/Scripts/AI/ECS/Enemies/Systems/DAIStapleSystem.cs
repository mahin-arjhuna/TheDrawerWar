using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct DAIStapleSystem : ISystem
{
    public float worldHeight;
    public float worldWidth;

    public void OnCreate(ref SystemState state)
    {
        worldHeight = Camera.main.orthographicSize * 2;
        worldWidth = worldHeight * ((float)Screen.width / Screen.height);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get Entity Command Buffer
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (staple, transform, entity) in SystemAPI.Query<RefRW<DAIStaple>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            // If outside of bounds (use 0.75f instead of 0.5f for a little bit of extra room) the destroy
            if (transform.ValueRO.Position.x < -worldWidth * 0.6f ||
                transform.ValueRO.Position.x > worldWidth * 0.6f ||
                transform.ValueRO.Position.y < -worldHeight * 0.6f ||
                transform.ValueRO.Position.y > worldHeight * 0.6f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
