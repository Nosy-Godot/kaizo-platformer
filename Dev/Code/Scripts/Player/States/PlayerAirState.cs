using Godot;
using NosyCore.FSM;

public class PlayerAirState : BaseState<Player>
{
    private bool _wasWallSliding;

    public PlayerAirState(Player context) : base(context)
    {
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        var wallSliding = ProcessWallSlide(fixedDelta, ref _context.DynamicVel);

        if (wallSliding == false)
        {
            _context.ProcessGravity(fixedDelta);
            _context.ProcessInputMovement(fixedDelta, _context.AirAcceleration, _context.AirDecceleration);
        }
        else if (PlayerInput.WasJumpPressed)
        {
            var normal = _context.GetWallNormal();
            _context.DynamicVel += normal * _context.WallJumpHorizontalVelocity + Vector2.Up * _context.WallJumpVerticalVelocity;
        }

        _context.DoMoveAndSlide();

        
        _wasWallSliding = wallSliding;
    }

    private bool ProcessWallSlide(float fixedDelta, ref Vector2 vel)
    {
        var wallSliding = _context.IsWallSliding();
        if (wallSliding)
        {
            if (_wasWallSliding == false)
                vel.Y = 0;
            
            vel.Y = Mathf.MoveToward(vel.Y, _context.MaxWallSlideSpeed, _context.Acceleration * fixedDelta);

            return true;
        }

        return false;
    }
}
