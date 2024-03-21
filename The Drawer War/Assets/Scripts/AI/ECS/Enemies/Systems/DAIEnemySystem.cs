using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public partial struct DAIEnemySystem : ISystem
{
    private Unity.Mathematics.Random random;
    public float worldHeight;
    public float worldWidth;

    public void OnCreate(ref SystemState state)
    {
        random = new((uint)System.DateTime.Now.Ticks);
        worldHeight = Camera.main.orthographicSize * 2;
        worldWidth = worldHeight * ((float)Screen.width / Screen.height);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (enemy, transform, physics) in SystemAPI.Query<RefRW<DAIEnemy>, RefRW<LocalTransform>, RefRW<DrawerPhysics>>())
        {
            // If new state detected, change states
            if (enemy.ValueRO.currentState != enemy.ValueRO.nextState)
            {
                enemy.ValueRW.currentState = enemy.ValueRO.nextState;
            }

            // Update current state
            switch (enemy.ValueRO.currentState)
            {
                case States.Chase:
                    Chase(enemy, transform, physics);
                    break;
                case States.Roam:
                    Roam(enemy, transform, physics);
                    break;
                case States.Dead:
                    break;
            }
        }
    }
}
