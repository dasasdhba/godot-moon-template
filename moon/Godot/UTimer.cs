using Utils;

namespace Godot;

/// <summary>
/// Alternate of Godot Timer
/// </summary>
[GlobalClass]
public partial class UTimer : Node
{
    [ExportCategory("UTimer")]
    [Export]
    public bool Autostart { get ;set; } = false;

    [Export]
    public bool OneShot { get; set; } = false;

    [Export]
    public bool Paused { get ;set; } = false;

    public enum UTimerProcessCallback { Idle, Physics }

    [Export]
    public UTimerProcessCallback ProcessCallback { get ;set; } = UTimerProcessCallback.Idle;
    
    [Export(PropertyHint.Range, "0.001,4096,or_greater")]
    public double WaitTime { get ;set; } = 1d;

    [Signal]
    public delegate void TimeoutEventHandler();

    public double TimeLeft { get ;set; }

    private bool Active { get ;set; } = false;

    private void TimerProcess(double delta)
    {
        if (Paused || !Active) return;
        
        TimeLeft -= delta;
        if (TimeLeft <= 0d)
        {
            EmitSignal(SignalName.Timeout);

            if (!OneShot)
            {
                TimeLeft += WaitTime;
            }
            else
            {
                Active = false;
            }
        }
    }

    public bool IsStopped() => !Active;

    public void Start(double time = -1d)
    {
        if (time > 0d) WaitTime = time;
        TimeLeft = WaitTime;
        Active = true;
    }

    public void TryStart(double time = -1d)
    {
        if (Active) return;
        Start(time);
    }

    public void Stop() => Active = false;

    public UTimer() : base()
    {
        TreeEntered += () =>
        {
            if (Autostart) Start();
            
            this.AddProcess(TimerProcess, () => ProcessCallback == UTimerProcessCallback.Physics);
        };
    }
}