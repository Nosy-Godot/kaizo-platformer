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
		_sprite.FlipH = _player.Facing < 0;

		var moveBlendValue = Mathf.Abs(pVel.X) / _player.MaxRunSpeed;
		_animTree.Set("parameters/MoveBlend/blend_position", moveBlendValue);

		var jumpBlendValue = Mathf.Sign(pVel.Y);
		_animTree.Set("parameters/JumpBlend/blend_position", jumpBlendValue);
	}
}
