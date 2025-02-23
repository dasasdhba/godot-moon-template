using GodotTask;
using Utils;

namespace Godot;

[GlobalClass]
public partial class MenuRect : Control
{
    [ExportCategory("MenuRect")]
    [Export]
    public MenuRoot Root { get ;set; }
    
    [Export]
    public bool AutoFindRoot { get ;set; } = true;
    
    [Export]
    public bool Disabled { get ;set; } = false;
    
    [ExportGroup("Neighbors")]
    [Export]
    public MenuRect LeftRect { get ;set; }
    
    [Export]
    public MenuRect RightRect { get ;set; }
    
    [Export]
    public MenuRect UpRect { get ;set; }
    
    [Export]
    public MenuRect DownRect { get ;set; }
    
    [Signal]
    public delegate void SelectedEventHandler();
    
    /// <summary>
    /// Useful for wait disappear and launch another menu
    /// </summary>
    [Signal]
    public delegate void ExitedEventHandler();
    
    protected virtual bool IsShortcutPressed() => false;

    public MenuRect() : base()
    {
        TreeEntered += () =>
        {
            if (Root == null && AutoFindRoot)
            {
                Root = this.FindParent<MenuRoot>();
            }
            Root?.AddRect(this);
            
            this.AddPhysicsProcess(() =>
            {
                if (!IsSelectable())
                {
                    MouseFilter = MouseFilterEnum.Ignore;
                    return;
                }
                MouseFilter = MouseFilterEnum.Pass;

                if (IsShortcutPressed())
                {
                    Root.Select(this);
                }
            });
        };
    }
    
    public bool IsDisabled() => Disabled || !Visible;
    public bool IsFocus() => Root?.GetCurrentRect() == this;
    public bool IsSelectable() => !IsDisabled() && IsInstanceValid(Root) && !Root.Disabled;
    
    public override void _GuiInput(InputEvent e)
    {
        if (!IsSelectable()) return;
        
        if (e is InputEventMouse mouse)
        {
            Root.Focus(this);
            if (mouse is InputEventMouseButton button
                && button.ButtonIndex == MouseButton.Left && button.Pressed)
            {
                Root.Select(this);
            }
            
            AcceptEvent();
        }
    }

    public MenuRect GetRightRect()
    {
        var rect = RightRect;
        while (rect != null && !rect.IsSelectable())
        {
            rect = rect.RightRect;
        }
        return rect;
    }
    
    public MenuRect GetLeftRect()
    {
        var rect = LeftRect;
        while (rect != null && !rect.IsSelectable())
        {
            rect = rect.LeftRect;
        }
        return rect;
    }
    
    public MenuRect GetUpRect()
    {
        var rect = UpRect;
        while (rect != null && !rect.IsSelectable())
        {
            rect = rect.UpRect;
        }
        return rect;
    }
    
    public MenuRect GetDownRect()
    {
        var rect = DownRect;
        while (rect != null && !rect.IsSelectable())
        {
            rect = rect.DownRect;
        }
        return rect;
    }

    private void LaunchRoot(bool quick)
    {
        if (IsDisabled() || Root is { Disabled: false }) return;
        Root.Cast(this, quick);
    }
    
    // for signal calls
    public void Launch() => LaunchRoot(false);
    public void QuickLaunch() => LaunchRoot(true);

    private async GDTask ExitAsync(bool quick)
    {
        if (IsDisabled() || Root is { Disabled: true }) return;
        await Root.ExitAsync(quick);
        EmitSignal(SignalName.Exited);
    }
    
    public void Exit() => ExitAsync(false).Forget();
    public void QuickExit() => ExitAsync(true).Forget();
}