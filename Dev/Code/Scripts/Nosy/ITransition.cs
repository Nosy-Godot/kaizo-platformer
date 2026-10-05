namespace NosyCore.FSM
{
    public interface ITransition
    {
        IState ToState { get; }
        IPredicate Condition { get; }
    }
}