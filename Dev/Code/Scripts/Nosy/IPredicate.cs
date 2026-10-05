using System;

namespace NosyCore.FSM
{
    public interface IPredicate
    {
        bool Evaluate();
    }
    
    public class FuncPredicate : IPredicate{
        private readonly Func<bool> _func;
        
        public FuncPredicate(Func<bool> func)
        {
            _func = func ?? throw new NotImplementedException();
        }
        
        public bool Evaluate()
        {
            return _func.Invoke();
        }
    }
}