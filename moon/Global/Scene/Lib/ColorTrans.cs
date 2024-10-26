using Godot;

namespace Global;

public class ColorTrans : SceneTrans
{
    public Color Color { get ;set; } = new(0f, 0f, 0f, 1f);

    public ColorTrans SetColor(Color color)
    {
        Color = color;
        return this;
    }

    public override TransNode GetTransNode()
    {
        var result = Moon.Scene.ColorTrans.Instantiate<ColorTransNode>();
        result.Color = Color;
        return result;
    }
}