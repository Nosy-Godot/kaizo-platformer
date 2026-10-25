using NosyCore.FSM;

public class PlayerAirState : BaseState<Player>
{
    public PlayerAirState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        _context.ProcessVarJump();

        if (!_context.TryJump())
        {
            _context.TryWallJump();
        }

        _context.ProcessGravity(fixedDelta, _context.UpdateAirMaxFall(fixedDelta));
        _context.ProcessRun(fixedDelta, _context.AirAccelMult);
        _context.DoMoveAndSlide();
    }
}
