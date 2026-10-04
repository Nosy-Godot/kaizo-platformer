using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public float SprintSpeed = Speed * _sprintBoost;
	public const float Speed = 100.0f;
	public const float JumpVelocity = -400.0f;

	private const float _sprintBoost = 1.5f;

    public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
    {
        var velocity = CalculateMovementVelocity(delta, Velocity);
        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector2 CalculateMovementVelocity(double delta, Vector2 velocity)
    {
		// gravity
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

		// jump
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

		// input and sprint
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        var isSprinting = Input.IsActionPressed("Sprint");

        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * (isSprinting ? SprintSpeed : Speed);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }

        return velocity;
    }

}
