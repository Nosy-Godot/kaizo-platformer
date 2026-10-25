using NosyCore.FSM;

public class PlayerWallSlideState : BaseState<Player>
{
    public PlayerWallSlideState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        _context.ProcessRun(fixedDelta, _context.AirAccelMult);

        if (!_context.TryWallJump())
        {
            _context.ProcessGravity(fixedDelta, _context.WallSlideMaxFall);
        }

        _context.DoMoveAndSlide();
    }
}
