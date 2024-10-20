using Utils;

namespace Godot;

[GlobalClass]
public partial class LabelScrollContainer : Control
{
    [ExportCategory("LabelScrollContainer")]
    [Export]
    public bool Disabled { get ;set; } = false;
    
    [Export]
    public float Sep { get ;set; } = 32f;
    
    [Export]
    public float Speed { get ;set; } = -10f;

    protected Label Label { get ;set; }
    protected Label ShadowLabel { get ;set; }
    
    public override void _Ready()
    {
        ClipContents = true;
    
        Label = GetChild<Label>(0);
        ShadowLabel = (Label)Label.Duplicate();
        Label.AddChild(ShadowLabel);
        this.AddProcess((delta) =>
        {
            if (Disabled) return;
        
            var scroll = Label.Size.X > Size.X;
            
            if (ResetFlag)
            {
                Label.Position = Label.Position with { X = scroll ? ResetOffset : 0f };
                ResetFlag = false;
            }

            if (scroll)
            {
                Label.Position += Speed * (float)delta * Vector2.Right;
                var width = Label.Size.X + Sep;
                while (Label.Position.X < -width)
                {
                    Label.Position += Vector2.Right * width;
                }

                while (Label.Position.X > 0f)
                {
                    Label.Position -= Vector2.Right * width;
                }
            }
            
            ShadowLabel.Visible = scroll;
            ShadowLabel.Position = new(Label.Size.X + Sep, 0f);
            ShadowLabel.Text = Label.Text;
        });
    }
    
    private bool ResetFlag { get ;set; } = false;
    private float ResetOffset { get ;set; } = 0f;

    public void ResetScroll(float offset = 0f)
    {
        ResetFlag = true;
        ResetOffset = offset;
    }
}