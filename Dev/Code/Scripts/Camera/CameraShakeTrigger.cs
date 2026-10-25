using Godot;

public partial class CameraShakeTrigger : Area2D
{
    [Export] public GameCamera Camera;
    [Export] public float Trauma = 0.6f;

    public override void _Ready() => BodyEntered += OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player) Camera?.AddTrauma(Trauma);
    }
}
