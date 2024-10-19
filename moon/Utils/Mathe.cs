using Godot;
using System;

namespace Utils;

/// <summary>
/// math functions
/// </summary>
public static partial class Mathe
{
    public static Vector2 WithAxis(this Vector2 origin, Vector2 axis)
    {
        var dir = axis.Normalized();
        origin -= dir * dir.Dot(origin);
        return origin + axis;
    }
    
    /// <summary>
    /// the search function should be like: search(x) = true if x &lt; t where t &gt; 0, false otherwise.
    /// the binary search function tries to find the t value
    /// </summary>
    public static double BinarySearch(Func<double, bool> search, double step = 256d)
    {
        if (!search(0d)) return 0d;

        var a = 0d;
        var b = step;

        while (search(b)) b *= 2d;

        while (b - a > 1d)
        {
            var c = (a + b) / 2d;
            if (search(c)) a = c;
            else b = c;
        }

        return b;
    }
    
    /// <summary>
    /// <inheritdoc cref="BinarySearch(System.Func{double,bool},double)"/>>
    /// </summary>
    public static float BinarySearch(Func<float, bool> search, float step = 256f)
    {
        if (!search(0f)) return 0f;

        var a = 0f;
        var b = step;

        while (search(b)) b *= 2f;

        while (b - a > 1f)
        {
            var c = (a + b) / 2f;
            if (search(c)) a = c;
            else b = c;
        }

        return b;
    }
    
    public static double Accelerate(double speed, double acc, double dec, double max, double delta)
        => Mathf.MoveToward(speed, max, speed < max ? acc * delta : dec * delta);
    
    public static float Accelerate(float speed, float acc, float dec, float max, float delta)
        => Mathf.MoveToward(speed, max, speed < max ? acc * delta : dec * delta);

    /// <summary>
    /// move an angle towards another one in closest direction
    /// </summary>
    public static double MoveTowardAngle(double current, double target, double delta)
    {
        while (current - target > double.Pi) target += double.Pi * 2f;
        while (current - target < -double.Pi) target -= double.Pi * 2f;
        return Mathf.MoveToward(current, target, delta);
    }
    
    /// <summary>
    /// <inheritdoc cref="MoveTowardAngle(double,double,double)"/>>
    /// </summary>
    public static float MoveTowardAngle(float current, float target, float delta)
    {
        while (current - target > float.Pi) target += float.Pi * 2f;
        while (current - target < -float.Pi) target -= float.Pi * 2f;
        return Mathf.MoveToward(current, target, delta);
    }
    
    /// <summary>
    /// move a direction vector towards another one by angle
    /// </summary>
    public static Vector2 MoveTowardDir(Vector2 origin, Vector2 target, double delta)
    {
        var o = origin.Angle();
        var t = target.Angle();
        return Vector2.Right.Rotated((float)MoveTowardAngle(o, t, delta));
    }
    
    /// <summary>
    /// clamp an angle in center ± spread range
    /// </summary>
    public static double ClampAngle(double angle, double center, double spread)
    {
        angle = Mathf.Wrap(angle, -double.Pi, double.Pi);
        center = Mathf.Wrap(center, -double.Pi, double.Pi);
        double diff = Mathf.Wrap(center - angle, -double.Pi, double.Pi);
        if (Math.Abs(diff) < spread) return angle;

        var min = center - spread;
        var max = center + spread;
        var dmin = Mathf.Wrap(min - angle, -double.Pi, double.Pi);
        var dmax = Mathf.Wrap(max - angle, -double.Pi, double.Pi);
        return Math.Abs(dmin) < Math.Abs(dmax) ? min : max;
    }
    
    /// <summary>
    /// <inheritdoc cref="ClampAngle(double,double,double)"/>>
    /// </summary>
    public static float ClampAngle(float angle, float center, float spread)
    {
        angle = Mathf.Wrap(angle, -float.Pi, float.Pi);
        center = Mathf.Wrap(center, -float.Pi, float.Pi);
        float diff = Mathf.Wrap(center - angle, -float.Pi, float.Pi);
        if (Math.Abs(diff) < spread) return angle;

        var min = center - spread;
        var max = center + spread;
        var dmin = Mathf.Wrap(min - angle, -float.Pi, float.Pi);
        var dmax = Mathf.Wrap(max - angle, -float.Pi, float.Pi);
        return Math.Abs(dmin) < Math.Abs(dmax) ? min : max;
    }
    
    /// <summary>
    /// clamp a direction vector in normal.Rotate(±spread) range
    /// </summary>
    public static Vector2 ClampDir(Vector2 dir, Vector2 normal, double spread)
        => Vector2.Right.Rotated((float)ClampAngle(dir.Angle(), normal.Angle(), spread));

    // random
    public static RandomNumberGenerator RNG { get ;set; }= new();

    /// <summary>
    /// 0~1 (inclusive)
    /// </summary>
    public static float Randf() => RNG.Randf();

    /// <summary>
    /// min~max (inclusive)
    /// </summary>
    public static float RandfRange(float min, float max) => RNG.RandfRange(min, max);

    /// <summary>
    /// 0~4294967295 (inclusive)
    /// </summary>
    public static uint Randi() => RNG.Randi();

    /// <summary>
    /// min~max (inclusive)
    /// </summary>
    public static int RandiRange(int min, int max) => RNG.RandiRange(min, max);
}