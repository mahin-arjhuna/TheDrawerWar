using Unity.Entities;

public struct DAIEnemyMidRange : IComponentData
{
    // Specific Variables (only mid range enemies have these)
    public float attackSlowExtendSpeed;
    public float attackFastExtendSpeed;
    public float attackRetractSpeed;
    // Internal Variables (for calculations and logic)
    public float bodyJerkSpeed;
    public float bladeSlowExtendMaxDisplacement; // this distance is not offset, it is distance from the base blade position
    public float bladeFastExtendMaxDisplacement; // this distance is not offset, it is distance from the base blade position
}
