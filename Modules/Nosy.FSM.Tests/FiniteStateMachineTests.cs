using System;
using System.Collections.Generic;

namespace NosyCore.FSM.Tests
{
    public class FiniteStateMachineTests : FsmTestBase
    {
        [Fact]
        public void Constructor_DoesNotEnterInitialState()
        {
            var fsm = new FiniteStateMachine(A);

            Assert.Empty(Log);
            Assert.False(fsm.IsStarted);
            Assert.Same(A, fsm.CurrentState);
        }

        [Fact]
        public void Start_EntersInitialStateOnce()
        {
            var fsm = new FiniteStateMachine(A);

            fsm.Start();
            fsm.Start();

            Assert.Equal(new[] { "A.Enter(null)" }, Log);
            Assert.True(fsm.IsStarted);
        }

        [Fact]
        public void FirstTick_StartsAutomatically()
        {
            var fsm = new FiniteStateMachine(A);

            fsm.FixedUpdate(0.1f);

            Assert.Equal(new[] { "A.Enter(null)", "A.Fixed" }, Log);
        }

        [Fact]
        public void SetState_ExitsThenEnters()
        {
            var fsm = CreateStarted();
            fsm.AddState(B);

            fsm.SetState(B);

            Assert.Equal(new[] { "A.Exit(B)", "B.Enter(A)" }, Log);
            Assert.Same(B, fsm.CurrentState);
            Assert.Same(A, fsm.PreviousState);
        }

        [Fact]
        public void SetState_SameState_IsIgnoredUnlessForced()
        {
            var fsm = CreateStarted();

            fsm.SetState(A);
            Assert.Empty(Log);

            fsm.SetState(A, force: true);
            Assert.Equal(new[] { "A.Exit(A)", "A.Enter(A)" }, Log);
        }

        [Fact]
        public void SetState_UnregisteredState_Throws()
        {
            var fsm = CreateStarted();

            var ex = Assert.Throws<InvalidOperationException>(() => fsm.SetState(B));
            Assert.Contains("not registered", ex.Message);
        }

        [Fact]
        public void SetState_FromInsideOnEnter_Throws()
        {
            var fsm = CreateStarted();
            fsm.AddState(C);
            B.OnEnterAction = () => fsm.SetState(C);
            fsm.AddState(B);

            Assert.Throws<InvalidOperationException>(() => fsm.SetState(B));
        }

        [Fact]
        public void SetState_FromInsideOnExit_Throws()
        {
            var fsm = CreateStarted();
            fsm.AddState(B);
            fsm.AddState(C);
            A.OnExitAction = () => fsm.SetState(C);

            Assert.Throws<InvalidOperationException>(() => fsm.SetState(B));
        }

        [Fact]
        public void InstancesOfSameType_AreDistinctStates()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => true);
            fsm.AddTransition(B, C, () => true);

            fsm.FixedUpdate(0.1f);
            fsm.FixedUpdate(0.1f);

            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void TimeAndTicksInState_AccumulateAndResetOnChange()
        {
            var goToB = false;
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => goToB);

            fsm.FixedUpdate(0.25f);
            fsm.FixedUpdate(0.25f);
            Assert.Equal(2, fsm.TicksInState);
            Assert.Equal(0.5f, fsm.TimeInState, 5);

            goToB = true;
            fsm.FixedUpdate(0.25f);
            Assert.Same(B, fsm.CurrentState);
            Assert.Equal(1, fsm.TicksInState);
            Assert.Equal(0.25f, fsm.TimeInState, 5);
        }

        [Fact]
        public void TicksInState_IsVisibleInsideStateCallback()
        {
            var ticksSeen = new List<int>();
            FiniteStateMachine? fsm = null;
            var state = new LambdaState(() => ticksSeen.Add(fsm!.TicksInState));
            fsm = new FiniteStateMachine(state);

            fsm.FixedUpdate(0.1f);
            fsm.FixedUpdate(0.1f);
            fsm.FixedUpdate(0.1f);

            Assert.Equal(new[] { 1, 2, 3 }, ticksSeen);
        }

        [Fact]
        public void StateChanged_IsRaisedOnStartAndOnChange()
        {
            var changes = new List<(IState? From, IState To)>();
            var fsm = new FiniteStateMachine(A);
            fsm.AddState(B);
            fsm.StateChanged += (from, to) => changes.Add((from, to));

            fsm.Start();
            fsm.SetState(B);

            Assert.Equal(new (IState?, IState)[] { (null, A), (A, B) }, changes);
        }

        [Fact]
        public void StateChanged_HandlerMayChangeState()
        {
            var fsm = CreateStarted();
            fsm.AddState(B);
            fsm.AddState(C);
            fsm.StateChanged += (_, to) =>
            {
                if (to == B) fsm.SetState(C);
            };

            fsm.SetState(B);

            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new FiniteStateMachine(null!));

            var fsm = new FiniteStateMachine(A);
            Assert.Throws<ArgumentNullException>(() => fsm.AddTransition(A, B, (IPredicate)null!));
            Assert.Throws<ArgumentNullException>(() => fsm.AddTransition(A, null!, () => true));
            Assert.Throws<ArgumentNullException>(() => fsm.AddTriggerTransition(A, B, null!));
        }

        private class LambdaState : BaseState
        {
            private readonly Action _onFixed;

            public LambdaState(Action onFixed)
            {
                _onFixed = onFixed;
            }

            public override void OnFixedUpdate(float fixedDelta) => _onFixed();
        }
    }
}
