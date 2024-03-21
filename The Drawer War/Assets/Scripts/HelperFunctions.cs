using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct HelperFunctions
{
    public static float GetAngleTowardsVector(float2 direction)
    {
        // Return in degrees
        if (direction.y >= 0.0f)
            return Mathf.Rad2Deg * Mathf.Atan(-direction.x / direction.y);
        else
            return 180.0f - Mathf.Rad2Deg * Mathf.Atan(direction.x / direction.y);
    }

    public static float2 GetVectorFromAngle(float angle)
    {
        // Angle is in degrees
        float x = math.sin(Mathf.Deg2Rad * -angle);
        float y = math.cos(Mathf.Deg2Rad * -angle);
        return new Vector2(x, y);
    }

    public static float3 ToEuler(quaternion quaternion)
    {
        // Credit to user xVergilx for this function
        // Link: https://forum.unity.com/threads/is-there-a-conversion-method-from-quaternion-to-euler.624007/
        float4 q = quaternion.value;
        double3 res;

        double sinr_cosp = +2.0 * (q.w * q.x + q.y * q.z);
        double cosr_cosp = +1.0 - 2.0 * (q.x * q.x + q.y * q.y);
        res.x = math.atan2(sinr_cosp, cosr_cosp);

        double sinp = +2.0 * (q.w * q.y - q.z * q.x);
        if (math.abs(sinp) >= 1)
        {
            res.y = math.PI / 2 * math.sign(sinp);
        }
        else
        {
            res.y = math.asin(sinp);
        }

        double siny_cosp = +2.0 * (q.w * q.z + q.x * q.y);
        double cosy_cosp = +1.0 - 2.0 * (q.y * q.y + q.z * q.z);
        res.z = math.atan2(siny_cosp, cosy_cosp);

        return (float3)res;
    }

    public static float LengthSquared(float2 vector)
    {
        return Vector2.Dot(vector, vector);
    }

    public static float Length(float2 vector)
    {
        return Mathf.Sqrt(LengthSquared(vector));
    }

    public static float2 Normalize(float2 vector)
    {
        if (vector.Equals(float2.zero))
            return vector;
        return vector / Length(vector);
    }
}
