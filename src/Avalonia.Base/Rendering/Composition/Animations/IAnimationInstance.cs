using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    internal interface IAnimationInstance : IServerClockItem
    {
        ServerObject TargetObject { get; }
        void Activate();
        void Deactivate();
        void Invalidate();
    }

    internal interface IAnimationInstance<T> : IAnimationInstance
    {
        T Evaluate(TimeSpan now, T currentValue);
        void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty property);
    }
}
