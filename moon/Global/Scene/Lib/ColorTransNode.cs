﻿using Godot;

namespace Global;

public partial class ColorTransNode : TransNode
{
    [ExportCategory("ColorTransNode")]
    [Export]
    public Color Color { get ;set; } = new(0f, 0f, 0f, 1f);

    public Color NodeColor
    {
        get => (Color)GetNode("ColorFill").Get("color");
        set => GetNode("ColorFill").Set("color", value);
    }

    public override void _Ready()
        => NodeColor = Color with { A = 0f };

    public override void TransInProcess(double p)
        => NodeColor = Color with { A = Color.A * (float)p };
}