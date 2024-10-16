namespace Godot;

[GlobalClass]
public partial class MenuItemRect : Control
{
    [ExportCategory("MenuItemRect")]
    [Export]
    public MenuItem MenuItem { get; set; }

    public MenuItemRect() : base()
    {
        TreeEntered += () =>
        {
            if (MenuItem == null) return;
            
            MenuItem.Rect = this;
        };
    }
    
    public bool IsDisabled() => !IsInstanceValid(MenuItem) || MenuItem.Disabled;
    public bool IsFocus() => IsInstanceValid(MenuItem) && MenuItem.IsFocus();
    public bool IsSelectable() => !IsDisabled() && IsInstanceValid(MenuItem.Menu) && !MenuItem.Menu.Disabled;
    
    public override void _GuiInput(InputEvent e)
    {
        if (!IsSelectable()) return;
        
        if (e is InputEventMouse mouse)
        {
            MenuItem.Focus();
            if (mouse is InputEventMouseButton button
                && button.ButtonIndex == MouseButton.Left && button.Pressed)
            {
                MenuItem.Menu.Select();
            }
            
            AcceptEvent();
        }
    }
}