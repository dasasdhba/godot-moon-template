using Godot;
using Godot.Collections;

namespace Global;

/// <summary>
/// Save item editing node.
/// </summary>
[GlobalClass]
public partial class SaveItem : Node
{
    [ExportCategory("SaveItem")]
    [Export]
    public Dictionary<string, Variant> Items { get; set; } = new();

    [Export]
    public bool DebugOverride { get; set; } = false;

    [Export]
    public Dictionary<string, Variant> DebugItems { get; set; } = new();

    public override void _EnterTree()
    {
        if (OS.IsDebugBuild() && DebugOverride)
        {
            foreach (string key in DebugItems.Keys)
            {
                Items[key] = DebugItems[key];
            }
        }
    }

}