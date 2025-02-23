using Godot.Collections;
using Utils;

namespace Godot;

[GlobalClass]
public abstract partial class Inputer : Node
{
    public enum InputBufferProcessCallback
    {
        Idle,
        Physics
    }

    [ExportCategory("Inputer")]
    [Export]
    public InputBufferProcessCallback BufferProcessMode { get; set; }
        = InputBufferProcessCallback.Physics;
        
    [Export]
    public Array<string> BufferPauseGuards { get; set; } = [];

    public struct InputKey
    {
        public bool Pressed { get; set; } = false;
        public bool JustPressed { get; set; } = false;
        public bool JustReleased { get; set; } = false;

        public InputKey() { }

        public InputKey(bool pressed, bool justPressed, bool justReleased)
            => (Pressed, JustPressed, JustReleased) = (pressed, justPressed, justReleased);
    }

    public abstract InputKey GetKey(string key);

    private System.Collections.Generic.Dictionary<string, bool> BufferMaps { get ;set; } = new();

    public bool IsKeyPressed(string key, bool buffered = false)
    {
        if (!buffered || !BufferMaps.TryGetValue(key, out bool value) || !value) 
            return GetKey(key).Pressed;

        return false;
    }

    public bool IsKeyBuffered(string key) => IsKeyPressed(key, true);

    public void SetKeyBuffered(string key) => BufferMaps[key] = true;

    public void BufferProcess()
    {
        var dict = BufferMaps;
        foreach (var key in dict.Keys)
        {
            if (!dict[key]) continue;

            dict[key] = GetKey(key).Pressed;
        }
    }
    
    // add buffer guards when paused
    private partial class BufferPauseNode : Node
    {
        private Inputer Inputer;
    
        public BufferPauseNode(Inputer inputer) : base()
        {
            Inputer = inputer;
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Inputer.CanProcess()) return;
            
            foreach (var key in Inputer.BufferPauseGuards)
            {
                Inputer.SetKeyBuffered(key);
            }
        }
    }

    public Inputer() : base()
    {
        TreeEntered += () =>
        {
            AddChild(new BufferPauseNode(this));
            this.AddProcess(BufferProcess, () => BufferProcessMode == InputBufferProcessCallback.Physics);
        };
    }
}