using Godot;
using System;

public partial class PlayerPresentation : AnimationPlayer
{
	[Export] private AnimationTree _animTree;
	[Export] private Player _player;


	public override void _Ready()
	{
		
	}

	public override void _Process(double delta)
	{
		_animTree.Set("parameters/blend_position", Mathf.Abs(_player.Velocity.X));
	}
}
