using UnityEngine;
using Unity.Entities;

public partial struct DrawerPlayerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnExit(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        Vector2 pos = new (0.0f, 0.0f);
        foreach (RefRW<DrawerPlayer> player in SystemAPI.Query<RefRW<DrawerPlayer>>())
        {
            pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            player.ValueRW.position = pos;
        }

        foreach (RefRW<DAIEnemy> enemy in SystemAPI.Query<RefRW<DAIEnemy>>())
        {
            enemy.ValueRW.playerPosition = pos;
        }
    }
}
