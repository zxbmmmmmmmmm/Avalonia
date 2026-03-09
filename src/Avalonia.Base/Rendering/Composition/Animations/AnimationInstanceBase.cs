using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// The base class for both key-frame and expression animation instances
/// Is responsible for activation tracking and for subscribing to properties used in dependencies
/// </summary>
internal abstract class AnimationInstanceBase : IAnimationInstance
{
    private List<(ServerObject obj, CompositionProperty member)>? _trackedObjects;
    public ServerObject TargetObject { get; }
    protected CompositionProperty Property { get; private set; } = null!;
    protected bool _invalidated;

    public AnimationInstanceBase(ServerObject target)
    {
        TargetObject = target;
    }

    protected void Initialize(CompositionProperty property, HashSet<(ServerObject name, CompositionProperty member)> trackedObjects)
    {
        if (trackedObjects.Count > 0)
        {
            _trackedObjects = [.. trackedObjects];
        }

        Property = property;
    }



    public virtual void Activate()
    {
        if (_trackedObjects != null)
            foreach (var tracked in _trackedObjects)
                tracked.obj.GetOrCreateAnimations().SubscribeToInvalidation(tracked.member, this);
    }

    public virtual void Deactivate()
    {
        if (_trackedObjects != null)
            foreach (var tracked in _trackedObjects)
                tracked.obj.Animations?.UnsubscribeFromInvalidation(tracked.member, this);
    }

    public void Invalidate()
    {
        if (_invalidated)
            return;
        _invalidated = true;
        TargetObject.Animations?.NotifyAnimationInstanceInvalidated(Property);
    }

    public void OnTick() => Invalidate();
}

internal abstract class AnimationInstanceBase<T> : AnimationInstanceBase, IAnimationInstance<T>
{
    public AnimationInstanceBase(ServerObject target) : base(target)
    {
    }

    public abstract void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty<T> property);
    protected abstract T EvaluateCore(TimeSpan now, T currentValue);

    public T Evaluate(TimeSpan now, T currentValue)
    {
        _invalidated = false;
        return EvaluateCore(now, currentValue);
    }
}
