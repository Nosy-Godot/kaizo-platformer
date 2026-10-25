using NosyCore.FSM;

public class PlayerGroundedState : BaseState<Player>
{
    public PlayerGroundedState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        // Gravity keeps the body pressed into the floor so IsOnFloor stays stable.
        _context.ProcessGravity(fixedDelta, _context.MaxFall);
        _context.TryJump();
        _context.ProcessRun(fixedDelta, 1f);
        _context.DoMoveAndSlide();
    }
}
