//using Microsoft.Unity.VisualStudio.Editor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

public class SampleEnemyAuthoring : MonoBehaviour
{
    // Start is called before the first frame update
    public float gravity;
    public float2 velocity;
    public float angularVelocity;
    public float mass;
    public float rotationalInertia;
    public float restitution;
    public float friction;
    public bool enabled = true;
    public float2 boundingBox = new(1, 1);
}

class SampleEnemyBaker : Baker<SampleEnemyAuthoring>
{
    public override void Bake(SampleEnemyAuthoring authoring)
    {
        // Convert GameObject Component into Entity Component
        Entity enemy = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(enemy, new DrawerPhysics
        {
            gravity = authoring.gravity / 1.4f,
            velocity = authoring.velocity,
            angularVelocity = authoring.angularVelocity,
            invm = (authoring.mass > 0.0f) ? 1.0f / authoring.mass : 0.0f,
            invI = (authoring.rotationalInertia > 0.0f) ? 1.0f / authoring.rotationalInertia : 0.0f,
            restitution = authoring.restitution,
            friction = authoring.friction,
            enabled = authoring.enabled ? (byte)1 : (byte)0,
        });
        AddComponent(enemy, new DrawerCollider
        {
            boundingBox = authoring.boundingBox
        });
    }
}
