using System.Collections.Generic;
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

    private Dictionary<string, bool> BufferMaps { get ;set; } = new();

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

    public Inputer() : base()
    {
        TreeEntered += () =>
        {
            this.AddProcess(BufferProcess, () => BufferProcessMode == InputBufferProcessCallback.Physics);
        };
    }
}