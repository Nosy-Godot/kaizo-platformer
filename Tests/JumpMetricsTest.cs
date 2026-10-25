using System;
using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;

// Level design relies on these numbers (16 px tiles); retune the Jump exports if they fail.
[TestSuite]
[RequireGodotRuntime]
public class JumpMetricsTest
{
    private const string FlatScene = "res://Tests/Scenes/player_test.tscn";
    private const float Tile = 16f;
    private const int MaxPhysicsFrames = 180;

    private ISceneRunner _runner;
    private Player _player;

    [TestCase]
    public async Task TapJump_ClearsOneAndHalfTilesButNotTwo()
    {
        await LoadAndLand();

        var start = _player.GlobalPosition;
        await Tap("Jump");
        var (apex, _) = await TrackUntilLanded();

        var height = start.Y - apex;
        AssertThat(height).IsGreaterEqual(1.5f * Tile);
        AssertThat(height).IsLess(2 * Tile);
    }

    [TestCase]
    public async Task HeldJump_ClearsFourTilesButNotFive()
    {
        await LoadAndLand();

        var start = _player.GlobalPosition;
        _runner.SimulateActionPress("Jump");
        var (apex, _) = await TrackUntilLanded();
        _runner.SimulateActionRelease("Jump");

        var height = start.Y - apex;
        AssertThat(height).IsGreaterEqual(4 * Tile);
        AssertThat(height).IsLess(5 * Tile);
    }

    [TestCase]
    public async Task HeldRunningJump_CoversAtLeastFiveTiles()
    {
        await LoadAndLand();

        _runner.SimulateActionPress("Move_Right");
        await AwaitPhysicsFramesUntil(() => _player.Velocity.X >= _player.MaxRunSpeed);

        var start = _player.GlobalPosition;
        _runner.SimulateActionPress("Jump");
        var (_, landing) = await TrackUntilLanded();
        _runner.SimulateActionRelease("Jump");
        _runner.SimulateActionRelease("Move_Right");

        AssertThat(landing.X - start.X).IsGreaterEqual(5 * Tile);
    }

    [TestCase]
    public async Task TapJumpFromStandstill_CoversAtLeastTwoTiles()
    {
        await LoadAndLand();

        var start = _player.GlobalPosition;
        _runner.SimulateActionPress("Move_Right");
        await Tap("Jump");
        var (_, landing) = await TrackUntilLanded();
        _runner.SimulateActionRelease("Move_Right");

        AssertThat(landing.X - start.X).IsGreaterEqual(2 * Tile);
    }

    private async Task LoadAndLand()
    {
        _runner = ISceneRunner.Load(FlatScene);
        _player = _runner.Scene().GetNode<Player>("Player");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);
    }

    private async Task Tap(string action)
    {
        _runner.SimulateActionPress(action);
        await ISceneRunner.SyncPhysicsFrame;
        _runner.SimulateActionRelease(action);
    }

    // The jump tick may already have run (Tap), so the air state can be current on entry.
    private async Task<(float Apex, Godot.Vector2 Landing)> TrackUntilLanded()
    {
        var apex = _player.GlobalPosition.Y;
        if (_player.CurrentState is not PlayerAirState)
        {
            await AwaitPhysicsFramesUntil(() =>
            {
                apex = Math.Min(apex, _player.GlobalPosition.Y);
                return _player.CurrentState is PlayerAirState;
            });
        }

        await AwaitPhysicsFramesUntil(() =>
        {
            apex = Math.Min(apex, _player.GlobalPosition.Y);
            return _player.CurrentState is PlayerGroundedState;
        });
        return (apex, _player.GlobalPosition);
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
