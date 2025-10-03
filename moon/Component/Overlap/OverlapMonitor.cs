using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Component;

[GlobalClass]
public abstract partial class OverlapMonitor : OverlapStaticMonitor
{
    /// <summary>
    /// if true, the monitor will be disabled after first overlapping
    /// </summary>
    [ExportCategory("OverlapMonitor")]
    [Export]
    public bool Oneshot { get ;set; } = false;
    
    /// <summary>
    /// enable self report, Overlapped Signal and OnOverlapped call will work
    /// </summary>
    [ExportGroup("Report", "Report")]
    [Export]
    public bool ReportSelf { get ;set; } = true;
    
    /// <summary>
    /// enable receiver report, receiver's MonitorOverlapped will be called
    /// </summary>
    [Export]
    public bool ReportReceiver { get ;set; } = true;
    
    /// <summary>
    /// enable receiver enter report, receiver's MonitorEntered will be emitted
    /// </summary>
    [Export]
    public bool ReportReceiverEnter { get ;set; } = true;
    
    /// <summary>
    /// enable receiver exit report, receiver's MonitorExited will be emitted
    /// </summary>
    [Export]
    public bool ReportReceiverExit { get ;set; } = true;
    
    /// <summary>
    /// Unlike Entered, This will emit even if in first frame
    /// </summary>
    [Signal]
    public delegate void OverlappedEventHandler();
    
    protected virtual void OnOverlapped() { }
    
    /// <summary>
    /// The filter when overlapping and ready to report,
    /// return false to block report, but still keep overlapping query
    /// </summary>
    protected virtual bool GetReceiverFilter(OverlapReceiver r) => true;
    
    /// <summary>
    /// The receiver key which must match with the receiver's
    /// </summary>
    protected abstract string GetReceiverKey();
    
    /// <summary>
    /// The data passed to the receiver when reporting
    /// </summary>
    protected virtual Variant GetReceiverData() => this;
    
    /// <summary>
    /// The filter when overlapping, return false to ignore the result permanently.
    /// the static filter is useless in this class.
    /// </summary>
    protected virtual bool GetFilter(OverlapReceiver r) => true;

    private List<OverlapReceiver> LastReceivers = [];
    
    public override bool IsOverlapping(Vector2 offset)
    {
        var overlapFlag = false;
        var rKey = GetReceiverKey();
        var data = GetReceiverData();
        
        List<OverlapReceiver> receivers = [];

        foreach (var result in Overlap.GetOverlappingObjects(
                     r => OverlapReceiver.HasRef(r.Collider, rKey)
                         && GetFilter(OverlapReceiver.GetRef(r.Collider, rKey)),
                     offset, true
                 ))
        {
            var r = OverlapReceiver.GetRef(result.Collider, rKey);
            if (r.IsDisabled() || !GetReceiverFilter(r)) continue;

            if (ReportSelf && !overlapFlag)
            {
                overlapFlag = true;

                if (!IsOverlapping())
                {
                    EmitSignal(SignalName.Overlapped);
                }
                
                OnOverlapped();
            
                if (Oneshot) Disabled = true;
                if (!ReportReceiver) break;
            }

            if (ReportReceiver)
            {
                if (ReportReceiverEnter && !LastReceivers.Contains(r))
                {
                    r.EmitSignal(OverlapReceiver.SignalName.MonitorEntered, data);
                }
                r.MonitorOverlapped(data);
                
                if (ReportReceiverEnter || ReportReceiverExit)
                    receivers.Add(r);
            }
        }

        if (ReportReceiver)
        {
            if (ReportReceiverExit)
            {
                foreach (var r in LastReceivers.Where(r => !receivers.Contains(r)))
                {
                    r.EmitSignal(OverlapReceiver.SignalName.MonitorExited, data);
                }
            }
            
            LastReceivers = receivers;
        }
        
        return overlapFlag;
    }
}