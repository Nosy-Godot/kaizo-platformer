using System;
using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class PlayerStateMachineTest
{
    private const string FlatScene = "res://Tests/Scenes/player_test.tscn";
    private const string LedgeScene = "res://Tests/Scenes/player_ledge_test.tscn";
    private const int MaxPhysicsFrames = 180;

    private ISceneRunner _runner;
    private Player _player;

    [TestCase]
    public async Task FallingPlayer_LandsInGroundedState()
    {
        Load(FlatScene);

        // The FSM starts grounded; the first physics tick finds no floor and switches to air.
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);

        AssertThat(_player.IsOnFloor()).IsTrue();
    }

    [TestCase]
    public async Task DashPressed_FreezesThenDashesForDurationTicks()
    {
        Load(FlatScene);
        await Land();

        _runner.SimulateActionPress("Move_Right");
        _runner.SimulateActionPress("Dash");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerDashingState);
        _runner.SimulateActionRelease("Dash");

        AssertThat(_player.Velocity.X).IsEqual(0f);

        var dashFrames = await AwaitPhysicsFramesUntil(() => _player.CurrentState is not PlayerDashingState);
        _runner.SimulateActionRelease("Move_Right");

        AssertThat(_player.CurrentState).IsInstanceOf<PlayerGroundedState>();
        AssertThat(dashFrames).IsEqual(_player.DashFreezeTicks + _player.DashTicks);
        AssertThat(_player.Velocity.X).IsGreater(_player.MaxRunSpeed);
    }

    [TestCase]
    public async Task JumpPressedBeforeLanding_IsBuffered()
    {
        Load(FlatScene);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState && _player.Velocity.Y > 0);

        // Floor top is at y = 90; press when the player is a few ticks from touching it.
        await AwaitPhysicsFramesUntil(() => _player.GlobalPosition.Y > 90 - _player.Velocity.Y * 3f / 60f);
        await Tap("Jump");

        await AwaitPhysicsFramesUntil(() => _player.Velocity.Y < 0, _player.JumpBufferTicks + 2);
    }

    [TestCase]
    public async Task HoldingJump_ReachesHigherApexThanTapping()
    {
        Load(FlatScene);
        await Land();

        var startY = _player.GlobalPosition.Y;
        await Tap("Jump");
        var tapApex = await TrackApexUntilLanded();

        _runner.SimulateActionPress("Jump");
        var holdApex = await TrackApexUntilLanded();
        _runner.SimulateActionRelease("Jump");

        AssertThat(startY - holdApex).IsGreater(startY - tapApex);
    }

    [TestCase]
    public async Task AirDash_OnlyOncePerLanding()
    {
        Load(FlatScene);
        await Land();

        await Tap("Jump");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await Tap("Dash");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);

        AssertThat(_player.Dashes).IsEqual(0);
        await Tap("Dash");
        for (var i = 0; i < 3; i++)
        {
            AssertThat(_player.CurrentState).IsNotInstanceOf<PlayerDashingState>();
            await ISceneRunner.SyncPhysicsFrame;
        }

        await Land();
        await ISceneRunner.SyncPhysicsFrame;
        AssertThat(_player.Dashes).IsEqual(_player.MaxDashes);
    }

    [TestCase]
    public async Task Falling_IsCappedAtMaxFall()
    {
        Load(FlatScene);
        await Land();

        _player.GlobalPosition = new Godot.Vector2(0, -3000);
        for (var i = 0; i < 60; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }

        AssertThat(_player.Velocity.Y).IsLessEqual(_player.MaxFall + 0.01f);
        AssertThat(_player.Velocity.Y).IsGreater(_player.MaxFall - 1f);
    }

    [TestCase]
    public async Task WalkingOffLedge_CanStillJumpWithinCoyoteTicks()
    {
        Load(LedgeScene);
        await Land();

        _runner.SimulateActionPress("Move_Right");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        _runner.SimulateActionRelease("Move_Right");

        await ISceneRunner.SyncPhysicsFrame;
        await Tap("Jump");

        await AwaitPhysicsFramesUntil(() => _player.Velocity.Y < 0, 2);
    }

    [TestCase]
    public async Task WallJump_PushesAwayEvenWhenHoldingTowardWall()
    {
        Load(LedgeScene);
        await Land();

        _runner.SimulateActionPress("Move_Left");
        await AwaitPhysicsFramesUntil(() => _player.WallAt(-1, 1));

        await Tap("Jump");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await ISceneRunner.SyncPhysicsFrame;
        await Tap("Jump");

        for (var i = 0; i < _player.WallJumpForceTicks - 2; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            AssertThat(_player.Velocity.X).IsGreater(0);
        }

        _runner.SimulateActionRelease("Move_Left");
    }

    private void Load(string scene)
    {
        _runner = ISceneRunner.Load(scene);
        _player = _runner.Scene().GetNode<Player>("Player");
    }

    private async Task Land()
    {
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);
    }

    private async Task Tap(string action)
    {
        _runner.SimulateActionPress(action);
        await ISceneRunner.SyncPhysicsFrame;
        _runner.SimulateActionRelease(action);
    }

    private async Task<float> TrackApexUntilLanded()
    {
        var apex = _player.GlobalPosition.Y;
        await AwaitPhysicsFramesUntil(() =>
        {
            apex = Math.Min(apex, _player.GlobalPosition.Y);
            return _player.CurrentState is PlayerAirState;
        });
        await AwaitPhysicsFramesUntil(() =>
        {
            apex = Math.Min(apex, _player.GlobalPosition.Y);
            return _player.CurrentState is PlayerGroundedState;
        });
        return apex;
    }

    private async Task<int> AwaitPhysicsFramesUntil(Func<bool> condition, int maxFrames = MaxPhysicsFrames)
    {
        for (var frame = 1; frame <= maxFrames; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            if (condition()) return frame;
        }

        throw new TimeoutException($"Condition not met after {maxFrames} physics frames.");
    }
}
