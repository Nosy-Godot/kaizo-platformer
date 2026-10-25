using System;
using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class PlayerStateMachineTest
{
    private const string TestScene = "res://Tests/Scenes/player_test.tscn";
    private const int MaxPhysicsFrames = 120;

    private ISceneRunner _runner;
    private Player _player;

    [BeforeTest]
    public void Setup()
    {
        _runner = ISceneRunner.Load(TestScene);
        _player = _runner.Scene().GetNode<Player>("Player");
    }

    [TestCase]
    public async Task FallingPlayer_LandsInGroundedState()
    {
        // The FSM starts grounded; the first physics tick finds no floor and switches to air.
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);

        AssertThat(_player.IsOnFloor()).IsTrue();
        AssertThat(_player.CurrentState).IsInstanceOf<PlayerGroundedState>();
    }

    [TestCase]
    public async Task DashPressed_EntersDashForDurationTicks()
    {
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);

        _runner.SimulateActionPress("Move_Right");
        _runner.SimulateActionPress("Dash");
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerDashingState);
        _runner.SimulateActionRelease("Dash");
        _runner.SimulateActionRelease("Move_Right");

        AssertThat(_player.Velocity.X).IsGreater(0);

        var dashFrames = await AwaitPhysicsFramesUntil(() => _player.CurrentState is not PlayerDashingState);
        AssertThat(_player.CurrentState).IsInstanceOf<PlayerGroundedState>();
        AssertThat(dashFrames).IsEqual(_player.DashDurationTicks);
    }

    private async Task<int> AwaitPhysicsFramesUntil(Func<bool> condition)
    {
        for (var frame = 1; frame <= MaxPhysicsFrames; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            if (condition()) return frame;
        }

        throw new TimeoutException($"Condition not met after {MaxPhysicsFrames} physics frames.");
    }
}
