namespace NosyCore.FSM.Tests
{
    public class TransitionTests : FsmTestBase
    {
        [Fact]
        public void FixedUpdateMode_TransitionsBeforeNewStateFixedCallback()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => true);

            fsm.FixedUpdate(0.1f);

            Assert.Equal(new[] { "A.Exit(B)", "B.Enter(A)", "B.Fixed" }, Log);
        }

        [Fact]
        public void FixedUpdateMode_UpdateDoesNotTransition()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => true);

            fsm.Update(0.1f);

            Assert.Equal(new[] { "A.Update" }, Log);
            Assert.Same(A, fsm.CurrentState);
        }

        [Fact]
        public void UpdateMode_TransitionsOnUpdateOnly()
        {
            var fsm = CreateStarted(TransitionMode.Update);
            fsm.AddTransition(A, B, () => true);

            fsm.FixedUpdate(0.1f);
            Assert.Same(A, fsm.CurrentState);

            fsm.Update(0.1f);
            Assert.Equal(new[] { "A.Fixed", "A.Exit(B)", "B.Enter(A)", "B.Update" }, Log);
        }

        [Fact]
        public void FirstRegisteredTransition_HasPriority()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, C, () => true);
            fsm.AddTransition(A, B, () => true);

            fsm.FixedUpdate(0.1f);

            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void AnyTransition_HasPriorityOverStateTransition()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => true);
            fsm.AddAnyTransition(C, () => true);

            fsm.FixedUpdate(0.1f);

            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void AtMostOneTransitionPerTick()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => true);
            fsm.AddTransition(B, C, () => true);

            fsm.FixedUpdate(0.1f);
            Assert.Same(B, fsm.CurrentState);

            fsm.FixedUpdate(0.1f);
            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void FalseCondition_KeepsState()
        {
            var fsm = CreateStarted();
            fsm.AddTransition(A, B, () => false);

            fsm.FixedUpdate(0.1f);

            Assert.Same(A, fsm.CurrentState);
        }

        [Fact]
        public void AnyTransitionToCurrentState_IsSkippedWithoutReentry()
        {
            var fsm = CreateStarted();
            fsm.AddAnyTransition(A, () => true);
            fsm.AddTransition(A, B, () => true);

            fsm.FixedUpdate(0.1f);

            Assert.Same(B, fsm.CurrentState);
        }

        [Fact]
        public void TransitionToCurrentState_WithReentry_ExitsAndEnters()
        {
            var fsm = CreateStarted();
            fsm.AddAnyTransition(A, () => true, allowReentry: true);

            fsm.FixedUpdate(0.1f);

            Assert.Equal(new[] { "A.Exit(A)", "A.Enter(A)", "A.Fixed" }, Log);
        }

        [Fact]
        public void SkippedTransition_DoesNotEvaluateCondition()
        {
            var evaluated = false;
            var fsm = CreateStarted();
            fsm.AddAnyTransition(A, () => evaluated = true);

            fsm.FixedUpdate(0.1f);

            Assert.False(evaluated);
        }
    }
}
