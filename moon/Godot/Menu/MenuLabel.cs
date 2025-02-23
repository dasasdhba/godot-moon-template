using Utils;

namespace Godot;

[GlobalClass]
public partial class MenuLabel : Label
{
    [ExportCategory("MenuItemLabel")]
    [Export]
    public MenuRect ItemRect { get ;set; }
    
    [Export]
    public Color GeneralColor { get ;set; } = Colors.White;
    
    [Export]
    public Color FocusColor { get ;set; } = Colors.Yellow;
    
    [Export]
    public Color DisabledColor { get ;set; } = new(0.5f, 0.5f, 0.5f);

    public MenuLabel() : base()
    {
        TreeEntered += () =>
        {
            ItemRect ??= this.FindParent<MenuRect>();
            
            this.AddPhysicsProcess(() =>
            {
                if (!IsInstanceValid(ItemRect)) return;
                
                var current = GetThemeColor("font_color");
                var color = ItemRect.IsDisabled() ? DisabledColor :
                            ItemRect.IsFocus() ? FocusColor : GeneralColor;
                if (current == color) return;
                AddThemeColorOverride("font_color", color);
            });
        };
    }
}