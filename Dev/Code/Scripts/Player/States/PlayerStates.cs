using System;
using Godot;
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

        var direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
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

