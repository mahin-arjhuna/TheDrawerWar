using Unity.Entities;
using UnityEngine;

public struct DAIEnemyLongRange : IComponentData
{
    // Specific Variables (only long range enemies have these)
    public int shots;
    public float shotDelay;
    public Entity staplesPrefab;
    // Internal Variables (for calculations and logic)
    public int numShots;
}
