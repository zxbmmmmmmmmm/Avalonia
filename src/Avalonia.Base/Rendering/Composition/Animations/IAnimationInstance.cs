using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    internal interface IAnimationInstance : IServerClockItem
    {
        void Activate();
        void Deactivate();
        void Invalidate();
    }

    internal interface IAnimationInstance<T> : IAnimationInstance
    {
        ServerObject TargetObject { get; }
        T Evaluate(TimeSpan now, T currentValue);
        void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty<T> property);
    }
}
