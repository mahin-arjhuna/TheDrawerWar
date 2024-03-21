using Unity.Entities;
using Unity.Mathematics;

public struct DrawerCollider : IComponentData
{
    public float2 boundingBox;
}

public struct DrawerPhysicsData
{
    // Mostly for collision detection & update portion
    public int index { get; set; }
    public float gravity { get; set; }
    public float2 position { get; set; }
    public float scale { get; set; }
    public float rotation { get; set; }
    public float2 velocity { get; set; }
    public float angularVelocity { get; set; }
    public float2 boundingBox { get; set; }

    // For impulse calculation
    public float invm { get; set; }
    public float invI { get; set; }
    public float restitution { get; set; }
    public float friction { get; set; }
}

// This is for parallel jobs to generate collisions
public struct DrawerPhysicsDataPair
{
    public DrawerPhysicsData physics1 { get; set; }
    public DrawerPhysicsData physics2 { get; set; }
}

// This is the parallel jobs' output type, so that the main thread
// can use these to compute impulses using data stored in DrawerPhysicsData instances
public struct DrawerCollision
{
    public int penetratingObject { get; set; }
    public int penetratedObject { get; set; }
    public float2 penetratingPoint { get; set; }
    public float2 normal { get; set; }
    public float depth { get; set; }
    public byte isColliding { get; set; }
}
