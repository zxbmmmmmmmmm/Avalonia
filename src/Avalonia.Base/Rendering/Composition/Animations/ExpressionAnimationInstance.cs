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
            return _compositionExpresssion.Evaluate();
        }

        public override void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty<T> property)
        {
            _startingValue = startingValue;
            var hs = new HashSet<(string, string)>();
            base.Initialize(property, _trackedObjects);
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
