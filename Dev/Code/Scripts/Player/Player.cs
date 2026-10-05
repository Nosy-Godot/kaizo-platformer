using Godot;
using NosyCore.FSM;
using System;

public partial class Player : CharacterBody2D, IStateContext
{
	public float Speed = 150.0f;
	public float JumpVelocity = -400.0f;

    [Export] public float Acceleration = 400;
    [Export] public float Decceleration = 250;
    [Export] public float AirAcceleration = 600;
    [Export] public float AirDecceleration = 500;
    [Export] public float OpositeInputMultiplier = 1.75f;

    private FiniteStateMachine _stateMachine;
    private PlayerGroundedState _groundedState;
    private PlayerAirState _airState;


    public override void _Ready()
    {
        _groundedState = new PlayerGroundedState(this);
        _airState = new PlayerAirState(this);
        _stateMachine = new FiniteStateMachine(_groundedState);

        _stateMachine.AddAnyTransition(_airState, new FuncPredicate(() => IsOnFloor() == false));
        _stateMachine.AddTransition(_airState, _groundedState, new FuncPredicate(() => IsOnFloor()));
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
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (direction != Vector2.Zero)
        {
            var target = direction.X * Speed;
            if (Mathf.Sign(direction.X) != Mathf.Sign(currentVelocity.X))
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
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
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
            currentVelocity += GetGravity() * delta;
        }

        return currentVelocity;
    }
}
