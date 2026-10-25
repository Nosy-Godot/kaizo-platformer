using System.Collections.Generic;
using Godot;

public partial class CameraRoom : CameraArea
{
    private static readonly List<CameraRoom> _all = new();
    public static IReadOnlyList<CameraRoom> All => _all;

    [Export] public Marker2D Spawn;

    public override void _EnterTree() => _all.Add(this);
    public override void _ExitTree() => _all.Remove(this);
}
