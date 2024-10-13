using Utils;

namespace Godot;

[GlobalClass]
public partial class MenuItemLabel : Label
{
    [ExportCategory("MenuItemLabel")]
    [Export]
    public MenuItemRect ItemRect { get ;set; }
    
    [Export]
    public Color GeneralColor { get ;set; } = Colors.White;
    
    [Export]
    public Color FocusColor { get ;set; } = Colors.Yellow;
    
    [Export]
    public Color DisabledColor { get ;set; } = Colors.Gray;

    public MenuItemLabel() : base()
    {
        TreeEntered += () =>
        {
            ItemRect ??= this.FindParent<MenuItemRect>();
            
            this.AddProcess(() =>
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