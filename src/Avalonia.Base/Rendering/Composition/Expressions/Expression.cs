using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Expressions
{
    /// <summary>
    /// A parsed composition expression
    /// </summary>
    internal class CompositionExpression<T> where T : struct
    {
        private Func<ExpressionEvaluationContext<T>, T> _func;
        public HashSet<(ServerObject parameter, CompositionProperty property)> References { get; init; }
        public T Evaluate(ref ExpressionEvaluationContext<T> ctx) => _func(ctx);
        public override string? ToString() => _func.ToString();

        public CompositionExpression(Expression<Func<ExpressionEvaluationContext<T>, T>> expression)
        {
            _func = expression.Compile();// TODO: Expr visitor
            References = new();
        }
    }

}
