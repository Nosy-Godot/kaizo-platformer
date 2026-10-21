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
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        var dir = _dashDir;
        _context.Velocity = dir * _context.Speed * 3;
        _context.MoveAndSlide();
    }
}
