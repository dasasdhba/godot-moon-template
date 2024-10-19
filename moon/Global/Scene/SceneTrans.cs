using System;
using Godot;

namespace Global;

public abstract class SceneTrans
{
    public abstract TransNode GetTransNode();

    public double InTime { get ;set; } = 0.5d;
    public double InWaitTime { get ;set; } = 0.4d;
    public double OutWaitTime { get ; set; } = 0.1d;
    public double OutTime { get ;set; } = 0.5d;
    public Func<double ,double> Interpolation { get ;set; } = p => p;

    public SceneTrans SetInTime(double time)
    {
        InTime = time;
        return this;
    }

    public SceneTrans SetOutTime(double time)
    {
        OutTime = time;
        return this;
    }

    public SceneTrans SetInWaitTime(double time)
    {
        InWaitTime = time;
        return this;
    }

    public SceneTrans SetOutWaitTime(double time)
    {
        OutWaitTime = time;
        return this;
    }

    public SceneTrans SetWaitTime(double time)
    {
        InWaitTime = time - OutWaitTime;
        return this;
    }

    public SceneTrans SetInterpolation(Func<double, double> interpolation)
    {
        Interpolation = interpolation;
        return this;
    }
}