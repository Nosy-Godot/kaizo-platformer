using Godot;

public static class PlayerInput
{
    private const float Deadzone = 0.3f;

    public static int MoveX => Digital(Input.GetAxis("Move_Left", "Move_Right"));
    public static int MoveY => Digital(Input.GetAxis("Move_Up", "Move_Down"));

    public static Vector2 DashAim => new Vector2(MoveX, MoveY).Normalized();

    public static bool WasJumpPressed => Input.IsActionJustPressed("Jump");
    public static bool JumpPressed => Input.IsActionPressed("Jump");
    public static bool DashPressed => Input.IsActionJustPressed("Dash");

    private static int Digital(float axis) => Mathf.Abs(axis) < Deadzone ? 0 : Mathf.Sign(axis);
}
