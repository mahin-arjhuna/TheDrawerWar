using Unity.Entities;
using UnityEngine;

public struct DAIEnemyBoss : IComponentData
{
    // Specific Variables (only boss enemies have these)
    public float lineShotDelay;
    public Entity staplesPrefab;
    public float waveShotDelay;
    public float waveConeAngle;
    public int streaksPerWave;
    public float scatterShotDelay;
    public float scatterConeAngle;
    // Internal Variables (for calculations and logic)
}
