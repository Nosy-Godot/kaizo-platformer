namespace NosyCore.FSM
{
	public class Transition : ITransition
	{
		public IState ToState { get; }
		public IPredicate Condition { get; }

		public Transition(IState toState, IPredicate condition)
		{
			ToState = toState;
			Condition = condition;
		}
	}
}