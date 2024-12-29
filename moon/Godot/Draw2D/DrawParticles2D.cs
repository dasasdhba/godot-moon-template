using Godot.Collections;
using Utils;

namespace Godot;

public partial class DrawParticles2D : DrawProcess2D
{
    [ExportCategory("DrawParticles2D")]
    [Export]
    public bool Emitting { get; set; } = false;
    
    [Export(PropertyHint.Range, "0.001,4096,or_greater")]
    public double Interval { get ;set; } = 0.02d;
    
    /// <summary>
    /// Make AddDrawProcess call here.
    /// </summary>
    protected virtual void ParticleSetup()
    {
        
    }

    public DrawParticles2D() : base()
    {
        TreeEntered += () =>
        {
            var timer = this.ActionRepeat(Interval, ParticleSetup);
            timer.Paused = !Emitting;
            timer.ProcessCallback = ProcessCallback == Draw2DProcessCallback.Idle ?
                UTimer.UTimerProcessCallback.Idle : UTimer.UTimerProcessCallback.Physics;
            this.AddProcess(() =>
            {
                timer.Paused = !Emitting;
                timer.ProcessCallback = ProcessCallback == Draw2DProcessCallback.Idle ?
                    UTimer.UTimerProcessCallback.Idle : UTimer.UTimerProcessCallback.Physics;
            }, () => ProcessCallback == Draw2DProcessCallback.Physics);
        };
    }
}