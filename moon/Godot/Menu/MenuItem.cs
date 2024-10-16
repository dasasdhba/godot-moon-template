namespace Godot;

// menu item managed by MenuControl

[GlobalClass]
public partial class MenuItem : Node
{
    [ExportCategory("MenuItem")]
    [Export]
    public bool Disabled
    {
        get => _Disabled;
        set
        {
            _Disabled = value;
            if (_Disabled && IsFocus())
            {
                Menu.TryMoveNext(false);
                if (Menu.CurrentItem == this) Menu.TryMovePrev(false);
                if (Menu.CurrentItem == this) Menu.CurrentItem = null;
            }
        }
    }
    
    private bool _Disabled = false;
    
    [Signal]
    public delegate void SelectedEventHandler();
    
    // set by MenuItemRect node
    public MenuItemRect Rect { get; set; }
    
    // set by MenuControl node
    public MenuControl Menu { get; set; }
    public bool IsFocus() => IsInstanceValid(Menu) && Menu.CurrentItem == this;

    public void Focus()
    {
        if (IsFocus()) return;
        
        Menu.CurrentItem = this;
        Menu.EmitSignal(MenuControl.SignalName.Moved);
    }
}