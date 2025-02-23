using System;
using System.Collections.Generic;
using System.Threading;
using Component;
using Global;
using GodotTask;
using Utils;

namespace Godot;

[GlobalClass]
public partial class MenuRoot : Control
{
    /// <summary>
    /// Recommend to select an default rect to enter the menu
    /// </summary>
    [ExportCategory("MenuRoot")]
    [Export]
    public bool Disabled
    {
        get => _Disabled;
        set
        {
            _Disabled = value;
            Active = !value;
        }
    }
    
    private bool _Disabled = true;
    
    [ExportGroup("GuiBindings")]
    [Export]
    public MenuScrollContainer ScrollContainer { get; set; }
    
    [Export]
    public MenuRectList RectList { get; set; }
    
    [Export]
    public MenuAnim Anim { get; set; }
    
    [Export]
    public MenuControl Control { get; set; }
    
    [ExportGroup("Settings")]
    [Export]
    public bool VerticalEnabled { get; set; } = true;
    
    [Export]
    public bool HorizontalEnabled { get; set; } = false;
    
    [Export]
    public bool VerticalLooped { get; set; } = false;
    
    [Export]
    public bool HorizontalLooped { get; set; } = false;
    
    [Export]
    public float OrthogonalWeight { get; set; } = 0.5f;
    
    [ExportGroup("ContinuousMoving", "Continuous")]
    [Export]
    public double ContinuousMoveDelay { get ; set; } = 0.5d;
    
    /// <summary>
    /// should not be changed during runtime
    /// </summary>
    [Export]
    public double ContinuousMoveInterval { get ; private set; } = 0.1d;
    
    [Signal]
    public delegate void MovedEventHandler();
    
    [Signal]
    public delegate void SelectedEventHandler(MenuRect rect);
    
    [Signal]
    public delegate void CastedEventHandler();
    
    [Signal]
    public delegate void ExitedEventHandler();
    
    private const int MinWaitFrame = 4;
    
    private CTask CurrentAnimTask;
    private bool Active;

    public void Cast(MenuRect defaultRect, bool quick = false)
    {
        if (Active) return;
        Active = true;
    
        if (IsInstanceValid(defaultRect) && HasRect(defaultRect))
            CurrentRect = defaultRect;
        
        RectList?.Sort();
        ScrollContainer?.ForceUpdate();
        
        CurrentAnimTask?.Cancel();
        CurrentAnimTask = new(async ct =>
        {
            if (quick)
            {
                QuickShow();
                await Async.WaitPhysicsFrame(this, MinWaitFrame, ct);
            }
            else await Appear(ct);
            Disabled = false;
            EmitSignal(SignalName.Casted);
        });
    }

    public void Exit(bool quick)
    {
        if (!Active) return;
        Disabled = true;
        
        CurrentAnimTask?.Cancel();
        CurrentAnimTask = new(async ct =>
        {
            if (quick)
            {
                QuickHide();
                await Async.WaitPhysicsFrame(this, MinWaitFrame, ct);
            }
            else await Disappear(ct);

            EmitSignal(SignalName.Exited);
        });
    }

    public async GDTask CastAsync(MenuRect defaultRect = null, bool quick = false)
    {
        if (Active) return;
        
        Cast(defaultRect, quick);
        await GDTask.ToSignal(this, SignalName.Casted);
    }
    
    public async GDTask ExitAsync(bool quick = false)
    {
        if (!Active) return;
        
        Exit(quick);
        await GDTask.ToSignal(this, SignalName.Exited);
    }

    private async GDTask Appear(CancellationToken ct)
    {
        if (IsInstanceValid(Anim))
        {
            await Anim.Appear(this, ct);
            return;
        }
    
        await Async.WaitPhysicsFrame(this, MinWaitFrame, ct);
        Show();
    }
    
    private async GDTask Disappear(CancellationToken ct)
    {
        if (IsInstanceValid(Anim))
        {
            await Anim.Disappear(this, ct);
            return;
        }
        
        Hide();
        await Async.WaitPhysicsFrame(this, MinWaitFrame, ct);
    }

    private void QuickShow()
    {
        if (IsInstanceValid(Anim))
        {
            Anim.QuickShow(this);
            return;
        }
        
        Show();
    }
    
