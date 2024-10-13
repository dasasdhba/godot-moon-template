namespace Godot;

// menu item managed by MenuControl

[GlobalClass]
public partial class MenuItem : Node
{
    [ExportCategory("MenuItem")]
    [Export]
    public bool Disabled { get ;set; } = false;
    
    [Signal]
    public delegate void SelectedEventHandler();
    
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