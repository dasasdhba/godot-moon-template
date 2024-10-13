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
    
    [Signal]
    public delegate void MovedEventHandler();
    
    public MenuItem CurrentItem { get ;set; }
    
    public List<MenuItem> Items { get ;set; } = new();

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
            this.AddPhysicsProcess(() =>
            {
                if (Disabled) return;
            
                var dir = Convert.ToInt16(IsMoveNext())
                    - Convert.ToInt16(IsMovePrev());
                
                if (dir > 0) TryMoveNext();
                else if (dir < 0) TryMovePrev();
                
                if (IsSelect()) Select();
            });
        };
    }
    
    public virtual bool IsMovePrev() => Input.IsActionJustPressed("Up");
    public virtual bool IsMoveNext() => Input.IsActionJustPressed("Down");
    public virtual bool IsSelect() => Input.IsActionJustPressed("Select");
    
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