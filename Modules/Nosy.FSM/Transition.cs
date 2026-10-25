namespace NosyCore.FSM
{
    public class Transition : IConditionalTransition
    {
        public IState ToState { get; }
        public IPredicate Condition { get; }
        public bool AllowReentry { get; }

        public Transition(IState toState, IPredicate condition, bool allowReentry = false)
        {
            ToState = toState;
            Condition = condition;
            AllowReentry = allowReentry;
        }
    }

    public class TriggerTransition : ITriggerTransition
    {
        public IState ToState { get; }
        public object Trigger { get; }
        public bool AllowReentry { get; }

        public TriggerTransition(IState toState, object trigger, bool allowReentry = false)
        {
            ToState = toState;
            Trigger = trigger;
            AllowReentry = allowReentry;
        }
    }
}
