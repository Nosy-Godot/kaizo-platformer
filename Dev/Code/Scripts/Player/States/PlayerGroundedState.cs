using System;
using NosyCore.FSM;

public class PlayerGroundedState : BaseState<Player>
{
    public PlayerGroundedState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        var vel = _context.Velocity;
        _context.ProcessGravity(fixedDelta, ref vel);
        _context.TryJump(ref vel);
        _context.ProcessInputMovement(fixedDelta, ref vel, _context.Acceleration, _context.Decceleration);

        _context.TryMoveAndSlide(vel);
    }
}
