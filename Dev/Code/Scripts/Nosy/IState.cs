
namespace NosyCore.FSM
{
    public interface IState
    {
        void OnEnter(IState previousState);
        void OnUpdate(float delta);
        void OnFixedUpdate(float fixedDelta);
        void OnExit(IState nextState);
    }

    public interface IStateContext {}
}
