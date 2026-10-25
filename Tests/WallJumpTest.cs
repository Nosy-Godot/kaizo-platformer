using System;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class WallJumpTest
{
    private const string FlatScene = "res://Tests/Scenes/player_test.tscn";
    private const float Tile = 16f;
    private const int WallCellX = -4;
    private const int ClimbFrames = 240;
    private const int MaxPhysicsFrames = 180;

    private ISceneRunner _runner;
    private Player _player;

    [TestCase]
    public async Task SingleWall_CanBeClimbedWithWallJumps()
    {
        await LoadWithWall(slippery: false);

        var (height, wallSlid) = await ClimbWall();

        AssertThat(height).IsGreater(8 * Tile);
        AssertThat(wallSlid).IsTrue();
    }

    [TestCase]
    public async Task SlipperyWall_AllowsNoWallJumpOrSlide()
    {
        await LoadWithWall(slippery: true);

        var (height, wallSlid) = await ClimbWall();

        AssertThat(height).IsLess(5 * Tile);
        AssertThat(wallSlid).IsFalse();
        AssertThat(_player.WallAt(-1, 1)).IsFalse();
    }

    [TestCase]
    public async Task HoldingTowardWall_TrimsWallJumpPush()
    {
        var neutralPush = await WallJumpPush(holdTowardWall: false);
        var towardPush = await WallJumpPush(holdTowardWall: true);

        AssertThat(towardPush).IsLess(neutralPush);
    }

    private async Task<float> WallJumpPush(bool holdTowardWall)
    {
        await LoadWithWall(slippery: false);

        _runner.SimulateActionPress("Move_Left");
        await Tap("Jump");
        for (var i = 0; i < 12; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }

        await Tap("Jump");
        if (!holdTowardWall) _runner.SimulateActionRelease("Move_Left");
        for (var i = 0; i < _player.WallJumpForceTicks - 1; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }

        _runner.SimulateActionRelease("Move_Left");
        return _player.GlobalPosition.X - PlayerHalfWidth - WallFace;
    }

    private async Task Tap(string action)
    {
        _runner.SimulateActionPress(action);
        await ISceneRunner.SyncPhysicsFrame;
        _runner.SimulateActionRelease(action);
    }

    private async Task LoadWithWall(bool slippery)
    {
        _runner = ISceneRunner.Load(FlatScene);
        _player = _runner.Scene().GetNode<Player>("Player");
        _runner.Scene().AddChild(BuildWall(slippery));

        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerAirState);
        await AwaitPhysicsFramesUntil(() => _player.CurrentState is PlayerGroundedState);

        _player.GlobalPosition = new Vector2(WallFace + PlayerHalfWidth + 1, _player.GlobalPosition.Y);
        _player.ResetPhysicsInterpolation();
        await ISceneRunner.SyncPhysicsFrame;
    }

    // Holds toward the wall and re-presses jump each time the player is back within wall-jump range.
    private async Task<(float Height, bool WallSlid)> ClimbWall()
    {
        var floorY = _player.GlobalPosition.Y;
        var best = 0f;
        var wallSlid = false;
        var sinceJump = 0;

        _runner.SimulateActionPress("Move_Left");
        _runner.SimulateActionPress("Jump");
        for (var i = 0; i < ClimbFrames; i++)
        {
            await ISceneRunner.SyncPhysicsFrame;
            sinceJump++;
            best = Math.Max(best, floorY - _player.GlobalPosition.Y);
            wallSlid |= _player.CurrentState is PlayerWallSlideState;

            var nearWall = _player.GlobalPosition.X - PlayerHalfWidth - WallFace <= _player.WallJumpCheckDist;
            if (!_player.IsOnFloor() && nearWall && sinceJump > _player.WallJumpForceTicks)
            {
                _runner.SimulateActionRelease("Jump");
                await ISceneRunner.SyncPhysicsFrame;
                _runner.SimulateActionPress("Jump");
                sinceJump = 0;
            }
        }

        _runner.SimulateActionRelease("Jump");
        _runner.SimulateActionRelease("Move_Left");
        return (best, wallSlid);
    }

    private static float WallFace => (WallCellX + 1) * Tile;

    private float PlayerHalfWidth =>
        ((RectangleShape2D)_player.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Size.X / 2;

    // Built in code so the test exercises the real TileMapLayer custom-data lookup.
    private static TileMapLayer BuildWall(bool slippery)
    {
        var tileSet = new TileSet { TileSize = new Vector2I((int)Tile, (int)Tile) };
        tileSet.AddPhysicsLayer();
        tileSet.AddCustomDataLayer();
        tileSet.SetCustomDataLayerName(0, Player.SlipperyData);
        tileSet.SetCustomDataLayerType(0, Variant.Type.Bool);

        var image = Image.CreateEmpty((int)Tile, (int)Tile, false, Image.Format.Rgba8);
        var source = new TileSetAtlasSource
        {
            Texture = ImageTexture.CreateFromImage(image),
            TextureRegionSize = new Vector2I((int)Tile, (int)Tile),
        };
        tileSet.AddSource(source);
        source.CreateTile(Vector2I.Zero);

        var tile = source.GetTileData(Vector2I.Zero, 0);
        var half = Tile / 2;
        tile.AddCollisionPolygon(0);
        tile.SetCollisionPolygonPoints(0, 0, [new(-half, -half), new(half, -half), new(half, half), new(-half, half)]);
        tile.SetCustomData(Player.SlipperyData, slippery);

        var layer = new TileMapLayer { TileSet = tileSet };
        for (var y = -30; y <= 5; y++)
        {
            layer.SetCell(new Vector2I(WallCellX, y), 0, Vector2I.Zero);
        }

        return layer;
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
