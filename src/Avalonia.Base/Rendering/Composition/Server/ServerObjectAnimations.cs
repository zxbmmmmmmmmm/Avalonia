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
    private InlineDictionary<CompositionProperty, ServerObjectAnimationInstance> _animations;
    private readonly IReadOnlyDictionary<string, CompositionProperty> _properties;

    public ServerObjectAnimations(ServerObject owner)
    {
        _owner = owner;
        _properties = CompositionProperty.TryGetPropertiesForType(owner.GetType()) ??
                      new Dictionary<string, CompositionProperty>();
    }

    private class ServerObjectSubscriptionStore
    {
        public bool IsValid = true;
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
    
    abstract class ServerObjectAnimationInstance
    {
        public ServerObjectAnimations Owner { get; }
        public bool IsDirty { get; set; } = true;
        public bool NeedsUpdate { get; set; } = true;
        public IAnimationInstance Animation { get; }

        public ServerObjectAnimationInstance(ServerObjectAnimations owner, IAnimationInstance animation)
        {
            Animation = animation;
            Owner = owner;
        }


        public abstract void UpdateTargetProperty();
    }

    class ServerObjectAnimationInstance<T> : ServerObjectAnimationInstance where T : struct
    {
        private readonly CompositionProperty<T> _property;
        private T _cachedVariant;
        public new IAnimationInstance<T> Animation { get; }

        public ServerObjectAnimationInstance(ServerObjectAnimations owner, IAnimationInstance<T> animation,
            CompositionProperty<T> property) : base(owner, animation)
        {
            _property = property;
            Animation = animation;
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

        public override void UpdateTargetProperty()
        {
            if (NeedsUpdate)
            {
                NeedsUpdate = false;
                _property.SetField(Owner._owner, GetVariant());
                Owner._owner.NotifyAnimatedValueChanged(_property);
                Owner.OnSetDirectValue(_property);
            }
        }
    }

    public void Activated()
    {
        foreach(var kp in _animations)
            kp.Value.Animation.Activate();
    }

    public void Deactivated()
    {
        foreach(var kp in _animations)
            kp.Value.Animation.Deactivate();
    }

    public void OnSetDirectValue(CompositionProperty property)
    {
        if(_subscriptions.TryGetValue(property, out var subs))
            subs.Invalidate();
    }
    
    public void OnSetAnimatedValue<T>(CompositionProperty<T> prop, ref T field, TimeSpan committedAt, IAnimationInstance<T> animation) where T : struct
    {
        if (_owner.IsActive && _animations.TryGetValue(prop, out var oldAnimation))
            oldAnimation.Animation.Deactivate();
        _animations[prop] = new ServerObjectAnimationInstance<T>(this, animation, prop);
            
        animation.Initialize(committedAt, field, prop);
        if(_owner.IsActive)
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

    public void ValidSubscription(CompositionProperty prop)
    {
        if (_subscriptions.TryGetValue(prop, out var subs))
            subs.IsValid = true;
    }

    public void UnsubscribeFromInvalidation(CompositionProperty member, IAnimationInstance animation)
    {
        if(_subscriptions.TryGetValue(member, out var store))
            store.Subscribers?.ReleaseRef(animation);
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
