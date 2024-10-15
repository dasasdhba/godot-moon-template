using System.Threading.Tasks;
using Utils;

namespace Godot;

// menu panel appear/disappear animation

[GlobalClass]
public partial class MenuPanel : Control
{
    [ExportCategory("MenuPanel")]
    [Export]
    public MenuControl Menu { get ;set; }

    [Export]
    public bool HideAtStart { get ;set; } = false;
    
    [Signal]
    public delegate void AppearedEventHandler();
    
    [Signal]
    public delegate void DisappearedEventHandler();

    public MenuPanel() : base()
    {
        TreeEntered += () =>
        {
            if (HideAtStart) QuickHide();
            else QuickShow();
        };
    }

    public void EnableMenu()
    {
        if (IsInstanceValid(Menu))
            Menu.Disabled = false;
    }

    public void DisableMenu()
    {
        if (IsInstanceValid(Menu))    
            Menu.Disabled = true;
    }

    public virtual void QuickShow()
    {
        Show();
        EnableMenu();
    }

    public virtual void QuickHide()
    {
        Hide();
        DisableMenu();
    }
    
    protected const string AppearTag = "AppearTag";
    protected const string DisappearTag = "DisappearTag";

    public async Task Appear()
    {
        await AppearAsync().Forget(this, AppearTag);
        EnableMenu();
        EmitSignal(SignalName.Appeared);
    }

    protected virtual async Task AppearAsync(double time = 0.3d)
    {
        Async.Clear(this, AppearTag);
        
        Show();
        
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate", Modulate with { A = 1f }, time);
        await Async.Wait(this, tween, AppearTag);
    }

    public async Task Disappear()
    {
        DisableMenu();
        await DisappearAsync().Forget(this, DisappearTag);
        EmitSignal(SignalName.Disappeared);
    }

    protected virtual async Task DisappearAsync(double time = 0.3d)
    {
        Async.Clear(this, DisappearTag);

        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate", Modulate with { A = 0f }, time);
        await Async.Wait(this, tween, DisappearTag);

        Hide();
    }
}