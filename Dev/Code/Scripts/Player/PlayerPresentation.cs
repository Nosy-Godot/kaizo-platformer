using Godot;
using System;

public partial class PlayerPresentation : AnimationPlayer
{
	[Export] private Sprite2D _sprite;
	[Export] private AnimationTree _animTree;
	[Export] private Player _player;


	public override void _Ready()
	{
		
	}

	public override void _Process(double delta)
	{
		var pVel = _player.Velocity;
		if (pVel.X != 0)
		{
			_sprite.FlipH = pVel.X < 0;
		}

		var blendValue = Mathf.Abs(pVel.X) / _player.SprintSpeed;
		_animTree.Set("parameters/MoveBlend/blend_position", blendValue);
	}
}
