using Godot;

public static class PlayerInput
{
    public static float MoveXAxis => Input.GetAxis("Move_Left", "Move_Right");
    public static float MoveYAxis => Input.GetAxis("Move_Up", "Move_Down");
    public static Vector2 MoveAxis => Input.GetVector("Move_Left", "Move_Right", "Move_Up", "Move_Down");
    
    public static bool WasJumpPressed => Input.IsActionJustPressed("Jump");
    public static bool JumpPressed => Input.IsActionPressed("Jump");
    public static bool DashPressed => Input.IsActionJustPressed("Dash");
}