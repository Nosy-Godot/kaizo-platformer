using Godot;
using NosyCore.FSM;
using System;

public partial class Player : CharacterBody2D
{
	public float Speed = 150.0f;
	public float JumpVelocity = -350.0f;

    [Export] public float Acceleration = 600;
    [Export] public float Decceleration = 500;
    [Export] public float AirAcceleration = 800;
    [Export] public float AirDecceleration = 500;
    [Export] public float OpositeInputMultiplier = 5;

    public float FallLongMult = 0.65f;
    public float FallShortMult = 1.65f;

    [Export] public float MaxWallSlideSpeed = 80f;
    [Export] public float WallJumpHorizontalVelocity = 180f;
    [Export] public float WallJumpVerticalVelocity = -300f;

    [Export] public int DashDurationTicks = 6;
    [Export] public bool DebugStates;

    private FiniteStateMachine _stateMachine;
    private PlayerGroundedState _groundedState;
    private PlayerAirState _airState;
    private PlayerDashingState _dashState;

    public Vector2 KinematicVel;
    public Vector2 DynamicVel;

    public IState CurrentState => _stateMachine.CurrentState;


    public override void _Ready()
    {
        _groundedState = new PlayerGroundedState(this);
        _airState = new PlayerAirState(this);
        _dashState = new PlayerDashingState(this);
        _stateMachine = new FiniteStateMachine(_groundedState);

        if (DebugStates)
        {
            _stateMachine.StateChanged += (from, to) => GD.Print($"FSM ({from?.GetType().Name}) -> ({to.GetType().Name})");
        }

        var onFloor = Predicates.From(IsOnFloor);
        var dashPressed = Predicates.From(() => PlayerInput.DashPressed);
        var dashEnded = Predicates.From(() => _dashState.DashEnded);

        _stateMachine.AddTransition(_airState, _groundedState, onFloor);
        _stateMachine.AddTransition(_groundedState, _airState, onFloor.Not());

        _stateMachine.AddTransition(_groundedState, _dashState, dashPressed);
        _stateMachine.AddTransition(_airState, _dashState, dashPressed);
        _stateMachine.AddTransition(_dashState, _groundedState, dashEnded.And(onFloor));
        _stateMachine.AddTransition(_dashState, _airState, dashEnded.And(onFloor.Not()));
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

    public bool DoMoveAndSlide()
    {
        Velocity = KinematicVel + DynamicVel;

        ApplyHorizontalDynamicDrag();

        return MoveAndSlide();
    }

    public void ProcessInputMovement(float delta, float acc, float decc)
    {
        var moveX = PlayerInput.MoveXAxis;

        if (moveX != 0)
        {
            var target = moveX * Speed;
            if (Mathf.Sign(moveX) != Mathf.Sign(KinematicVel.X))
            {
                acc *= OpositeInputMultiplier;
            }
            KinematicVel.X = Mathf.MoveToward(KinematicVel.X, target, acc * delta);
        }
        else
        {
            KinematicVel.X = Mathf.MoveToward(KinematicVel.X, 0, decc * delta);
        }
    }

    public void ApplyHorizontalDynamicDrag()
    {
        
        var delta = (float)GetPhysicsProcessDeltaTime();
        var xValue = Mathf.MoveToward(DynamicVel.X, 0f, AirDecceleration * delta);
        DynamicVel.X = xValue;
    }

    public bool TryJump()
    {
        // jump
        if (PlayerInput.WasJumpPressed && IsOnFloor())
        {
            DynamicVel.Y = JumpVelocity;
            return true;
        }

        return false;
    }

    public void ProcessGravity(float delta)
    {
        // gravity
        if (IsOnFloor() == false)
        {
            var gravity = GetGravity();
            var multiplier = PlayerInput.JumpPressed ? FallLongMult : FallShortMult;
            DynamicVel += gravity * multiplier * delta;
        }
    }

    public bool IsWallSliding()
    {
        var xInput = PlayerInput.MoveXAxis;
        var wallNormal = GetWallNormal();
        var dot = wallNormal.Dot(Vector2.Right * xInput);

        return IsOnWall() && dot < 0 && Velocity.Y > 0;
    }
}
