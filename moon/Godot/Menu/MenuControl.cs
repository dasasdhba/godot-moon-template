using System;
using System.Collections.Generic;
using Utils;

namespace Godot;

// provide a simplified workflow for creating user interfaces in Godot
// it only handles the logic of menu

[GlobalClass]
public partial class MenuControl : Node
{
    [ExportCategory("MenuControl")]
    [Export]
    public MenuItem DefaultItem { get; set; } = null;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    [Export]
    public bool LoopSelection { get; set; } = false;
    
    [ExportGroup("ContinuousMoving", "Continuous")]
    [Export]
    public double ContinuousMoveDelay { get ; set; } = 0.5d;
    
    [Export]
    public double ContinuousMoveInterval { get ; set; } = 0.1d;
    
    [Signal]
    public delegate void MovedEventHandler();
    
    public MenuItem CurrentItem { get ;set; }
    public List<MenuItem> Items { get ;set; } = new();
    
    private int ContinuousMoveDir { get ; set; } = 0;
    private double ContinuousMoveTimer { get ; set; } = 0d;
    private double ContinuousMoveInternalTimer { get ; set; } = 0d;

    public MenuControl() : base()
    {
        ChildEnteredTree += child =>
        {
            if (child is MenuItem item)
            {
                DefaultItem ??= item;
                item.Menu = this;
                Items.Add(item);
            }
        };
    
        TreeEntered += () =>
        {
            this.AddPhysicsProcess((delta) =>
            {
                if (Disabled)
                {
                    ContinuousMoveDir = 0;
                    ContinuousMoveTimer = 0d;
                    ContinuousMoveInternalTimer = 0d;
                    return;
                }
            
                var dir = Convert.ToInt16(IsMovingNext())
                    - Convert.ToInt16(IsMovingPrev());
                if (dir != ContinuousMoveDir)
                {
                    ContinuousMoveDir = dir;
                    ContinuousMoveTimer = 0d;
                    ContinuousMoveInternalTimer = 0d;
                }
                else
                {
                    dir = 0;
                    if (ContinuousMoveTimer < ContinuousMoveDelay)
                    {
                        ContinuousMoveTimer += delta;
                    }
                    else
                    {
                        ContinuousMoveInternalTimer += delta;
                        if (ContinuousMoveInternalTimer >= ContinuousMoveInterval)
                        {
                            ContinuousMoveInternalTimer = 0d;
                            dir = ContinuousMoveDir;
                        }
                    }
                }
                
                if (dir > 0) TryMoveNext();
                else if (dir < 0) TryMovePrev();
                
                if (IsSelected()) Select();
            });
        };
        
        Ready += () => CurrentItem = DefaultItem;
    }
    
    public virtual bool IsMovingPrev() => Input.IsActionPressed("Up");
    public virtual bool IsMovingNext() => Input.IsActionPressed("Down");
    public virtual bool IsSelected() => Input.IsActionJustPressed("Select");
    
    public void Select() => CurrentItem.EmitSignal(MenuItem.SignalName.Selected);

    public void TryMoveNext()
    {
        var index = Items.IndexOf(CurrentItem);
        for (int i = index + 1; i < Items.Count; i++)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                EmitSignal(SignalName.Moved);
                return;
            }
        }
        
        if (!LoopSelection) return;
        
        for (int i = 0; i < index; i++)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                EmitSignal(SignalName.Moved);
                return;
            }
        }
    }

    public void TryMovePrev()
    {
        var index = Items.IndexOf(CurrentItem);
        for (int i = index - 1; i >= 0; i--)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                EmitSignal(SignalName.Moved);
                return;
            }
        }

        if (!LoopSelection) return;

        for (int i = Items.Count - 1; i > index; i--)
        {
            if (!Items[i].Disabled)
            {
                CurrentItem = Items[i];
                EmitSignal(SignalName.Moved);
                return;
            }
        }
    }
}