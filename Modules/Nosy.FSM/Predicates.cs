using System;

namespace NosyCore.FSM
{
    public static class Predicates
    {
        public static IPredicate From(Func<bool> func) => new FuncPredicate(func);

        public static IPredicate And(this IPredicate left, IPredicate right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);
            return new FuncPredicate(() => left.Evaluate() && right.Evaluate());
        }

        public static IPredicate Or(this IPredicate left, IPredicate right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);
            return new FuncPredicate(() => left.Evaluate() || right.Evaluate());
        }

        public static IPredicate Not(this IPredicate predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return new FuncPredicate(() => !predicate.Evaluate());
        }
    }
}
