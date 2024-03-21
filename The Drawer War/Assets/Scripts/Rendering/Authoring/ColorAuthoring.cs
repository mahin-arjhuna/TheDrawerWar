using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ColorAuthoring : MonoBehaviour
{
    public Color color;
}

class ColorBaker : Baker<ColorAuthoring>
{
    public override void Bake(ColorAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = authoring.color;
    }
}
