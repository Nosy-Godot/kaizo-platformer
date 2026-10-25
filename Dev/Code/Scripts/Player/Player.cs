using Godot;
using NosyCore.FSM;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Run")]
    [Export] public float MaxRunSpeed = 180f;
    [Export] public float RunAccel = 2000f;
    [Export] public float RunReduce = 800f;
    [Export] public float AirAccelMult = 0.65f;

    [ExportGroup("Jump")]
    [Export] public float JumpSpeed = 210f;
    [Export] public float JumpHBoost = 80f;
    [Export] public int VarJumpTicks = 12;
    [Export] public float Gravity = 1500f;
    [Export] public float HalfGravThreshold = 100f;
    [Export] public float MaxFall = 280f;
    [Export] public float FastMaxFall = 440f;
    [Export] public float FastMaxAccel = 600f;

    [ExportGroup("Wall")]
    [Export] public float WallSlideMaxFall = 120f;
    [Export] public int WallJumpCheckDist = 5;
    [Export] public float WallJumpHSpeed = 260f;
    [Export] public int WallJumpForceTicks = 10;

    [ExportGroup("Dash")]
    [Export] public float DashSpeed = 480f;
    [Export] public float EndDashSpeed = 320f;
    [Export] public float EndDashUpMult = 0.75f;
    [Export] public int DashTicks = 9;
    [Export] public int DashFreezeTicks = 3;
    [Export] public int DashCooldownTicks = 12;
    [Export] public int DashRefillCooldownTicks = 6;
    [Export] public int MaxDashes = 1;
    [Export] public float SuperJumpHSpeed = 520f;
    [Export] public float HyperJumpHSpeed = 650f;

    [ExportGroup("Assists")]
    [Export] public int CoyoteTicks = 6;
    [Export] public int JumpBufferTicks = 6;
    [Export] public int DashBufferTicks = 5;
    [Export] public int UpwardCornerCorrection = 6;
    [Export] public int DashCornerCorrection = 6;

    [ExportGroup("Debug")]
    [Export] public bool DebugStates;

    private FiniteStateMachine _stateMachine;
    private PlayerGroundedState _groundedState;
    private PlayerAirState _airState;
    private PlayerWallSlideState _wallSlideState;
    private PlayerDashingState _dashState;

    private readonly KinematicCollision2D _probe = new();

    public Vector2 Vel;
    public int Facing { get; private set; } = 1;
    public int Dashes { get; private set; }

    private int _coyote;
    private int _jumpBuffer;
    private int _dashBuffer;
    private int _varJump;
    private float _varJumpSpeed;
    private int _forceMoveX;
    private int _forceMoveTicks;
    private int _dashCooldown;
    private int _dashRefillCooldown;
    private float _maxFall;

    public IState CurrentState => _stateMachine.CurrentState;
    public bool CanDash => _dashBuffer > 0 && Dashes > 0 && _dashCooldown == 0;
    public bool WantsWallSlide => !IsOnFloor() && Vel.Y > 0 && PlayerInput.MoveX != 0 && WallAt(PlayerInput.MoveX, 1);

    public override void _Ready()
    {
        Dashes = MaxDashes;
        _maxFall = MaxFall;

        _groundedState = new PlayerGroundedState(this);
        _airState = new PlayerAirState(this);
        _wallSlideState = new PlayerWallSlideState(this);
        _dashState = new PlayerDashingState(this);
        _stateMachine = new FiniteStateMachine(_groundedState);

        if (DebugStates)
        {
            _stateMachine.StateChanged += (from, to) => GD.Print($"FSM ({from?.GetType().Name}) -> ({to.GetType().Name})");
        }

        var onFloor = Predicates.From(IsOnFloor);
        var landed = onFloor.And(Predicates.From(() => Vel.Y >= 0));
        var canDash = Predicates.From(() => CanDash);
        var dashEnded = Predicates.From(() => _dashState.DashEnded);
        var wantsWallSlide = Predicates.From(() => WantsWallSlide);

        _stateMachine.AddTransition(_groundedState, _dashState, canDash);
        _stateMachine.AddTransition(_groundedState, _airState, onFloor.Not());

        _stateMachine.AddTransition(_airState, _dashState, canDash);
        _stateMachine.AddTransition(_airState, _groundedState, landed);
        _stateMachine.AddTransition(_airState, _wallSlideState, wantsWallSlide);

        _stateMachine.AddTransition(_wallSlideState, _dashState, canDash);
        _stateMachine.AddTransition(_wallSlideState, _groundedState, onFloor);
        _stateMachine.AddTransition(_wallSlideState, _airState, wantsWallSlide.Not());

        _stateMachine.AddTransition(_dashState, _groundedState, dashEnded.And(onFloor));
        _stateMachine.AddTransition(_dashState, _airState, dashEnded.And(onFloor.Not()));
    }

    public override void _Process(double delta)
    {
        _stateMachine.Update((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        TickTimers();
        _stateMachine.FixedUpdate((float)delta);
    }

    private void TickTimers()
    {
        Tick(ref _coyote);
        Tick(ref _jumpBuffer);
        Tick(ref _dashBuffer);
        Tick(ref _varJump);
        Tick(ref _forceMoveTicks);
        Tick(ref _dashCooldown);
        Tick(ref _dashRefillCooldown);

        if (PlayerInput.WasJumpPressed) _jumpBuffer = JumpBufferTicks;
        if (PlayerInput.DashPressed) _dashBuffer = DashBufferTicks;

        if (IsOnFloor())
        {
            _coyote = CoyoteTicks;
            if (_dashRefillCooldown == 0) Dashes = MaxDashes;
        }
    }

    private static void Tick(ref int timer)
    {
        if (timer > 0) timer--;
    }

    public bool DoMoveAndSlide()
    {
        var delta = (float)GetPhysicsProcessDeltaTime();
        if (Vel.Y < 0)
        {
            CornerCorrect(new Vector2(0, Vel.Y * delta), Vector2.Right, UpwardCornerCorrection, Mathf.Sign(Vel.X));
        }

        Velocity = Vel;
        var collided = MoveAndSlide();
        Vel = Velocity;

        // Var jump would keep pushing into the ceiling for its remaining ticks.
        if (IsOnCeiling()) _varJump = 0;

        return collided;
    }

    public void ProcessRun(float delta, float mult)
    {
        var moveX = _forceMoveTicks > 0 ? _forceMoveX : PlayerInput.MoveX;
        if (moveX != 0) Facing = moveX;

        var target = moveX * MaxRunSpeed;
        var overspeed = Mathf.Abs(Vel.X) > MaxRunSpeed && Mathf.Sign(Vel.X) == moveX;
        var accel = overspeed ? RunReduce : RunAccel;
        Vel.X = Mathf.MoveToward(Vel.X, target, accel * mult * delta);
    }

    public void ProcessGravity(float delta, float maxFall)
    {
        var mult = Mathf.Abs(Vel.Y) < HalfGravThreshold && PlayerInput.JumpPressed ? 0.5f : 1f;
        Vel.Y = Mathf.MoveToward(Vel.Y, maxFall, Gravity * mult * delta);
    }

    public float UpdateAirMaxFall(float delta)
    {
        var target = PlayerInput.MoveY > 0 && Vel.Y >= MaxFall ? FastMaxFall : MaxFall;
        _maxFall = Mathf.MoveToward(_maxFall, target, FastMaxAccel * delta);
        return _maxFall;
    }

    public void ProcessVarJump()
    {
        if (_varJump == 0) return;

        if (PlayerInput.JumpPressed)
            Vel.Y = Mathf.Min(Vel.Y, _varJumpSpeed);
        else
            _varJump = 0;
    }

    public bool TryJump()
    {
        if (_jumpBuffer == 0 || (!IsOnFloor() && _coyote == 0)) return false;

        Vel.X += JumpHBoost * PlayerInput.MoveX;
        StartJump(-JumpSpeed);
        return true;
    }

    public bool TryWallJump()
    {
        if (_jumpBuffer == 0) return false;

        var wallDir = WallDir(WallJumpCheckDist);
        if (wallDir == 0) return false;

        Vel.X = -wallDir * WallJumpHSpeed;
        _forceMoveX = -wallDir;
        _forceMoveTicks = WallJumpForceTicks;
        Facing = -wallDir;
        StartJump(-JumpSpeed);
        return true;
    }

    public bool TrySuperJump(Vector2 dashDir)
    {
        if (_jumpBuffer == 0 || (!IsOnFloor() && _coyote == 0)) return false;
        if (dashDir.X == 0 || dashDir.Y < 0) return false;

        var hyper = dashDir.Y > 0;
        Vel.X = Mathf.Sign(dashDir.X) * (hyper ? HyperJumpHSpeed : SuperJumpHSpeed);
        StartJump(hyper ? -JumpSpeed * 0.5f : -JumpSpeed);
        return true;
    }

    private void StartJump(float speedY)
    {
        Vel.Y = speedY;
        _varJumpSpeed = speedY;
        _varJump = VarJumpTicks;
        _jumpBuffer = 0;
        _coyote = 0;
    }

    public void BeginDash()
    {
        Dashes--;
        _dashBuffer = 0;
        _dashCooldown = DashCooldownTicks;
        _dashRefillCooldown = DashRefillCooldownTicks;
        _varJump = 0;
    }

    public void SetFacing(int dir)
    {
        if (dir != 0) Facing = dir;
    }

    public int WallDir(int dist)
    {
        if (WallAt(Facing, dist)) return Facing;
        if (WallAt(-Facing, dist)) return -Facing;
        return 0;
    }

    public bool WallAt(int dir, int dist) => Blocked(GlobalTransform, Vector2.Right * dir * dist);

    public bool CornerCorrect(Vector2 motion, Vector2 axis, int maxOffset, int sign)
    {
        var from = GlobalTransform;
        if (!Blocked(from, motion)) return false;

        for (var i = 1; i <= maxOffset; i++)
        {
            for (var s = -1; s <= 1; s += 2)
            {
                if (sign != 0 && s != sign) continue;

                var offset = axis * i * s;
                if (TestMove(from, offset)) continue;

                var shifted = from;
                shifted.Origin += offset;
                if (Blocked(shifted, motion)) continue;

                GlobalPosition += offset;
                return true;
            }
        }

        return false;
    }

    // Only counts hits that face the motion, so resting on a floor never reads as a wall.
    private bool Blocked(Transform2D from, Vector2 motion)
    {
        return TestMove(from, motion, _probe) && _probe.GetNormal().Dot(motion.Normalized()) < -0.7f;
    }
}
