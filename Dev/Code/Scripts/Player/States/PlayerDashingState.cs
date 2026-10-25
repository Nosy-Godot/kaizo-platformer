using Godot;
using NosyCore.FSM;

public class PlayerDashingState : BaseState<Player>
{
    public bool DashEnded => _superJumped || _ticks >= _context.DashFreezeTicks + _context.DashTicks;

    private Vector2 _dir;
    private int _ticks;
    private bool _superJumped;

    public PlayerDashingState(Player context) : base(context)
    {
    }

    public override void OnEnter(IState previousState)
    {
        _ticks = 0;
        _superJumped = false;
        _dir = Vector2.Zero;

        _context.BeginDash();
        _context.Vel = Vector2.Zero;
        _context.Velocity = Vector2.Zero;
    }

    public override void OnFixedUpdate(float fixedDelta)
    {
        _ticks++;
        if (_ticks <= _context.DashFreezeTicks) return;

        // Aim is read after the freeze so a direction pressed slightly late still counts.
        if (_dir == Vector2.Zero)
        {
            _dir = PlayerInput.DashAim;
            if (_dir == Vector2.Zero) _dir = new Vector2(_context.Facing, 0);
            _context.SetFacing(Mathf.Sign(_dir.X));
        }

        if (_context.TrySuperJump(_dir))
        {
            _superJumped = true;
            _context.DoMoveAndSlide();
            return;
        }

        _context.Vel = _dir * _context.DashSpeed;
        if (_dir.X != 0)
        {
            var motion = new Vector2(_context.Vel.X * fixedDelta, 0);
            _context.CornerCorrect(motion, Vector2.Down, _context.DashCornerCorrection, Mathf.Sign(_dir.Y));
        }

        _context.DoMoveAndSlide();
    }

    public override void OnExit(IState nextState)
    {
        if (_superJumped || _dir.Y > 0) return;

        _context.Vel = _dir * _context.EndDashSpeed;
        if (_context.Vel.Y < 0) _context.Vel.Y *= _context.EndDashUpMult;
    }
}
