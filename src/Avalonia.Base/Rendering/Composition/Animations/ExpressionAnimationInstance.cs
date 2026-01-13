using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;


namespace Avalonia.Rendering.Composition.Animations
{
    
    /// <summary>
    /// Server-side counterpart of <see cref="ExpressionAnimation"/> with values baked-in.
    /// </summary>
    internal class ExpressionAnimationInstance<T> : AnimationInstanceBase<T> where T:struct
    {
        private readonly Expression _expression;
        private T _startingValue;
        private readonly T? _finalValue;

        protected override T EvaluateCore(TimeSpan now, T currentValue)
        {
            var ctx = new ExpressionEvaluationContext
            {
                Parameters = Parameters,
                Target = TargetObject,
                ForeignFunctionInterface = BuiltInExpressionFfi.Instance,
                StartingValue = ExpressionVariant.Create(_startingValue),
                FinalValue = ExpressionVariant.Create(_finalValue ?? _startingValue),
                CurrentValue = ExpressionVariant.Create(currentValue)
            };
            return _expression.Evaluate(ref ctx).CastOrDefault<T>();
        }

        public override void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty property)
        {
            _startingValue = startingValue;
            var hs = new HashSet<(string, string)>();
            _expression.CollectReferences(hs);
            base.Initialize(property, hs);
        }
        
        public ExpressionAnimationInstance(Expression expression,
            ServerObject target,
            T? finalValue,
            PropertySetSnapshot parameters) : base(target, parameters)
        {
            _expression = expression;
            _finalValue = finalValue;
        }
    }
}
