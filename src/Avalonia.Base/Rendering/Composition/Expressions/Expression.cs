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
            var visitor = new CompositionExpressionVisitor();
            var newExpression = (Expression<Func<ExpressionEvaluationContext<T>, T>>)visitor.Visit(expression)!;
            References = visitor.CollectedInfo;
            _func = newExpression.Compile();
        }
    }

    internal class CompositionExpressionVisitor : ExpressionVisitor
    {
        private static readonly PropertyInfo s_serverProperty =
            typeof(CompositionObject).GetProperty(nameof(CompositionObject.Server), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly PropertyInfo s_animationsProperty =
            typeof(ServerObject).GetProperty(nameof(ServerObject.Animations), BindingFlags.Public | BindingFlags.Instance)!;

        private static readonly MethodInfo s_ValidSubscriptionMethod =
            typeof(ServerObjectAnimations).GetMethod(nameof(ServerObjectAnimations.ValidSubscription), BindingFlags.Public | BindingFlags.Instance)!;

        internal HashSet<(ServerObject Instance, CompositionProperty Property)> CollectedInfo { get; } = new();

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not null &&
                typeof(CompositionObject).IsAssignableFrom(node.Expression.Type))
            {
                var instance = GetValue(node.Expression) as CompositionObject;
                if (instance is not null)
                {
                    var server = instance.Server as ServerObject;
                    var compProperty = server?.GetCompositionProperty(node.Member.Name);

                    if (compProperty is not null && compProperty.GetVariant is not null)
                    {
                        CollectedInfo.Add((server, compProperty));

                        var visitedExpression = Visit(node.Expression);
                        var serverAccess = Expression.MakeMemberAccess(visitedExpression, s_serverProperty);
                        var serverObjectAccess = Expression.Convert(serverAccess, typeof(ServerObject));
                        var animationsAccess = Expression.MakeMemberAccess(serverObjectAccess, s_animationsProperty);
                        var animationsVariable = Expression.Variable(typeof(ServerObjectAnimations), "animations");

                        var assignAnimations = Expression.Assign(animationsVariable, animationsAccess);

                        var validSubscriptionCall = Expression.IfThen(
                            Expression.ReferenceNotEqual(
                                animationsVariable,
                                Expression.Constant(null, typeof(ServerObjectAnimations))),
                            Expression.Call(
                                animationsVariable,
                                s_ValidSubscriptionMethod,
                                Expression.Constant(compProperty, typeof(CompositionProperty))));

                        var typedPropertyType = typeof(CompositionProperty<>).MakeGenericType(node.Type);
                        var typedGetVariantProperty = typedPropertyType.GetProperty(
                            nameof(CompositionProperty<int>.GetVariant),
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

                        var typedPropertyAccess = Expression.Constant(compProperty, typedPropertyType);
                        var getVariantAccess = Expression.MakeMemberAccess(typedPropertyAccess, typedGetVariantProperty);
                        var variantAccess = Expression.Invoke(
                            getVariantAccess,
                            Expression.Convert(serverAccess, typeof(SimpleServerObject)));

                        return Expression.Block(
                            [animationsVariable],
                            assignAnimations,
                            validSubscriptionCall,
                            variantAccess);
                    }
                }
            }

            return base.VisitMember(node);
        }

        private object? GetValue(Expression exp)
        {
            if (exp is ConstantExpression ce)
                return ce.Value;
            if (exp is MemberExpression me)
            {
                var target = GetValue(me.Expression!);
                if (me.Member is FieldInfo fi)
                    return fi.GetValue(target);
                if (me.Member is PropertyInfo pi)
                    return pi.GetValue(target);
            }
            return null;
        }
    }
}
