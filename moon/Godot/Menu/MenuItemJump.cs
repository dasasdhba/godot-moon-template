namespace Godot;

[GlobalClass]
public partial class MenuItemJump : MenuItem
{
    [ExportCategory("MenuItemJump")]
    [Export]
    public MenuControl NextMenu { get; set; }
    
    [Export]
    public MenuItem NextItem { get; set; }
    
    [Export]
    public bool Parallel { get ;set; } = true;

    public MenuItemJump() : base()
    {
        Ready += () =>
        {
            SignalSelected += () => JumpToNextMenu();
        };
    }

    public async void JumpToNextMenu()
    {
        if (Parallel)
        {
            var p = Menu.GuiDisappear();
            await NextMenu.GuiAppear(NextItem);
            await p;
            return;
        }
        
        await Menu.GuiDisappear();
        await NextMenu.GuiAppear(NextItem);
    }

}