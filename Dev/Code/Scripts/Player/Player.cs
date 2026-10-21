using Godot;
using NosyCore.FSM;
using System;

public partial class Player : CharacterBody2D, IStateContext
{
	public float Speed = 150.0f;
	public float JumpVelocity = -350.0f;

    [Export] public float Acceleration = 600;
    [Export] public float Decceleration = 500;
    [Export] public float AirAcceleration = 800;
    [Export] public float AirDecceleration = 500;
    [Export] public float OpositeInputMultiplier = 2;

    public float FallLongMult = 0.85f;
    public float FallShortMult = 1.85f;

    [Export] public float MaxWallSlideSpeed = 80f;
    [Export] public float WallJumpHorizontalVelocity = 180f;
    [Export] public float WallJumpVerticalVelocity = -300f;

    private FiniteStateMachine _stateMachine;
    private PlayerGroundedState _groundedState;
    private PlayerAirState _airState;
    private PlayerDashingState _dashState;


    public override void _Ready()
    {
        _groundedState = new PlayerGroundedState(this);
        _airState = new PlayerAirState(this);
        _dashState = new PlayerDashingState(this);
        _stateMachine = new FiniteStateMachine(_groundedState);

        _stateMachine.AddTransition(_airState, _groundedState, new FuncPredicate(() => IsOnFloor()));
        _stateMachine.AddTransition(_groundedState, _airState, new FuncPredicate(() => IsOnFloor() == false));

        var dashPressedPred = new FuncPredicate(() => PlayerInput.DashPressed);
        _stateMachine.AddTransition(_groundedState, _dashState, dashPressedPred);
        _stateMachine.AddTransition(_airState, _dashState, dashPressedPred);
        _stateMachine.AddTransition(_dashState, _groundedState, new FuncPredicate(() => _dashState.DashEnded && IsOnFloor()));
        _stateMachine.AddTransition(_dashState, _airState, new FuncPredicate(() => _dashState.DashEnded && IsOnFloor() == false));
    }

    public override void _Process(double delta)
    {
        var fDelta = (float)delta;
        _stateMachine.Update(fDelta);
    }

	public override void _PhysicsProcess(double delta)
    {
        var fDelta = (float)delta;
        _stateMachine.FixedUpdate(fDelta);
    }

    public bool TryMoveAndSlide(Vector2 velocity)
    {
        Velocity = velocity;
        return MoveAndSlide();
    }

    public Vector2 ProcessInputMovement(float delta, Vector2 currentVelocity, float acc, float decc)
    {
        var moveX = PlayerInput.MoveXAxis;

        if (moveX != 0)
        {
            var target = moveX * Speed;
            if (Mathf.Sign(moveX) != Mathf.Sign(currentVelocity.X))
            {
                acc *= OpositeInputMultiplier;
            }
            currentVelocity.X = Mathf.MoveToward(Velocity.X, target, acc * delta);
        }
        else
        {
            currentVelocity.X = Mathf.MoveToward(Velocity.X, 0, decc * delta);
        }

        return currentVelocity;
    }

    public Vector2 TryJump(Vector2 currentVelocity)
    {
        // jump
        if (PlayerInput.WasJumpPressed && IsOnFloor())
        {
            currentVelocity.Y = JumpVelocity;
        }

        return currentVelocity;
    }

    public Vector2 ProcessGravity(float delta, Vector2 currentVelocity)
    {
        // gravity
        if (!IsOnFloor())
        {
            var gravity = GetGravity();
            var multiplier = PlayerInput.JumpPressed ? FallLongMult : FallShortMult;
            currentVelocity += gravity * multiplier * delta;
        }

        return currentVelocity;
    }

    public bool IsWallSliding()
    {
        var xInput = PlayerInput.MoveXAxis;
        var wallNormal = GetWallNormal();
        var dot = wallNormal.Dot(Vector2.Right * xInput);

        return IsOnWall() && dot < 0 && Velocity.Y > 0;
    }
}
