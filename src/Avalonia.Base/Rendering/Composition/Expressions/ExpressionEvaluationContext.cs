using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions
{
    public struct ExpressionEvaluationContext<T> where T : struct
    {
        public T StartingValue { get; set; }
        public T CurrentValue { get; set; }
        public T FinalValue { get; set; }
        internal IExpressionObject Target { get; set; }
    }

    internal interface IExpressionObject
    {
        T GetProperty<T>(string name) where T : struct;
    }

}
