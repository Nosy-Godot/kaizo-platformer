using System;
using NosyCore.FSM;

public class PlayerGroundedState : BaseState<Player>
{
    public PlayerGroundedState(Player context) : base(context)
    {
    }

    public override void OnEnter(IState previousState)
    {
        _context.DynamicVel.Y = 0;
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        _context.ProcessGravity(fixedDelta);
        _context.TryJump();
        _context.ProcessInputMovement(fixedDelta, _context.Acceleration, _context.Decceleration);
        _context.DoMoveAndSlide();
    }
}