    private void QuickHide()
    {
        if (IsInstanceValid(Anim))
        {
            Anim.QuickHide(this);
            return;
        }
    
        Hide();
    }
    
    // for signal calls
    public void Cast() => Cast(null);
    public void QuickCast() => Cast(null, true);
    public void Exit() => Exit(false);
    public void QuickExit() => Exit(true);

    private HashSet<MenuRect> Rects = [];
    
    private MenuRect CurrentRect;
    public MenuRect GetCurrentRect() => CurrentRect;
    public void ResetCurrentRect() => CurrentRect = null;
    public void SetCurrentRect(MenuRect rect) => CurrentRect = rect;
    
    private bool IsRectSelectable(MenuRect rect)
        => IsInstanceValid(rect) && HasRect(rect) && rect.IsSelectable();

    public void Focus(MenuRect rect)
    {
        if (CurrentRect == rect || !IsRectSelectable(rect)) return;
        var isValid = IsInstanceValid(CurrentRect) && CurrentRect.IsSelectable();
        CurrentRect = rect;
        
        // treat move from invalid state as init and should not emit signal
        if (isValid)
            EmitSignal(SignalName.Moved);
    }

    public void Select(MenuRect rect = null)
    {
        rect ??= CurrentRect;
        if (!IsRectSelectable(rect)) return;
        EmitSignal(SignalName.Selected, rect);
        rect.EmitSignal(MenuRect.SignalName.Selected);
    }
    
    public void AddRect(MenuRect rect)
    {
        rect.Root = this;
        Rects.Add(rect);
        rect.TreeExited += () => RemoveRect(rect);
    }

    public void AddRects(IEnumerable<MenuRect> rects)
    {
        foreach (var rect in rects) AddRect(rect);
    }

    public void AddRects(Node root, Func<MenuRect, bool> filter = null)
    {
        foreach (var node in root.GetChildrenRecursively())
        {
            if (node is MenuRect rect && (filter == null || filter(rect)))
            {
                AddRect(rect);
            }
        }
    }
    
    public bool HasRect(MenuRect rect) => Rects.Contains(rect);
    
    public void RemoveRect(MenuRect rect)
    {
        if (Rects.Remove(rect)) rect.Root = null;
    }

    public void ClearRects()
    {
        foreach (var rect in Rects)
        {
            rect.Root = null;
        }
        Rects.Clear();
    }
    
    public IEnumerable<MenuRect> GetRects() => Rects;

    public bool IsMovingLeft()
    {
        if (IsInstanceValid(Control)) return Control.IsMovingLeft();
        return Input.IsActionPressed("Left");
    }

    public bool IsMovingRight()
    {
        if (IsInstanceValid(Control)) return Control.IsMovingRight();
        return Input.IsActionPressed("Right");
    }

    public bool IsMovingUp()
    {
        if (IsInstanceValid(Control)) return Control.IsMovingUp();
        return Input.IsActionPressed("Up");
    }

    public bool IsMovingDown()
    {
        if (IsInstanceValid(Control)) return Control.IsMovingDown();
        return Input.IsActionPressed("Down");
    }

    public bool IsSelected()
    {
        if (IsInstanceValid(Control)) return Control.IsSelected();
        return Input.IsActionJustPressed("Select");
    }
    
    private Tracker<Vector2I> ContinuousDirTracker;
    private STimer ContinuousMoveTimer;

    private void MoveProcess(bool disabled, double delta)
    {
        ContinuousDirTracker ??= new(Vector2I.Zero);
        if (ContinuousMoveTimer == null && ContinuousMoveDelay > 0d && ContinuousMoveInterval > 0d)
        {
            ContinuousMoveTimer = new(ContinuousMoveInterval);
        }

        if (disabled)
        {
            ContinuousMoveTimer?.Clear();
            ContinuousDirTracker.Reset(Vector2I.Zero);
            return;
        }
        
        var x = HorizontalEnabled ? Convert.ToInt16(IsMovingRight())
                  - Convert.ToInt16(IsMovingLeft()) : 0;
        var y = x == 0 && VerticalEnabled ? Convert.ToInt16(IsMovingDown())
                  - Convert.ToInt16(IsMovingUp()) : 0;
        var dir = new Vector2I(x, y);

        if (ContinuousDirTracker.Update(dir, delta))
        {
            ContinuousMoveTimer?.Clear();
            TryMove(dir, HorizontalLooped, VerticalLooped);
        }
        else if (ContinuousMoveTimer != null && ContinuousDirTracker.Time >= ContinuousMoveDelay)
        {
            if (ContinuousMoveTimer.Update(delta))
            {
                TryMove(dir, HorizontalLooped, VerticalLooped);
            }
        }
    }

