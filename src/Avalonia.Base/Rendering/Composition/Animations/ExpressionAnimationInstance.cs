using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;


namespace Avalonia.Rendering.Composition.Animations
{

    /// <summary>
    /// Server-side counterpart of <see cref="ExpressionAnimation"/> with values baked-in.
    /// </summary>
    internal class ExpressionAnimationInstance<T> : AnimationInstanceBase<T> where T : struct
    {
        private readonly CompositionExpression<T> _compositionExpresssion;
        private T _startingValue;
        private readonly T? _finalValue;
        private HashSet<(ServerObject, CompositionProperty)> _trackedObjects;

        protected override T EvaluateCore(TimeSpan now, T currentValue)
        {
            var ctx = new ExpressionEvaluationContext<T>
            {
                StartingValue = _startingValue,
                FinalValue = _finalValue ?? _startingValue,
                CurrentValue = currentValue,
                Target = TargetObject
            };
            return _compositionExpresssion.Evaluate(ref ctx);
        }

        public override void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty<T> property)
        {
            _startingValue = startingValue;
            var references = _compositionExpresssion.References;
            base.Initialize(property, references);
        }

        public ExpressionAnimationInstance(CompositionExpression<T> expression,
            ServerObject target,
            T? finalValue,
            HashSet<(ServerObject, CompositionProperty)>? trackedObjects) : base(target)
        {
            _compositionExpresssion = expression;
            _finalValue = finalValue;
            _trackedObjects = trackedObjects ?? new();
        }
    }
}
