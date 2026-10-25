using Godot;

public partial class CameraArea : Area2D
{
    public Rect2 Bounds { get; private set; }

    public override void _Ready()
    {
        // Only the rect matters; the area never takes part in physics queries.
        Monitoring = false;
        Monitorable = false;

        foreach (var child in GetChildren())
        {
            if (child is not CollisionShape2D { Shape: RectangleShape2D rect } shape) continue;

            var size = rect.Size * shape.GlobalScale.Abs();
            Bounds = new Rect2(shape.GlobalPosition - size / 2, size);
            return;
        }

        GD.PushWarning($"{Name} needs a CollisionShape2D child with a RectangleShape2D.");
    }
}
