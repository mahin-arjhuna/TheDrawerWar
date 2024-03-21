using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public partial struct DrawerPhysicsSystem : ISystem
{
    NativeArray<DrawerPhysicsData> source;
    public void OnCreate(ref SystemState state)
    {
         source = new(20000, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        source.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Wait for user input to spawn enemies on LMB
        foreach (var spawner in SystemAPI.Query<RefRO<EnemySpawner>>())
        {
            if (Input.GetMouseButtonDown(0))
            {
                Entity enemy = state.EntityManager.Instantiate(spawner.ValueRO.enemyPrefab);
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePosition.z = 0.0f;
                state.EntityManager.SetComponentData(enemy, LocalTransform.FromPositionRotationScale(mousePosition, Quaternion.identity, 0.5f));
            }
        }

        int jobCount = 12;        

        int counter = 0;
        foreach (var (physics, collider, transform) in SystemAPI.Query<RefRW<DrawerPhysics>, RefRW<DrawerCollider>, RefRW<LocalTransform>>())
        {
            if (physics.ValueRO.enabled != 0)
            {
                // Populate the source array which will act as the input/output buffer between the system and the components
                source[counter] = new()
                {
                    index = counter,
                    position = new(transform.ValueRO.Position.x, transform.ValueRO.Position.y),
                    scale = transform.ValueRO.Scale,
                    rotation = math.radians(((Quaternion)transform.ValueRO.Rotation).eulerAngles.z),
                    gravity = physics.ValueRO.gravity,
                    velocity = physics.ValueRO.velocity,
                    angularVelocity = physics.ValueRO.angularVelocity,
                    boundingBox = collider.ValueRO.boundingBox,
                    invm = physics.ValueRO.invm,
                    invI = physics.ValueRO.invI,
                    restitution = physics.ValueRO.restitution,
                    friction = physics.ValueRO.friction,
                };
                ++counter;
            }
        }
        int objCount = counter;

        int objPairCount = objCount * (objCount - 1) / 2;
        NativeArray<DrawerPhysicsDataPair> input = new(objPairCount, Allocator.Persistent);
        NativeArray<DrawerCollision> output = new(objPairCount, Allocator.Persistent);

        // TODO: This part can be optimized using sort and sweep
        // Generate all unordered pairs of physics data
        counter = 0;
        for (int i = 0; i < objCount - 1; ++i)
        {
            for (int j = i + 1; j < objCount; ++j)
            {
                input[counter] = new()
                {
                    physics1 = source[i],
                    physics2 = source[j],
                };
                ++counter;
            }
        }

        // TODO: This part has to be offloaded to IJobParallelFor
        int numOfCollisions = 0;
        foreach (var pair in input)
        {
            DrawerCollision collision = IsColliding(pair);
            if (collision.isColliding == 1)
            {
                output[numOfCollisions] = collision;
                ++numOfCollisions;
            }
        }

        // Back in the main thread, apply impulses, normal and tangential
        float[] normalImpulses = new float[numOfCollisions];
        float[] tangentialImpulses = new float[numOfCollisions];
        float accumNormalImpulse, accumTangentialImpulse;

        for (int i = 0; i < 10; ++i)
        {
            for (int k = 0; k < numOfCollisions; ++k)
            {
                DrawerCollision collision = output[k];
                if (collision.isColliding == 1)
                {
                    float normalImpulse, tangentialImpulse;

                    DrawerPhysicsData penetratingObject = source[collision.penetratingObject];
                    DrawerPhysicsData penetratedObject = source[collision.penetratedObject];

                    float2 r1 = collision.penetratingPoint - new float2(penetratingObject.position.x, penetratingObject.position.y);
                    float2 r2 = collision.penetratingPoint - new float2(penetratedObject.position.x, penetratedObject.position.y);
                    float2 normal = collision.normal, tangent = GetNormal(collision.normal);

                    // Compute friction along a tangent
                    float frictionClamp = normalImpulses[k] * math.sqrt(penetratingObject.friction * penetratedObject.friction);
                    accumTangentialImpulse = tangentialImpulses[k];
                    tangentialImpulse = CalcTangentialImpulse(collision, penetratingObject, penetratedObject, r1, r2);
                    tangentialImpulses[k] = math.clamp(tangentialImpulses[k] + tangentialImpulse, -frictionClamp, frictionClamp);
                    tangentialImpulse = tangentialImpulses[k] - accumTangentialImpulse;

                    // Apply tangential impulse (friction)
                    penetratingObject.velocity -= penetratingObject.invm * tangentialImpulse * tangent;
                    penetratedObject.velocity += penetratedObject.invm * tangentialImpulse * tangent;
                    penetratingObject.angularVelocity -= penetratingObject.invI * tangentialImpulse * (r1.x * tangent.y - r1.y * tangent.x);
                    penetratedObject.angularVelocity += penetratedObject.invI * tangentialImpulse * (r2.x * tangent.y - r2.y * tangent.x);

                    // Compute impulse exerted by the collision normal
                    accumNormalImpulse = normalImpulses[k];
                    normalImpulse = CalcNormalImpulse(collision, penetratingObject, penetratedObject, r1, r2, deltaTime);
                    normalImpulses[k] = math.max(normalImpulses[k] + normalImpulse, 0);
                    normalImpulse = normalImpulses[k] - accumNormalImpulse;

                    // Compute impulse along the normal
                    float2 penetration = collision.depth * collision.normal;

                    // Apply normal impulse
                    penetratingObject.velocity -= penetratingObject.invm * normalImpulse * normal;
                    penetratedObject.velocity += penetratedObject.invm * normalImpulse * normal;
                    penetratingObject.angularVelocity -= penetratingObject.invI * normalImpulse * (r1.x * normal.y - r1.y * normal.x);
                    penetratedObject.angularVelocity += penetratedObject.invI * normalImpulse * (r2.x * normal.y - r2.y * normal.x);

                    source[penetratingObject.index] = penetratingObject;
                    source[penetratedObject.index] = penetratedObject;
                }
                ++counter;
            }
        }

        // Update the components with the newly calculated values
        counter = 0;
        foreach (var (physics, transform) in SystemAPI.Query<RefRW<DrawerPhysics>, RefRW<LocalTransform>>())
        {
            // If physics component is enabled, we have processed it
            if (physics.ValueRO.enabled != 0)
            {
                transform.ValueRW.Position = new float3(source[counter].position + source[counter].velocity * deltaTime, 0);
                transform.ValueRW.Rotation = Quaternion.EulerRotation(0, 0, source[counter].rotation + source[counter].angularVelocity * deltaTime);
                physics.ValueRW.velocity = source[counter].velocity - new float2(0, source[counter].gravity * deltaTime);
                physics.ValueRW.angularVelocity = source[counter].angularVelocity;

                ++counter;
            }
            // If physics component is disabled, we do not interfere with velocity or angular velocity calculation except for gravity
            else
            {
                transform.ValueRW.Position += new float3(physics.ValueRO.velocity * deltaTime, 0);
                transform.ValueRW = transform.ValueRO.RotateZ(physics.ValueRO.angularVelocity * 180 / math.PI * deltaTime);

                physics.ValueRW.velocity -= new float2(0, physics.ValueRO.gravity * deltaTime);
            }
        }
        input.Dispose();
        output.Dispose();

    }


    [BurstCompile]
    private static float CalcNormalImpulse(in DrawerCollision collision, in DrawerPhysicsData penetratingObject, in DrawerPhysicsData penetratedObject, in float2 r1, in float2 r2, float deltaTime)
    {

        // Coefficient of restitution
        float e = math.max(penetratedObject.restitution, penetratingObject.restitution);

        float2 r1Perp = new float2(-r1.y, r1.x);
        float2 r2Perp = new float2(-r2.y, r2.x);

        // Relative velocity of penetrating object from the penetrated object's POV
        float2 vr = penetratedObject.velocity + penetratedObject.angularVelocity * r2Perp
            - penetratingObject.velocity - penetratingObject.angularVelocity * r1Perp;

        float2 normal = collision.normal;

        float jnv = Vector2.Dot(vr, normal);

        if (jnv > 0)
        {
            return 0;
        }

        float rn1 = Vector2.Dot(r1Perp, normal);
        float rn2 = Vector2.Dot(r2Perp, normal);

        float theta1 = penetratingObject.invI * rn1 * rn1;
        float theta2 = penetratedObject.invI * rn2 * rn2;

        // Impulse felt by the penetrated object, as the penetration normal goes into the penetrated object
        //- e * math.min(jnv + 0.1f, 0
        float baumgarte = 0.005f / deltaTime * math.max(collision.depth - 0.00005f, 0.0f);
        //baumgarte = 0;
        float J = (-jnv + baumgarte - e * math.min(jnv + 0.0f, 0)) / (penetratingObject.invm + penetratedObject.invm + theta1 + theta2);

        return J;
    }

    [BurstCompile]
    private static float CalcTangentialImpulse(in DrawerCollision collision, in DrawerPhysicsData penetratingObject, in DrawerPhysicsData penetratedObject, in float2 r1, in float2 r2)
    {

        // Coefficient of restitution
        float e = math.max(penetratedObject.restitution, penetratingObject.restitution);

        float2 r1Perp = new float2(-r1.y, r1.x);
        float2 r2Perp = new float2(-r2.y, r2.x);

        // Relative velocity of penetrating object from the penetrated object's POV
        float2 vr = penetratedObject.velocity + penetratedObject.angularVelocity * r2Perp
            - penetratingObject.velocity - penetratingObject.angularVelocity * r1Perp;

        float2 tangent = GetNormal(collision.normal);

        float jnv = Vector2.Dot(vr, tangent);

        float rt1 = Vector2.Dot(r1Perp, tangent);
        float rt2 = Vector2.Dot(r2Perp, tangent);

        float theta1 = penetratingObject.invI * rt1 * rt1;
        float theta2 = penetratedObject.invI * rt2 * rt2;

        // Impulse felt by the penetrated object, as the penetration normal goes into the penetrated object
        float J = -(1 + e) * jnv / (penetratingObject.invm + penetratedObject.invm + theta1 + theta2);

        return J;
    }


    [BurstCompile]
    private DrawerCollision IsColliding(in DrawerPhysicsDataPair pair)
    {
        DrawerCollision result = PolygonToPolygonCollision(pair.physics1, pair.physics2);

        if (result.isColliding == 1)
        {
            DrawVector(result.penetratingPoint, result.depth * result.normal, Color.red);
        }

        return result;
    }

    // Use normals of edges in polygon A to check for collision against polygon B
    [BurstCompile]
    private DrawerCollision PolygonToPolygonCollision(in DrawerPhysicsData objectA, in DrawerPhysicsData objectB)
    {
        // Use separating axis theorem to check for collision
        // 2 box colliders * 2 axes = 4 axes
        // 2 box colliders * 4 vertices = 8 vertices
        float2[] verticesA = GetVertices(objectA);
        float2[] verticesB = GetVertices(objectB);

        // Compute normals of all edges in both colliders A and B
        float2[] normalsA = GetNormals(verticesA);
        float2[] normalsB = GetNormals(verticesB);

        float Amin, Amax, Bmin, Bmax;
        float2 AminVertex, AmaxVertex, BminVertex, BmaxVertex;
        DrawerCollision result = new DrawerCollision { depth = float.PositiveInfinity };

        int axisCountA = normalsA.Length;
        List<float2> normals = new(normalsA);
        normals.AddRange(normalsB);

        char penetratingObject = ' ';
        for (int i = 0; i < normals.Count; ++i)
        {            
            float2 normal = normals[i];
            ProjectPolygon(verticesA, normal, out Amin, out Amax, out AminVertex, out AmaxVertex);
            ProjectPolygon(verticesB, normal, out Bmin, out Bmax, out BminVertex, out BmaxVertex);

            // Checking from A's perspective
            if (i < axisCountA)
            {
                // B on the left, A on the right
                if (Amin <= Bmax && Amin >= Bmin)
                {
                    if (math.abs(result.depth) > Bmax - Amin)
                    {
                        penetratingObject = 'B';
                        result.normal = normal;
                        result.depth = Bmax - Amin;
                        result.penetratingPoint = BmaxVertex;
                    }
                }
                // A on the left, B on the right
                else if (Bmin <= Amax && Bmin >= Amin)
                {
                    if (math.abs(result.depth) > Amax - Bmin)
                    {
                        penetratingObject = 'B';
                        result.normal = normal;
                        result.depth = Bmin - Amax;
                        result.penetratingPoint = BminVertex;
                    }
                }
                else
                {
                    result.isColliding = 0;
                    return result;
                }
            }
            // Checking from B's perspective
            else
            {
                // B on the left, A on the right
                if (Amin <= Bmax && Amin >= Bmin)
                {
                    if (math.abs(result.depth) > Bmax - Amin)
                    {
                        penetratingObject = 'A';
                        result.normal = normal;
                        result.depth = Amin - Bmax;
                        result.penetratingPoint = AminVertex;
                    }
                }
                // A on the left, B on the right
                else if (Bmin <= Amax && Bmin >= Amin)
                {
                    if (math.abs(result.depth) > Amax - Bmin)
                    {
                        penetratingObject = 'A';
                        result.normal = normal;
                        result.depth = Amax - Bmin;
                        result.penetratingPoint = AmaxVertex;
                    }
                }
                else
                {
                    result.isColliding = 0;
                    return result;
                }
            }
        }

        // Normal should be going "into" the penetrated object
        result.isColliding = 1;
        result.normal *= math.sign(result.depth);
        result.depth = math.abs(result.depth);
        switch (penetratingObject)
        {
            case 'A':
                result.penetratingObject = objectA.index;
                result.penetratedObject = objectB.index;
                EdgeToEdgeCollision(ref result, verticesA, verticesB, normalsA, normalsB);
                break;
            case 'B':
                result.penetratingObject = objectB.index;
                result.penetratedObject = objectA.index;
                EdgeToEdgeCollision(ref result, verticesB, verticesA, normalsB, normalsA);
                break;
        }

        return result;
    }

    [BurstCompile]
    private static void EdgeToEdgeCollision(ref DrawerCollision collision, float2[] penetratingObjectVertices, float2[] penetratedObjectVertices, float2[] penetratingObjectNormals, float2[] penetratedObjectNormals)
    {
        float2 firstOtherPoint = collision.penetratingPoint - collision.depth * collision.normal;
        Debug.DrawLine(new(collision.penetratingPoint.x, collision.penetratingPoint.y), new(firstOtherPoint.x, firstOtherPoint.y));
        // In the penetrating polygon, find an edge whose normal is anti-parallel to the penetration vector
        int penetratedObjectEdgeIndex = -1;
        int penetratingObjectEdgeIndex = -1;
        for (int i = 0; i < penetratingObjectNormals.Length; ++i)
        {
            if (Parallel(penetratingObjectNormals[i], collision.normal, 0.000002f))
            {
                penetratingObjectEdgeIndex = i;
            }
        }
        for (int i = 0; i < penetratedObjectNormals.Length; ++i)
        {
            if (Antiparallel(penetratedObjectNormals[i], collision.normal, 0.000002f))
            {
                penetratedObjectEdgeIndex = i;
            }
        }

        if (penetratingObjectEdgeIndex == -1) return;

        // Pick one of the edges and project all vertices from both edges onto it
        float2 axis = new(-collision.normal.y, collision.normal.x);
        float penetratingMin, penetratingMax, penetratedMin, penetratedMax;
        float2 penetratingMinVertex, penetratingMaxVertex, penetratedMinVertex, penetratedMaxVertex;
        float2[] penetratingObjectEdgeVertices = { penetratingObjectVertices[penetratingObjectEdgeIndex], penetratingObjectVertices[(penetratingObjectEdgeIndex + 1) % penetratingObjectVertices.Length] };
        float2[] penetratedObjectEdgeVertices = { penetratedObjectVertices[penetratedObjectEdgeIndex], penetratedObjectVertices[(penetratedObjectEdgeIndex + 1) % penetratedObjectVertices.Length] };
        ProjectPolygon(penetratingObjectEdgeVertices, axis, out penetratingMin, out penetratingMax, out penetratingMinVertex, out penetratingMaxVertex);
        ProjectPolygon(penetratedObjectEdgeVertices, axis, out penetratedMin, out penetratedMax, out penetratedMinVertex, out penetratedMaxVertex);

        // Find the midpoint of the two vertices that form the overlap
        if (penetratedMin <= penetratingMin && penetratedMax >= penetratingMax)
        {
            collision.penetratingPoint = (penetratingMinVertex + penetratingMaxVertex) / 2.0f;
            Debug.DrawLine(new(penetratingMinVertex.x, penetratingMinVertex.y), new(penetratingMaxVertex.x, penetratingMaxVertex.y), Color.green);
        }
        else if (penetratingMin <= penetratedMin && penetratingMax >= penetratedMax)
        {
            collision.penetratingPoint = (penetratedMinVertex + penetratedMaxVertex) / 2.0f;
            Debug.DrawLine(new(penetratedMinVertex.x, penetratedMinVertex.y), new(penetratedMaxVertex.x, penetratedMaxVertex.y), Color.green);
        }
        // Penetrated object on the left, penetrating object on the right
        else if (penetratingMin <= penetratedMax && penetratingMin >= penetratedMin)
        {
            collision.penetratingPoint = (penetratingMinVertex + penetratedMaxVertex) / 2.0f;
            Debug.DrawLine(new(penetratingMinVertex.x, penetratingMinVertex.y), new(penetratedMaxVertex.x, penetratedMaxVertex.y), Color.green);
        }
        // Penetrating object on the left, penetrated object on the right
        else if (penetratedMin <= penetratingMax && penetratedMin >= penetratingMin)
        {
            collision.penetratingPoint = (penetratedMinVertex + penetratingMaxVertex) / 2.0f;
            Debug.DrawLine(new(penetratedMinVertex.x, penetratedMinVertex.y), new(penetratingMaxVertex.x, penetratingMaxVertex.y), Color.green);
        }
    }

    // Takes two UNIT vectors and checks if they are considered close enough to anti-parallel (dot product is -1)
    [BurstCompile]
    private static bool Antiparallel(in float2 v1, in float2 v2, float epsilon)
    {
        return math.abs(Vector2.Dot(v1, v2) + 1.0f) < epsilon;
    }

    [BurstCompile]
    private static bool Parallel(in float2 v1, in float2 v2, float epsilon)
    {
        return math.abs(Vector2.Dot(v1, v2) - 1.0f) < epsilon;
    }

    // Takes a polygon and a normal to project its vertices onto,
    // then returns 4 values (min dot product, max dot product, min dot product's source vertex, max dot product's source vertex)
    [BurstCompile]
    private static void ProjectPolygon(float2[] vertices, in float2 normal, out float min, out float max, out float2 minVertex, out float2 maxVertex)
    {
        min = float.PositiveInfinity;
        max = float.NegativeInfinity;
        minVertex = float2.zero;
        maxVertex = float2.zero;
        foreach (float2 vertex in vertices)
        {
            float dotProduct = Vector2.Dot(vertex, normal);
            if (max < dotProduct)
            {
                max = dotProduct;
                maxVertex = vertex;
            }
            if (min > dotProduct)
            {
                min = dotProduct;
                minVertex = vertex;
            }
        }
    }

    [BurstCompile]
    private static float2[] GetVertices(in DrawerPhysicsData data)
    {
        float2 center = new(data.position.x, data.position.y);
        float2 halfSize = data.boundingBox * data.scale / 2.0f;
        float angle = data.rotation;
        float2[] vertices = { halfSize, new(halfSize.x, -halfSize.y), -halfSize, new(-halfSize.x, halfSize.y) };
        vertices = vertices.Select(vertex => Rotate(vertex, angle) + center).ToArray();
        

        Debug.DrawLine(new(vertices[0].x, vertices[0].y), new(vertices[1].x, vertices[1].y));
        Debug.DrawLine(new(vertices[1].x, vertices[1].y), new(vertices[2].x, vertices[2].y));
        Debug.DrawLine(new(vertices[2].x, vertices[2].y), new(vertices[3].x, vertices[3].y));
        Debug.DrawLine(new(vertices[3].x, vertices[3].y), new(vertices[0].x, vertices[0].y));

        return vertices;
    }

    [BurstCompile]
    private static float2[] GetEdgeNormals(in DrawerPhysicsData data)
    {
        float angle = data.rotation;
        float2[] edges = { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };
        edges = edges.Select(edge => new float2(edge.x * math.sin(angle) + edge.y * math.cos(angle),
                                                -edge.x * math.cos(angle) + edge.y * math.sin(angle))).ToArray();
        return edges;
    }

    [BurstCompile]
    private static float2[] GetNormals(in float2[] vertices)
    {
        return GetEdges(vertices).Select(edge => GetNormal(edge)).ToArray();
    }

    // Connects all vertices in the given List to create edges
    [BurstCompile]
    private static float2[] GetEdges(in float2[] vertices)
    {
        float2[] edges = new float2[vertices.Length];
        for (int i = 0; i < vertices.Length; ++i)
        {
            edges[i] = (vertices[(i + 1) % vertices.Length] - vertices[i]);
        }
        return edges;
    }

    [BurstCompile]
    private static float2 GetNormal(in float2 source)
    {
        if (!source.Equals(float2.zero))
        {
            float2 result = new(-source.y, source.x);
            result /= math.sqrt(Vector2.Dot(result, result));
            return result;
        }
        return float2.zero;
    }


    [BurstCompile]
    private static float2 Rotate(in float2 source, float angle)
    {
        return new(source.x * math.cos(angle) - source.y * math.sin(angle), source.x * math.sin(angle) + source.y * math.cos(angle));
    }

    [BurstCompile]
    public static void DrawVector(in float2 position, in float2 vector, in Color color)
    {
        if (!vector.Equals(float2.zero))
        {
            Vector3 beginPoint = new(position.x, position.y);
            Vector3 endPoint = new(position.x + vector.x, position.y + vector.y);
            float2 normal = GetNormal(vector);
            float2 pullBack = -vector / math.sqrt(Vector2.Dot(vector, vector));
            Vector3 leftArrowEndPoint = endPoint + 0.03f * new Vector3((normal + pullBack).x, (normal + pullBack).y);
            Vector3 rightArrowEndPoint = endPoint + 0.03f * new Vector3((-normal + pullBack).x, (-normal + pullBack).y);
            Debug.DrawLine(beginPoint, endPoint, color);
            Debug.DrawLine(endPoint, leftArrowEndPoint, color);
            Debug.DrawLine(endPoint, rightArrowEndPoint, color);
        }
    }
}