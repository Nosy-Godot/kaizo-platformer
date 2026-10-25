using System;
using System.Collections.Generic;

namespace NosyCore.FSM.Tests
{
    public class RecordingState : IState
    {
        public string Name { get; }
        public Action? OnEnterAction { get; set; }
        public Action? OnExitAction { get; set; }

        private readonly List<string> _log;

        public RecordingState(string name, List<string> log)
        {
            Name = name;
            _log = log;
        }

        public void OnEnter(IState? previousState)
        {
            _log.Add($"{Name}.Enter({NameOf(previousState)})");
            OnEnterAction?.Invoke();
        }

        public void OnUpdate(float delta) => _log.Add($"{Name}.Update");

        public void OnFixedUpdate(float fixedDelta) => _log.Add($"{Name}.Fixed");

        public void OnExit(IState nextState)
        {
            _log.Add($"{Name}.Exit({NameOf(nextState)})");
            OnExitAction?.Invoke();
        }

        public override string ToString() => Name;

        private static string NameOf(IState? state) => state is RecordingState r ? r.Name : state?.ToString() ?? "null";
    }

    public abstract class FsmTestBase
    {
        protected readonly List<string> Log = new();
        protected readonly RecordingState A;
        protected readonly RecordingState B;
        protected readonly RecordingState C;

        protected FsmTestBase()
        {
            A = new RecordingState("A", Log);
            B = new RecordingState("B", Log);
            C = new RecordingState("C", Log);
        }

        protected FiniteStateMachine CreateStarted(TransitionMode mode = TransitionMode.FixedUpdate)
        {
            var fsm = new FiniteStateMachine(A, mode);
            fsm.Start();
            Log.Clear();
            return fsm;
        }
    }
}
