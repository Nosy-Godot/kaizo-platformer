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
        var vel = _context.Velocity;
        var wallSliding = _context.IsWallSliding();

        if (wallSliding)
        {
            if (_wasWallSliding == false)
                vel.Y = 0;
            
            vel.Y = Mathf.MoveToward(vel.Y, _context.MaxWallSlideSpeed, _context.Acceleration * fixedDelta);
        }
        else
        {
            vel = _context.ProcessGravity(fixedDelta, vel);
        }

        vel = _context.ProcessInputMovement(fixedDelta, vel, _context.AirAcceleration, _context.AirDecceleration);
        _context.TryMoveAndSlide(vel);

        
        _wasWallSliding = wallSliding;
    }
}
