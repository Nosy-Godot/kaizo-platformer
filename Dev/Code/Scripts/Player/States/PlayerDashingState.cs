using Godot;
using NosyCore.FSM;

public class PlayerDashingState : BaseState<Player>
{
    public bool DashEnded => Time.GetTicksMsec() >= _dashEndTime;
    private Vector2 _enterVel;
    private Vector2 _dashDir;
    private ulong _dashEndTime;

    public PlayerDashingState(Player context) : base(context)
    {
    }

    public override void OnEnter(IState previousState)
    {
        _enterVel = _context.Velocity;

        var direction = PlayerInput.MoveAxis;
        _dashDir = direction;

        _dashEndTime = Time.GetTicksMsec() + 100;

        var dir = _dashDir;
        _context.DynamicVel = dir * _context.Speed * 3;
        _context.KinematicVel = Vector2.Zero;
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        _context.DoMoveAndSlide();
    }
}
