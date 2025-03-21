using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game;

[GlobalClass]
public partial class OverlapMonitor : OverlapStaticMonitor
{
    [ExportCategory("OverlapMonitor")]
    [Export]
    public bool Oneshot { get ;set; } = false;
    
    [ExportGroup("Report", "Report")]
    [Export]
    public bool ReportSelf { get ;set; } = true;
    
    [Export]
    public bool ReportReceiver { get ;set; } = true;
    
    [Export]
    public bool ReportReceiverEnter { get ;set; } = true;
    
    [Export]
    public bool ReportReceiverExit { get ;set; } = true;
    
    /// <summary>
    /// Unlike Entered, This will emit even if in first frame
    /// </summary>
    [Signal]
    public delegate void OverlappedEventHandler();
    
    protected virtual void OnOverlapped() { }
    protected virtual Func<OverlapReceiver, bool> GetReceiverFilter() => null;
    protected virtual Variant GetReceiverData() => this;
    
    private List<OverlapReceiver> LastReceivers = [];
    
    public override bool IsOverlapping(Vector2 offset)
    {
        var overlapFlag = false;
        var filter = GetFilter();
        var rFilter = GetReceiverFilter();
        var data = GetReceiverData();
        
        List<OverlapReceiver> receivers = [];

        foreach (var result in Overlap.GetOverlappingObjects(
                     r => OverlapReceiver.HasRef(r.Collider)
                         && (filter == null || filter(r)),
                     offset, true
                 ))
        {
            var r = OverlapReceiver.GetRef(result.Collider);
            if (r.IsDisabled() || !(rFilter == null || rFilter(r))) continue;

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