    private bool TryMove(Vector2I dir, bool hLoop = false, bool vLoop = false)
    {
        var current = CurrentRect;
        if (current == null) return false;
        
        // using neighbours first

        if (dir.X > 0)
        {
            if (TryMoveTo(current.GetRightRect()))
                return true;
        }
        
        if (dir.X < 0)
        {
            if (TryMoveTo(current.GetLeftRect()))
                return true;
        }
        
        if (dir.Y > 0)
        {
            if (TryMoveTo(current.GetDownRect()))
                return true;
        }

        if (dir.Y < 0)
        {
            if (TryMoveTo(current.GetUpRect()))
                return true;
        }
        
        // find nearest one with weighted projection
        
        var c = current.GlobalPosition + current.Size / 2f;
        var d = -1f;
        var r = current;
        foreach (var rect in Rects)
        {
            if (rect == current || !rect.IsSelectable()) continue;
            
            var p = rect.GlobalPosition + rect.Size / 2f;
            var proj = (p - c).Dot(dir);
            if (proj <= 0f) continue;
            if (HorizontalEnabled && VerticalEnabled)
                proj += OrthogonalWeight * Math.Abs((p-c).Dot(
                    new Vector2(dir.X, dir.Y).Orthogonal()));
            if (d < 0f || proj < d)
            {
                d = proj;
                r = rect;
            }
        }
        
        if (TryMoveTo(r)) return true;
        
        // nothing found, try loop
        
        if (dir.X != 0 && hLoop)
        {
            var dx = new Vector2(dir.X, 0f);
            d = -1f;
            
            foreach (var rect in Rects)
            {
                if (rect == current || !rect.IsSelectable()) continue;
            
                var p = rect.GlobalPosition + rect.Size / 2f;
                var proj = (p - c).Dot(-dx);
                if (proj > d)
                {
                    d = proj;
                    r = rect;
                }
            }
            
            if (TryMoveTo(r)) return true;
        }
        
        if (dir.Y != 0 && vLoop)
        {
            var dy = new Vector2(0f, dir.Y);
            d = -1f;
            
            foreach (var rect in Rects)
            {
                if (rect == current || !rect.IsSelectable()) continue;
            
                var p = rect.GlobalPosition + rect.Size / 2f;
                var proj = (p - c).Dot(-dy);
                if (proj > d)
                {
                    d = proj;
                    r = rect;
                }
            }
            
            if (TryMoveTo(r)) return true;
        }
        
        return false;
    }
    
    private bool TryMoveTo(MenuRect rect)
    {
        if (!IsRectSelectable(rect)) return false;
        if (CurrentRect != rect)
        {
            Focus(rect);
            return true;
        }
        return false;
    }

    private void TryFallback()
    {
        if (CurrentRect == null || !IsInstanceValid(CurrentRect) 
            || !HasRect(CurrentRect))
        {
            // fallback to first selectable rect
                    
            foreach (var rect in Rects)
            {
                if (rect.IsSelectable())
                {
                    Focus(rect);
                    break;
                }
            }
        }
        else if (!CurrentRect.IsSelectable())
        {
            // fallback to previous selectable rect
                    
            var result = false;
            if (HorizontalEnabled)
            {
                result = TryMove(new Vector2I(-1, 0));
                if (!result) result = TryMove(new Vector2I(1, 0));
            }

            if (!result && VerticalEnabled)
            {
                result = TryMove(new Vector2I(0, -1));
                if (!result) TryMove(new Vector2I(0, 1));
            }
        }
    }
    
    public MenuRoot() : base()
    {
        TreeEntered += () =>
        {
            this.AddPhysicsProcess((delta) =>
            {
                if (!Disabled) TryFallback();
                
                MoveProcess(Disabled, delta);
                
                if (!Disabled && IsSelected()) Select();
            });
        };
    }
}