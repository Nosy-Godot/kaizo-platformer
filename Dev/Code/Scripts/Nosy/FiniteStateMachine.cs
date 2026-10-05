using System;
using System.Collections.Generic;
using Godot;

namespace NosyCore.FSM
{
    public class FiniteStateMachine
    {
        public bool DebugStates;

        public IState CurrentState => _currentState.State;
        private StateNode _currentState;
        private Dictionary<Type, StateNode> _nodes = new ();
        private HashSet<ITransition> _anyTransitions = new ();

        public FiniteStateMachine(IState initialState)
        {
            SetState(GetOrAddNode(initialState).State);
        }

        public void Update(float delta)
        {
            var transition = GetTransition();
            if (transition != null)
            {
                if (DebugStates)
                {
                    GD.Print($"FSM ({_currentState.State.GetType().Name}) -> ({transition.ToState.GetType().Name})");
                }
                SetState(transition.ToState);
            }
            
            _currentState.State.OnUpdate(delta);
        }

        public void FixedUpdate(float fixedDelta)
        {
            _currentState.State.OnFixedUpdate(fixedDelta);
        }

        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).AddTransitions(GetOrAddNode(to).State, condition);
        }
        
        public void AddAnyTransition(IState to, IPredicate condition)
        {
            GetOrAddNode(to);
            _anyTransitions.Add(new Transition(to, condition));
        }

        public void SetState(IState state)
        {
            if (_currentState?.State == state) return;
            
            var nextState = _nodes[state.GetType()];
            var previousState = _currentState;
            if (previousState != null)
            {
                previousState.State.OnExit(nextState.State);
            }
           
            _currentState = nextState;
            _currentState.State.OnEnter(previousState?.State);
        }
        
        private ITransition GetTransition()
        {
            foreach (var anyTransition in _anyTransitions)
            {
                if (anyTransition.Condition.Evaluate() && anyTransition.ToState != _currentState.State)
                {
                    return anyTransition;
                }
            }
            
            foreach (var transition in _currentState.Transitions)
            {
                if (transition.Condition.Evaluate())
                {
                    return transition;
                }
            }

            return null;
        }
                
        private StateNode GetOrAddNode(IState state)
        {
            if (_nodes.TryGetValue(state.GetType(), out var node))
            {
                return node;
            }

            node = new StateNode(state);
            _nodes.Add(state.GetType(), node);
            return node;
        }

        class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }

            public StateNode(IState state)
            {
                State = state;
                Transitions = new HashSet<ITransition>();
            }

            public void AddTransitions(IState to, IPredicate condition)
            {
                Transitions.Add(new Transition(to, condition));
            }
        }
    }
}