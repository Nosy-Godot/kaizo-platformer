using System;

namespace NosyCore.FSM.Tests
{
    public class PredicateTests
    {
        private static readonly IPredicate True = Predicates.From(() => true);
        private static readonly IPredicate False = Predicates.From(() => false);

        [Fact]
        public void FuncPredicate_EvaluatesDelegate()
        {
            var value = false;
            var predicate = new FuncPredicate(() => value);

            Assert.False(predicate.Evaluate());
            value = true;
            Assert.True(predicate.Evaluate());
        }

        [Fact]
        public void FuncPredicate_NullDelegate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FuncPredicate(null!));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void And(bool left, bool right, bool expected)
        {
            Assert.Equal(expected, Of(left).And(Of(right)).Evaluate());
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void Or(bool left, bool right, bool expected)
        {
            Assert.Equal(expected, Of(left).Or(Of(right)).Evaluate());
        }

        [Fact]
        public void Not()
        {
            Assert.False(True.Not().Evaluate());
            Assert.True(False.Not().Evaluate());
        }

        [Fact]
        public void And_ShortCircuits()
        {
            var rightEvaluated = false;

            False.And(Predicates.From(() => rightEvaluated = true)).Evaluate();

            Assert.False(rightEvaluated);
        }

        private static IPredicate Of(bool value) => value ? True : False;
    }
}
