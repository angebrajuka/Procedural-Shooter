using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Math
{
    public static float ROOT_ONE_HALF = 0.70710678118654752440084436210485f;
    
    public static float VecToAngle(Vector2 vec)
    {
        vec.Normalize();
        return NormalizedVecToAngle(vec);
    }

    public static float NormalizedVecToAngle(Vector2 vec)
    {
        float angle = Vector3.Angle(Vector3.right, vec);
        if(vec.y < 0) angle = 360-angle;
        return angle;
    }

    public static readonly byte[,] directions =
    {
        {1, 0, 0}, 
        {1, 0, 0}, 
        {1, 1, 0}
    };
    
    public static readonly Vector2[] vectors =
    {
        Vector2.right,
        new Vector2(ROOT_ONE_HALF, ROOT_ONE_HALF),
        Vector2.up,
        new Vector2(-ROOT_ONE_HALF, ROOT_ONE_HALF),
        Vector2.left,
        new Vector2(-ROOT_ONE_HALF, -ROOT_ONE_HALF),
        Vector2.down,
        new Vector2(ROOT_ONE_HALF, -ROOT_ONE_HALF)
    };
    
    public static int AngleToDir(float angle)
    {
        angle %= 360;
        return (angle >= 90 && angle <= 270) ? 1 : 0;
    }

    public static bool Closer(Vector2 pos1, Vector2 pos2, float distance)
    {
        float dx = pos2.x-pos1.x;
        float dy = pos2.y-pos2.y;
        return dx*dx+dy*dy < distance*distance;
    }

    public static float Sqr(float x)
    {
        return x*x;
    }

    public static float Dist(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Sqr(x2-x1)+Sqr(y2-y1));
    }

    public static Vector3Int Vec3(Vector2Int vec)
    {
        return new Vector3Int(vec.x, vec.y, 0);
    }

    public static Vector3 Vec3(Vector2 vec)
    {
        return new Vector3(vec.x, vec.y, 0);
    }

    public static Vector2 Vec2(Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }

    public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, value));
    }

    public static float Avg(float val1, float val2) {
        return (val1+val2)/2;
    }

    public static bool Ish(float value1, float value2, float range=0.05f)
    {
        return Mathf.Abs(value1-value2) < range;
    }

    public static Vector2 AngleToVector2(float degrees)
    {
        degrees *= Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(degrees), Mathf.Sin(degrees));
    }

    public static Vector2Int Divide(Vector2Int vec1, Vector2Int vec2)
    {
        return new Vector2Int(vec1.x/vec2.x, vec1.y/vec2.y);
    }

    public static float Mod(float a,float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    public static long RandLong() {
        ushort ll = (ushort)Random.Range(0, 65535);
        ushort lm = (ushort)Random.Range(0, 65535);
        ushort rm = (ushort)Random.Range(0, 65535);
        ushort rr = (ushort)Random.Range(0, 65535);

        return ll << 48 + lm << 32 + rm << 16 + rr;
    }

    public static float Perlin3D(float seed, float x, float y, float z, float scale=1.0f) {
        x *= scale;
        y *= scale;
        z *= scale;
        x += seed+123.321f;
        y += seed+123.321f;
        z += seed+123.321f;

        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f;
    }
}
