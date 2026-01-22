using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions
{
    public struct ExpressionEvaluationContext<T> where T : struct
    {
        public T StartingValue { get; set; }
        public T CurrentValue { get; set; }
        public T FinalValue { get; set; }
        internal IExpressionObject Target { get; set; }
        internal IExpressionParameterCollection Parameters { get; set; }
    }

    internal interface IExpressionObject
    {
        ExpressionVariant GetProperty(string name);
    }

    internal interface IExpressionParameterCollection
    {
        public ExpressionVariant GetParameter(string name);

        public IExpressionObject GetObjectParameter(string name);
    }

    internal interface IExpressionForeignFunctionInterface
    {
        bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result);
    }
}
