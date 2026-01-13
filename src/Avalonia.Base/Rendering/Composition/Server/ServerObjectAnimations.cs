using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

class ServerObjectAnimations
{
    private readonly ServerObject _owner;
    private InlineDictionary<CompositionProperty, ServerObjectSubscriptionStore> _subscriptions;
    private InlineDictionary<CompositionProperty, ServerObjectAnimationInstanceBase> _animations;
    private readonly IReadOnlyDictionary<string, CompositionProperty> _properties;

    public ServerObjectAnimations(ServerObject owner)
    {
        _owner = owner;
        _properties = CompositionProperty.TryGetPropertiesForType(owner.GetType()) ??
                      new Dictionary<string, CompositionProperty>();
    }

    private class ServerObjectSubscriptionStore
    {
        public bool IsValid;
        public RefTrackingDictionary<IAnimationInstance>? Subscribers;

        public void Invalidate()
        {
            if (!IsValid)
                return;
            IsValid = false;
            if (Subscribers != null)
                foreach (var sub in Subscribers)
                    sub.Key.Invalidate();
        }
    }

    abstract class ServerObjectAnimationInstanceBase
    {
        public ServerObjectAnimations Owner { get; }
        public bool IsDirty { get; set; } = true;
        public bool NeedsUpdate { get; set; } = true;
        public IAnimationInstance Animation { get; }

        public ServerObjectAnimationInstanceBase(ServerObjectAnimations owner, IAnimationInstance animation)
        {
            Owner = owner;
            Animation = animation;
        }


        public abstract void UpdateTargetProperty();
    }

    class ServerObjectAnimationInstance<T> : ServerObjectAnimationInstanceBase where T : struct
    {
        private readonly CompositionProperty<T> _property;

        private T _cachedVariant;

        public new IAnimationInstance<T> Animation { get; }


        public ServerObjectAnimationInstance(ServerObjectAnimations owner, IAnimationInstance<T> animation,
            CompositionProperty<T> property) : base(owner, animation)
        {

            Animation = animation;
            _property = property;
        }

        public override void UpdateTargetProperty()
        {
            if (NeedsUpdate)
            {
                NeedsUpdate = false;
                _property.SetField(Owner._owner, GetVariant());
                Owner._owner.NotifyAnimatedValueChanged(_property);
            }
        }

        public T GetVariant()
        {
            var compositor = Owner._owner.Compositor;
            if (!IsDirty)
                return _cachedVariant;

            // We are setting this _before_ evaluating animation to prevent stack overflows due to potential
            // cyclic references
            IsDirty = false;

            return _cachedVariant = Animation.Evaluate(Owner._owner.Compositor.ServerNow, _cachedVariant);
        }
    }

    class ServerObjectVariantAnimationInstance<T, TVariant> : ServerObjectAnimationInstanceBase
        where T : struct
        where TVariant : struct, IExpressionVariant<TVariant>
    {
        private readonly CompositionProperty<T> _property;

        private TVariant _cachedVariant;

        public new IAnimationInstance<TVariant> Animation { get; }


        public ServerObjectVariantAnimationInstance(ServerObjectAnimations owner, IAnimationInstance<TVariant> animation,
            CompositionProperty<T> property) : base(owner, animation)
        {

            Animation = animation;
            _property = property;
        }

        public override void UpdateTargetProperty()
        {
            if (NeedsUpdate)
            {
                NeedsUpdate = false;
                _property.SetField(Owner._owner, GetVariant().CastOrDefault<T>());
                Owner._owner.NotifyAnimatedValueChanged(_property);
            }
        }

        public TVariant GetVariant()
        {
            var compositor = Owner._owner.Compositor;
            if (!IsDirty)
                return _cachedVariant;

            // We are setting this _before_ evaluating animation to prevent stack overflows due to potential
            // cyclic references
            IsDirty = false;

            return _cachedVariant = Animation.Evaluate(Owner._owner.Compositor.ServerNow, _cachedVariant);
        }

    }

    public void Activated()
    {
        foreach (var kp in _animations)
            kp.Value.Animation.Activate();
    }

    public void Deactivated()
    {
        foreach (var kp in _animations)
            kp.Value.Animation.Deactivate();
    }

    public void OnSetDirectValue(CompositionProperty property)
    {
        if (_subscriptions.TryGetValue(property, out var subs))
            subs.Invalidate();
    }

    public void OnSetAnimatedValue<T>(CompositionProperty<T> prop, ref T field, TimeSpan committedAt, IAnimationInstance<T> animation) where T : struct
    {
        if (_owner.IsActive && _animations.TryGetValue(prop, out var oldAnimation))
            oldAnimation.Animation.Deactivate();
        _animations[prop] = new ServerObjectAnimationInstance<T>(this, animation, prop);

        animation.Initialize(committedAt, field, prop);
        if (_owner.IsActive)
            animation.Activate();

        OnSetDirectValue(prop);
    }

    public void OnSetAnimatedValue<T, TAnimationVariant>(CompositionProperty<T> prop, ref T field, TimeSpan committedAt, IAnimationInstance<TAnimationVariant> animation)
        where T : struct
        where TAnimationVariant : struct, IExpressionVariant<TAnimationVariant>
    {
        if (_owner.IsActive && _animations.TryGetValue(prop, out var oldAnimation))
            oldAnimation.Animation.Deactivate();
        _animations[prop] = new ServerObjectVariantAnimationInstance<T, TAnimationVariant>(this, animation, prop);

        animation.Initialize(committedAt, TAnimationVariant.Create(field), prop);
        if (_owner.IsActive)
            animation.Activate();

        OnSetDirectValue(prop);
    }


    public void RemoveAnimationForProperty(CompositionProperty property)
    {
        if (_animations.TryGetAndRemoveValue(property, out var animation) && _owner.IsActive)
            animation.Animation.Deactivate();
        OnSetDirectValue(property);
    }

    public void SubscribeToInvalidation(CompositionProperty member, IAnimationInstance animation)
    {
        if (!_subscriptions.TryGetValue(member, out var store))
            _subscriptions[member] = store = new ServerObjectSubscriptionStore();
        if (store.Subscribers == null)
            store.Subscribers = new();
        store.Subscribers.AddRef(animation);
    }

    public void UnsubscribeFromInvalidation(CompositionProperty member, IAnimationInstance animation)
    {
        if (_subscriptions.TryGetValue(member, out var store))
            store.Subscribers?.ReleaseRef(animation);
    }

    public ExpressionVariant GetPropertyForAnimation(string name)
    {
        if (!_properties.TryGetValue(name, out var prop))
            return default;

        if (_subscriptions.TryGetValue(prop, out var subs))
            subs.IsValid = true;

        if (_animations.TryGetValue(prop, out var animation))
            return (animation as ServerObjectAnimationInstance<ExpressionVariant>)?.GetVariant() ?? default;

        return prop.GetVariant?.Invoke(_owner) ?? default;
    }

    public void EvaluateAnimations()
    {
        foreach (var animation in _animations)
            if (animation.Value.IsDirty)
                animation.Value.UpdateTargetProperty();
    }

    public void NotifyAnimationInstanceInvalidated(CompositionProperty property)
    {
        if (_animations.TryGetValue(property, out var instance))
        {
            instance.IsDirty = instance.NeedsUpdate = true;
            _owner.Compositor.Animations.AddDirtyAnimatedObject(this);
        }
        else
            Debug.Assert(false);
    }
}
