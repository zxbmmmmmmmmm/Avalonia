using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    /// Server-side counterpart of KeyFrameAnimation with values baked-in
    /// </summary>
    class KeyFrameAnimationInstance<T> : AnimationInstanceBase<T>, IAnimationInstance where T : struct
    {
        private readonly IInterpolator<T> _interpolator;
        private readonly ServerKeyFrame<T>[] _keyFrames;
        private readonly T? _finalValue;
        private readonly AnimationDelayBehavior _delayBehavior;
        private readonly TimeSpan _delayTime;
        private readonly PlaybackDirection _direction;
        private readonly TimeSpan _duration;
        private readonly AnimationIterationBehavior _iterationBehavior;
        private readonly int _iterationCount;
        private readonly AnimationStopBehavior _stopBehavior;
        private TimeSpan _startedAt;
        private T _startingValue;
        private readonly TimeSpan _totalDuration;
        private bool _finished;

        public KeyFrameAnimationInstance(
            IInterpolator<T> interpolator, ServerKeyFrame<T>[] keyFrames, T? finalValue,
            ServerObject target,
            AnimationDelayBehavior delayBehavior, TimeSpan delayTime,
            PlaybackDirection direction, TimeSpan duration,
            AnimationIterationBehavior iterationBehavior,
            int iterationCount, AnimationStopBehavior stopBehavior) : base(target)
        {
            _interpolator = interpolator;
            _keyFrames = keyFrames;
            _finalValue = finalValue;
            _delayBehavior = delayBehavior;
            _delayTime = delayTime;
            _direction = direction;
            _duration = duration;
            _iterationBehavior = iterationBehavior;
            _iterationCount = iterationCount;
            _stopBehavior = stopBehavior;
            if (_iterationBehavior == AnimationIterationBehavior.Count)
                _totalDuration = delayTime.Add(TimeSpan.FromTicks(iterationCount * _duration.Ticks));
            if (_keyFrames.Length == 0)
                throw new InvalidOperationException("Animation has no key frames");
            if (_duration.Ticks <= 0)
                throw new InvalidOperationException("Invalid animation duration");
        }


        protected override T EvaluateCore(TimeSpan now, T currentValue)
        {
            var starting = _startingValue;
            var elapsed = now - _startedAt;
            var res = EvaluateImpl(elapsed, currentValue);

            if (_iterationBehavior == AnimationIterationBehavior.Count
                && !_finished
                && elapsed > _totalDuration)
            {
                // Active check?
                TargetObject.Compositor.Animations.RemoveFromClock(this);
                _finished = true;
            }
            return res;
        }

        private T EvaluateImpl(TimeSpan elapsed, T currentValue)
        {
            if (elapsed < _delayTime)
            {
                if (_delayBehavior == AnimationDelayBehavior.SetInitialValueBeforeDelay)
                    return KeyFrameAnimationInstance<T>.GetKeyFrame(_keyFrames[0]);
                return currentValue;
            }

            elapsed -= _delayTime;
            var iterationNumber = elapsed.Ticks / _duration.Ticks;
            if (_iterationBehavior == AnimationIterationBehavior.Count
                && iterationNumber >= _iterationCount)
                return KeyFrameAnimationInstance<T>.GetKeyFrame(_keyFrames[_keyFrames.Length - 1]);


            var evenIterationNumber = iterationNumber % 2 == 0;
            elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);

            var reverse =
                _direction == PlaybackDirection.Alternate
                    ? !evenIterationNumber
                    : _direction == PlaybackDirection.AlternateReverse
                        ? evenIterationNumber
                        : _direction == PlaybackDirection.Reverse;

            var iterationProgress = elapsed.TotalSeconds / _duration.TotalSeconds;
            if (reverse)
                iterationProgress = 1 - iterationProgress;

            var left = new ServerKeyFrame<T>
            {
                Value = _startingValue
            };
            var right = _keyFrames[_keyFrames.Length - 1];
            for (var c = 0; c < _keyFrames.Length; c++)
            {
                var kf = _keyFrames[c];
                if (kf.Key < iterationProgress)
                {
                    // this is the last frame
                    if (c == _keyFrames.Length - 1)
                        return KeyFrameAnimationInstance<T>.GetKeyFrame(kf);

                    left = kf;
                    right = _keyFrames[c + 1];
                }
                else if (c == 0)
                {
                    // The current progress is before the first frame, we implicitly use the starting value 
                    // as the first frame in this case
                    right = _keyFrames[c];
                    break;
                }
                else
                    break;
            }

            var keyProgress = Math.Max(0, Math.Min(1, (iterationProgress - left.Key) / (right.Key - left.Key)));

            var easedKeyProgress = (float)right.EasingFunction.Ease(keyProgress);
            if (float.IsNaN(easedKeyProgress) || float.IsInfinity(easedKeyProgress))
                return currentValue;

            return _interpolator.Interpolate(
                KeyFrameAnimationInstance<T>.GetKeyFrame(left),
                KeyFrameAnimationInstance<T>.GetKeyFrame(right),
                easedKeyProgress
            );
        }

        static T GetKeyFrame(ServerKeyFrame<T> f)
        {
            if (f.Expression != null)
                return f.Expression.Evaluate();
            else
                return f.Value;
        }

        public override void Initialize(TimeSpan startedAt, T startingValue, CompositionProperty<T> property)
        {
            _startedAt = startedAt;
            _startingValue = startingValue;
            var hs = new HashSet<(ServerObject name, CompositionProperty member)>();

            // TODO: Update subscriptions based on the current keyframe rather than keeping subscriptions to all of them
            foreach (var frame in _keyFrames)
            {
                if (frame.Expression?.References is null)
                    continue;
                foreach(var r in frame.Expression.References)
                {
                    hs.Add(r);
                }
            }
            base.Initialize(property, hs);
        }

        public override void Activate()
        {
            if (_finished)
            {
                return;
            }
            TargetObject.Compositor.Animations.AddToClock(this);
            base.Activate();
        }

        public override void Deactivate()
        {
            TargetObject.Compositor.Animations.RemoveFromClock(this);
            base.Deactivate();
        }
    }
}
