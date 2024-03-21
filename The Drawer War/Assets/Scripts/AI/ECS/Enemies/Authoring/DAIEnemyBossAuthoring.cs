using UnityEngine;
using Unity.Entities;

class DAIEnemyBossAuthoring : MonoBehaviour
{
    public int lineShotsPerSecond;
    public GameObject staplesPrefab;
    public int waveShotsPerSecond;
    public float waveConeAngle;
    public int streaksPerWave;
    public int scatterShotsPerSecond;
    public float scatterConeAngle;
}

class DAIEnemyBossBaker : Baker<DAIEnemyBossAuthoring>
{
    public override void Bake(DAIEnemyBossAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new DAIEnemyBoss
        {
            lineShotDelay = 1.0f / authoring.lineShotsPerSecond,
            staplesPrefab = GetEntity(authoring.staplesPrefab, TransformUsageFlags.Dynamic),
            waveShotDelay = 1.0f / authoring.waveShotsPerSecond,
            waveConeAngle = authoring.waveConeAngle,
            streaksPerWave = authoring.streaksPerWave,
            scatterShotDelay = 1.0f / authoring.scatterShotsPerSecond,
            scatterConeAngle = authoring.scatterConeAngle
        });
    }
}