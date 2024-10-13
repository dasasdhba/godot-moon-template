namespace Godot;

[GlobalClass]
public partial class AnimSprite2D : AnimatedSprite2D
{
    [ExportCategory("AnimSprite2D")]
    [Export]
    public bool AutoPlay { get; set; } = true;

    public AnimSprite2D() : base()
    {
        TreeEntered += () =>
        {
            if (AutoPlay) Play();  
        };
    }
}