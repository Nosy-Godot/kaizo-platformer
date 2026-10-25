using System.Collections.Generic;
using Godot;

public partial class CameraZone : CameraArea
{
    private static readonly List<CameraZone> _all = new();

    [Export] public int ZonePriority;
    [Export] public Vector2 TargetOffset;
    [Export] public bool LockX;
    [Export] public bool LockY;
    [Export] public float LookAheadMult = 1f;

    public override void _EnterTree() => _all.Add(this);
    public override void _ExitTree() => _all.Remove(this);

    public static CameraZone Find(Vector2 point)
    {
        CameraZone best = null;
        foreach (var zone in _all)
        {
            if (!zone.Bounds.HasPoint(point)) continue;
            if (best == null || zone.ZonePriority > best.ZonePriority) best = zone;
        }

        return best;
    }

    public Vector2 Apply(Vector2 target)
    {
        var center = Bounds.GetCenter();
        if (LockX) target.X = center.X;
        if (LockY) target.Y = center.Y;
        return target + TargetOffset;
    }
}
