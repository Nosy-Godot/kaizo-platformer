using Godot;

public partial class GameCamera : Camera2D
{
    [Export] public Player Target;
    [Export] public Vector2 FocusOffset = new(0, -11);

    [ExportGroup("Follow")]
    [Export] public float FollowSpeedX = 8f;
    [Export] public float FollowSpeedY = 6f;

    [ExportGroup("Look Ahead")]
    [Export] public float LookAheadX = 64f;
    [Export] public float LookAheadSpeed = 3f;
    [Export] public float LookDown = 48f;
    [Export] public float LookDownSpeed = 4f;

    [ExportGroup("Vertical")]
    [Export] public float AirDeadZoneUp = 64f;
    [Export] public float AirDeadZoneDown = 24f;

    [ExportGroup("Margins")]
    [Export] public float MarginX = 96f;
    [Export] public float MarginY = 64f;

    [ExportGroup("Transition")]
    [Export] public float TransitionDuration = 0.3f;

    [ExportGroup("Shake")]
    [Export] public bool ScreenShakeEnabled = true;
    [Export] public float ShakeMaxOffset = 6f;
    [Export] public float ShakeFrequency = 30f;
    [Export] public float ShakeDecay = 1.5f;

    private readonly CameraShake _shake = new();

    private bool _started;
    private float _lookAhead;
    private float _lookDown;
    private float _anchorY;
    private bool _transitioning;
    private float _transitionTime;
    private Vector2 _transitionFrom;
    private Vector2 _transitionTo;

    public CameraRoom CurrentRoom { get; private set; }
    public bool Transitioning => _transitioning;
    public Vector2 HalfView => GetViewportRect().Size / Zoom * 0.5f;
    public Vector2 Focus => Target.GlobalPosition + FocusOffset;

    public override void _Ready()
    {
        // Must tick after the player so it frames the position resolved in the same physics frame.
        ProcessPhysicsPriority = 100;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target == null) return;

        var dt = (float)delta;
        if (!_started) SnapToTarget();

        var room = FindRoom();
        if (room != CurrentRoom) BeginTransition(room);

        if (_transitioning)
            ProcessTransition(dt);
        else
            Follow(dt);

        var shake = _shake.Tick(dt, ShakeDecay, ShakeMaxOffset, ShakeFrequency);
        Offset = ScreenShakeEnabled ? shake : Vector2.Zero;
    }

    public void SnapToTarget()
    {
        _started = true;
        CurrentRoom = FindRoom();
        _anchorY = Focus.Y;
        _lookAhead = 0f;
        _lookDown = 0f;
        EndTransition();

        GlobalPosition = Constrain(DesiredPosition());
        ResetPhysicsInterpolation();
    }

    public void AddTrauma(float amount) => _shake.Add(amount);

    private void Follow(float dt)
    {
        UpdateAnchorY();
        UpdateOffsets(dt);

        var desired = DesiredPosition();
        var pos = GlobalPosition;
        pos.X = Damp(pos.X, desired.X, FollowSpeedX, dt);
        pos.Y = Damp(pos.Y, desired.Y, FollowSpeedY, dt);
        GlobalPosition = Constrain(pos);
    }

    // The anchor only follows the player's height when standing on something, so jumps don't bob the camera.
    private void UpdateAnchorY()
    {
        var y = Focus.Y;
        if (Target.IsOnFloor() || Target.CurrentState is PlayerWallSlideState)
            _anchorY = y;
        else
            _anchorY = Mathf.Clamp(_anchorY, y - AirDeadZoneDown, y + AirDeadZoneUp);
    }

    private void UpdateOffsets(float dt)
    {
        var mult = CameraZone.Find(Focus)?.LookAheadMult ?? 1f;
        var speedRatio = Mathf.Min(Mathf.Abs(Target.Vel.X) / Target.MaxRunSpeed, 1f);
        _lookAhead = Damp(_lookAhead, Target.Facing * speedRatio * LookAheadX * mult, LookAheadSpeed, dt);

        var fastFalling = Target.Vel.Y > Target.MaxFall;
        _lookDown = Damp(_lookDown, fastFalling ? LookDown : 0f, LookDownSpeed, dt);
    }

    private Vector2 DesiredPosition()
    {
        var target = new Vector2(Focus.X + _lookAhead, _anchorY + _lookDown);
        var zone = CameraZone.Find(Focus);
        if (zone != null) target = zone.Apply(target);
        return ClampToRoom(target);
    }

    private Vector2 Constrain(Vector2 pos) => ClampToRoom(ApplyMargins(pos));

    private Vector2 ApplyMargins(Vector2 pos)
    {
        var focus = Focus;
        var reach = (HalfView - new Vector2(MarginX, MarginY)).Max(Vector2.Zero);
        pos.X = Mathf.Clamp(pos.X, focus.X - reach.X, focus.X + reach.X);
        pos.Y = Mathf.Clamp(pos.Y, focus.Y - reach.Y, focus.Y + reach.Y);
        return pos;
    }

    private Vector2 ClampToRoom(Vector2 pos)
    {
        if (CurrentRoom == null) return pos;

        var bounds = CurrentRoom.Bounds;
        var half = HalfView;
        pos.X = ClampAxis(pos.X, bounds.Position.X, bounds.End.X, half.X);
        pos.Y = ClampAxis(pos.Y, bounds.Position.Y, bounds.End.Y, half.Y);
        return pos;
    }

    private static float ClampAxis(float value, float min, float max, float half)
    {
        if (max - min <= half * 2) return (min + max) / 2;
        return Mathf.Clamp(value, min + half, max - half);
    }

    // Keeps the current room while the player is still inside it, so overlapping rooms don't flicker.
    private CameraRoom FindRoom()
    {
        var focus = Focus;
        if (CurrentRoom != null && CurrentRoom.Bounds.HasPoint(focus)) return CurrentRoom;

        foreach (var room in CameraRoom.All)
        {
            if (room.Bounds.HasPoint(focus)) return room;
        }

        return CurrentRoom;
    }

    private void BeginTransition(CameraRoom room)
    {
        CurrentRoom = room;
        _anchorY = Focus.Y;
        _transitionFrom = GlobalPosition;
        _transitionTo = Constrain(DesiredPosition());
        _transitionTime = 0f;
        _transitioning = true;
        Target.Frozen = true;
    }

    private void ProcessTransition(float dt)
    {
        _transitionTime += dt;
        var t = TransitionDuration > 0f ? Mathf.Min(_transitionTime / TransitionDuration, 1f) : 1f;
        var eased = 1f - Mathf.Pow(1f - t, 3f);
        GlobalPosition = _transitionFrom.Lerp(_transitionTo, eased);

        if (t >= 1f) EndTransition();
    }

    private void EndTransition()
    {
        if (!_transitioning) return;

        _transitioning = false;
        Target.Frozen = false;
    }

    private static float Damp(float from, float to, float speed, float dt) =>
        to + (from - to) * Mathf.Exp(-speed * dt);
}
