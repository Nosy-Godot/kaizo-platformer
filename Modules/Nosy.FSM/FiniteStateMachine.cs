using System;
using System.Collections.Generic;

namespace NosyCore.FSM
{
    public enum TransitionMode
    {
        FixedUpdate,
        Update,
    }

    public class FiniteStateMachine
    {
        public event Action<IState?, IState>? StateChanged;

        public TransitionMode Mode { get; }
        public bool IsStarted { get; private set; }
        public IState CurrentState => _currentNode.State;
        public IState? PreviousState { get; private set; }
        public float TimeInState { get; private set; }
        public int TicksInState { get; private set; }

        private StateNode _currentNode;
        private readonly Dictionary<IState, StateNode> _nodes = new(ReferenceEqualityComparer.Instance);
        private readonly List<IConditionalTransition> _anyTransitions = new();
        private readonly List<ITriggerTransition> _anyTriggerTransitions = new();
        private bool _isChangingState;

        public FiniteStateMachine(IState initialState, TransitionMode mode = TransitionMode.FixedUpdate)
        {
            ArgumentNullException.ThrowIfNull(initialState);
            Mode = mode;
            _currentNode = GetOrAddNode(initialState);
        }

        public void Start()
        {
            if (IsStarted) return;
            ThrowIfChangingState();

            IsStarted = true;
            _isChangingState = true;
            try
            {
                _currentNode.State.OnEnter(null);
            }
            finally
            {
                _isChangingState = false;
            }

            StateChanged?.Invoke(null, _currentNode.State);
        }

        public void Update(float delta)
        {
            Start();
            if (Mode == TransitionMode.Update)
            {
                TransitionTick(delta);
            }

            _currentNode.State.OnUpdate(delta);
        }

        public void FixedUpdate(float fixedDelta)
        {
            Start();
            if (Mode == TransitionMode.FixedUpdate)
            {
                TransitionTick(fixedDelta);
            }

            _currentNode.State.OnFixedUpdate(fixedDelta);
        }

        public void AddState(IState state)
        {
            GetOrAddNode(state);
        }

        public void AddTransition(IState from, IState to, IPredicate condition, bool allowReentry = false)
        {
            ArgumentNullException.ThrowIfNull(condition);
            GetOrAddNode(from).Transitions.Add(new Transition(GetOrAddNode(to).State, condition, allowReentry));
        }

        public void AddTransition(IState from, IState to, Func<bool> condition, bool allowReentry = false)
        {
            AddTransition(from, to, new FuncPredicate(condition), allowReentry);
        }

        public void AddAnyTransition(IState to, IPredicate condition, bool allowReentry = false)
        {
            ArgumentNullException.ThrowIfNull(condition);
            _anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition, allowReentry));
        }

        public void AddAnyTransition(IState to, Func<bool> condition, bool allowReentry = false)
        {
            AddAnyTransition(to, new FuncPredicate(condition), allowReentry);
        }

        public void AddTriggerTransition(IState from, IState to, object trigger, bool allowReentry = false)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            GetOrAddNode(from).TriggerTransitions.Add(new TriggerTransition(GetOrAddNode(to).State, trigger, allowReentry));
        }

        public void AddAnyTriggerTransition(IState to, object trigger, bool allowReentry = false)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            _anyTriggerTransitions.Add(new TriggerTransition(GetOrAddNode(to).State, trigger, allowReentry));
        }

        public bool Fire(object trigger)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            Start();
            ThrowIfChangingState();

            var transition = FindTrigger(_anyTriggerTransitions, trigger) ?? FindTrigger(_currentNode.TriggerTransitions, trigger);
            if (transition == null) return false;

            ChangeState(_nodes[transition.ToState]);
            return true;
        }

        public void SetState(IState state, bool force = false)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (!_nodes.TryGetValue(state, out var node))
            {
                throw new InvalidOperationException($"State '{state.GetType().Name}' is not registered in this state machine.");
            }

            Start();
            ThrowIfChangingState();
            if (node == _currentNode && !force) return;

            ChangeState(node);
        }

        private void TransitionTick(float delta)
        {
            var transition = FindConditional(_anyTransitions) ?? FindConditional(_currentNode.Transitions);
            if (transition != null)
            {
                ChangeState(_nodes[transition.ToState]);
            }

            TimeInState += delta;
            TicksInState++;
        }

        private void ChangeState(StateNode nextNode)
        {
            var previous = _currentNode.State;

            _isChangingState = true;
            try
            {
                previous.OnExit(nextNode.State);
                PreviousState = previous;
                _currentNode = nextNode;
                TimeInState = 0f;
                TicksInState = 0;
                nextNode.State.OnEnter(previous);
            }
            finally
            {
                _isChangingState = false;
            }

            StateChanged?.Invoke(previous, nextNode.State);
        }

        private IConditionalTransition? FindConditional(List<IConditionalTransition> transitions)
        {
            foreach (var transition in transitions)
            {
                if (CanTake(transition) && transition.Condition.Evaluate())
                {
                    return transition;
                }
            }

            return null;
        }

        private ITriggerTransition? FindTrigger(List<ITriggerTransition> transitions, object trigger)
        {
            foreach (var transition in transitions)
            {
                if (CanTake(transition) && transition.Trigger.Equals(trigger))
                {
                    return transition;
                }
            }

            return null;
        }

        private bool CanTake(ITransition transition)
        {
            return transition.AllowReentry || transition.ToState != _currentNode.State;
        }

        private void ThrowIfChangingState()
        {
            if (_isChangingState)
            {
                throw new InvalidOperationException("Cannot change state from inside OnEnter/OnExit.");
            }
        }

        private StateNode GetOrAddNode(IState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (_nodes.TryGetValue(state, out var node))
            {
                return node;
            }

            node = new StateNode(state);
            _nodes.Add(state, node);
            return node;
        }

        private class StateNode
        {
            public IState State { get; }
            public List<IConditionalTransition> Transitions { get; } = new();
            public List<ITriggerTransition> TriggerTransitions { get; } = new();

            public StateNode(IState state)
            {
                State = state;
            }
        }
    }
}
