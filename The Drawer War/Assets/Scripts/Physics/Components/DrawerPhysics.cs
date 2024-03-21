using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct DrawerPhysics : IComponentData
{
    // Properties representing rates of change
    public float gravity;
    public float2 velocity;
    public float angularVelocity;

    // Sources of inertia (but their inverses for faster computation)
    public float invm;
    public float invI;

    // Damping for collision, linear & angular movements
    public float restitution;
    public float friction;
    public byte enabled;
}
