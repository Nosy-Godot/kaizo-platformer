namespace NosyCore.FSM
{
    public abstract class BaseState : IState
    {
        public virtual void OnEnter(IState? previousState)
        {
            // noop
        }

        public virtual void OnUpdate(float delta)
        {
            // noop
        }

        public virtual void OnFixedUpdate(float fixedDelta)
        {
            // noop
        }

        public virtual void OnExit(IState nextState)
        {
            // noop
        }
    }

    public abstract class BaseState<TContext> : BaseState
    {
        protected readonly TContext _context;

        protected BaseState(TContext context)
        {
            _context = context;
        }
    }
}
