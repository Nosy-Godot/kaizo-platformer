using System;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class GameCameraTest
{
    private const string Scene = "res://Tests/Scenes/camera_test.tscn";
    private const int MaxPhysicsFrames = 180;
    private const float Eps = 0.01f;

    private ISceneRunner _runner;
    private Player _player;
    private GameCamera _camera;
    private CameraRoom _smallRoom;
    private CameraRoom _wideRoom;

    [BeforeTest]
    public void Setup()
    {
        _runner = ISceneRunner.Load(Scene);
        var root = _runner.Scene();
        _player = root.GetNode<Player>("Player");
        _camera = root.GetNode<GameCamera>("GameCamera");
        _smallRoom = root.GetNode<CameraRoom>("SmallRoom");
        _wideRoom = root.GetNode<CameraRoom>("WideRoom");
    }

    [TestCase]
    public async Task RoomSmallerThanView_KeepsCameraCentered()
    {
        await AwaitPhysicsFramesUntil(() => _player.IsOnFloor());

        _runner.SimulateActionPress("Move_Right");
        for (var i = 0; i < 30; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            AssertThat(_camera.CurrentRoom).IsSame(_smallRoom);
            AssertVector(_camera.GlobalPosition, _smallRoom.Bounds.GetCenter());
        }

        _runner.SimulateActionRelease("Move_Right");
    }

    [TestCase]
    public async Task CrossingIntoNextRoom_FreezesPlayerDuringTransition()
    {
        await AwaitPhysicsFramesUntil(() => _player.IsOnFloor());

        _runner.SimulateActionPress("Move_Right");
        await AwaitPhysicsFramesUntil(() => _camera.CurrentRoom == _wideRoom);

        AssertThat(_player.Frozen).IsTrue();
        var frozenAt = _player.GlobalPosition;

        var frames = await AwaitPhysicsFramesUntil(() => !_player.Frozen);
        _runner.SimulateActionRelease("Move_Right");

        AssertThat(frames).IsLessEqual(Mathf.CeilToInt(_camera.TransitionDuration * 60) + 1);
        AssertVector(_player.GlobalPosition, frozenAt);
        AssertThat(_camera.Transitioning).IsFalse();
        AssertVector(_camera.GlobalPosition, new Vector2(_wideRoom.Bounds.Position.X + _camera.HalfView.X, _wideRoom.Bounds.GetCenter().Y));
    }

    [TestCase]
    public async Task SnapToTarget_EntersRoomWithoutTransition()
    {
        await ISceneRunner.SyncPhysicsFrame;

        _player.GlobalPosition = new Vector2(880, 40);
        _camera.SnapToTarget();

        AssertThat(_camera.CurrentRoom).IsSame(_wideRoom);
        AssertThat(_player.Frozen).IsFalse();
        AssertVector(_camera.GlobalPosition, new Vector2(880, _wideRoom.Bounds.GetCenter().Y));
    }

    [TestCase]
    public async Task ScrollingRoom_ViewNeverLeavesBounds()
    {
        await EnterWideRoom();
        var startX = _camera.GlobalPosition.X;

        _runner.SimulateActionPress("Move_Right");
        for (var i = 0; i < 150; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            AssertViewInside(_wideRoom.Bounds);
        }

        _runner.SimulateActionRelease("Move_Right");
        AssertThat(_camera.GlobalPosition.X).IsGreater(startX + 100);
    }

    [TestCase]
    public async Task PlayerJumpingAhead_StaysInsideHardMargin()
    {
        await EnterWideRoom();

        _player.GlobalPosition += new Vector2(300, 0);
        await ISceneRunner.SyncPhysicsFrame;

        var reach = _camera.HalfView.X - _camera.MarginX;
        AssertThat(Mathf.Abs(_camera.Focus.X - _camera.GlobalPosition.X)).IsLessEqual(reach + Eps);
    }

    [TestCase]
    public async Task Trauma_ShakesThenSettles()
    {
        await ISceneRunner.SyncPhysicsFrame;

        _camera.AddTrauma(1f);
        await ISceneRunner.SyncPhysicsFrame;
        AssertThat(_camera.Offset.Length()).IsGreater(0f);

        await AwaitPhysicsFramesUntil(() => _camera.Offset == Vector2.Zero);
    }

    private async Task EnterWideRoom()
    {
        await ISceneRunner.SyncPhysicsFrame;
        _player.GlobalPosition = new Vector2(880, 80);
        _camera.SnapToTarget();
        await AwaitPhysicsFramesUntil(() => _player.IsOnFloor());
    }

    private void AssertViewInside(Rect2 bounds)
    {
        var half = _camera.HalfView;
        var pos = _camera.GlobalPosition;
        AssertThat(pos.X - half.X).IsGreaterEqual(bounds.Position.X - Eps);
        AssertThat(pos.X + half.X).IsLessEqual(bounds.End.X + Eps);
        AssertThat(pos.Y - half.Y).IsGreaterEqual(bounds.Position.Y - Eps);
        AssertThat(pos.Y + half.Y).IsLessEqual(bounds.End.Y + Eps);
    }

    private static void AssertVector(Vector2 actual, Vector2 expected)
    {
        AssertThat(actual.X).IsEqualApprox(expected.X, Eps);
        AssertThat(actual.Y).IsEqualApprox(expected.Y, Eps);
    }

    private static async Task<int> AwaitPhysicsFramesUntil(Func<bool> condition, int maxFrames = MaxPhysicsFrames)
    {
        for (var frame = 1; frame <= maxFrames; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            if (condition()) return frame;
        }

        throw new TimeoutException($"Condition not met after {maxFrames} physics frames.");
    }
}
