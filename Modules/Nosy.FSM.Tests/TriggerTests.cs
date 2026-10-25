namespace NosyCore.FSM.Tests
{
    public class TriggerTests : FsmTestBase
    {
        private enum Trigger
        {
            Hurt,
            Heal,
        }

        [Fact]
        public void Fire_TransitionsImmediately()
        {
            var fsm = CreateStarted();
            fsm.AddTriggerTransition(A, B, Trigger.Hurt);

            var taken = fsm.Fire(Trigger.Hurt);

            Assert.True(taken);
            Assert.Equal(new[] { "A.Exit(B)", "B.Enter(A)" }, Log);
        }

        [Fact]
        public void Fire_UnknownTrigger_ReturnsFalse()
        {
            var fsm = CreateStarted();
            fsm.AddTriggerTransition(A, B, Trigger.Hurt);

            Assert.False(fsm.Fire(Trigger.Heal));
            Assert.Same(A, fsm.CurrentState);
        }

        [Fact]
        public void Fire_TriggerOfAnotherState_IsNotTakenNorKept()
        {
            var fsm = CreateStarted();
            fsm.AddTriggerTransition(B, C, Trigger.Hurt);
            fsm.AddTransition(A, B, () => true);

            Assert.False(fsm.Fire(Trigger.Hurt));
            fsm.FixedUpdate(0.1f);

            Assert.Same(B, fsm.CurrentState);
        }

        [Fact]
        public void AnyTrigger_WorksFromEveryState()
        {
            var fsm = CreateStarted();
            fsm.AddAnyTriggerTransition(C, Trigger.Hurt);
            fsm.AddState(B);
            fsm.SetState(B);

            Assert.True(fsm.Fire(Trigger.Hurt));
            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void AnyTrigger_HasPriorityOverStateTrigger()
        {
            var fsm = CreateStarted();
            fsm.AddTriggerTransition(A, B, Trigger.Hurt);
            fsm.AddAnyTriggerTransition(C, Trigger.Hurt);

            fsm.Fire(Trigger.Hurt);

            Assert.Same(C, fsm.CurrentState);
        }

        [Fact]
        public void Fire_ToCurrentState_RespectsReentry()
        {
            var fsm = CreateStarted();
            fsm.AddAnyTriggerTransition(A, Trigger.Hurt);
            Assert.False(fsm.Fire(Trigger.Hurt));

            fsm.AddAnyTriggerTransition(A, Trigger.Heal, allowReentry: true);
            Assert.True(fsm.Fire(Trigger.Heal));
            Assert.Equal(new[] { "A.Exit(A)", "A.Enter(A)" }, Log);
        }

        [Fact]
        public void Fire_BeforeStart_StartsFirst()
        {
            var fsm = new FiniteStateMachine(A);
            fsm.AddTriggerTransition(A, B, "go");

            fsm.Fire("go");

            Assert.Equal(new[] { "A.Enter(null)", "A.Exit(B)", "B.Enter(A)" }, Log);
        }
    }
}
