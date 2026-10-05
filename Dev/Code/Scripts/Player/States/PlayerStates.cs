using NosyCore.FSM;

public class PlayerGroundedState : BaseState<Player>
{
    public PlayerGroundedState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        var vel = _context.Velocity;
        vel = _context.ProcessGravity(fixedDelta, vel);
        vel = _context.TryJump(vel);
        vel = _context.ProcessInputMovement(fixedDelta, vel, _context.Acceleration, _context.Decceleration);

        _context.TryMoveAndSlide(vel);
    }
}

public class PlayerAirState : BaseState<Player>
{
    public PlayerAirState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        var vel = _context.Velocity;
        vel = _context.ProcessGravity(fixedDelta, vel);
        vel = _context.ProcessInputMovement(fixedDelta, vel, _context.AirAcceleration, _context.AirDecceleration);
        _context.TryMoveAndSlide(vel);
    }
}

