namespace NosyCore.FSM
{
    public interface ITransition
    {
        IState ToState { get; }
        bool AllowReentry { get; }
    }

    public interface IConditionalTransition : ITransition
    {
        IPredicate Condition { get; }
    }

    public interface ITriggerTransition : ITransition
    {
        object Trigger { get; }
    }
}
