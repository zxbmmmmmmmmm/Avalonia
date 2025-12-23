using System;
using System.Diagnostics;
using System.Numerics;

namespace Avalonia.Rendering.Composition.Interaction;

internal sealed partial class InteractionTrackerActiveInputInertiaHandler : IInteractionTrackerInertiaHandler
{
    private readonly InteractionTracker _interactionTracker;
    private readonly AxisHelper _xHelper;
    private readonly AxisHelper _yHelper;
    private readonly AxisHelper _zHelper;
    private readonly int _requestId;

    private Timer? _timer;
    private Stopwatch? _stopwatch;

    // InteractionTracker works at 60 FPS, per documentation
    // https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
    // > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
    private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

    public Vector3 InitialVelocity => new Vector3(_xHelper.InitialVelocity, _yHelper.InitialVelocity, _zHelper.InitialVelocity);
    public Vector3 FinalPosition => new Vector3(_xHelper.FinalValue, _yHelper.FinalValue, _zHelper.FinalValue);
    public Vector3 FinalModifiedPosition => new Vector3(_xHelper.FinalModifiedValue, _yHelper.FinalModifiedValue, _zHelper.FinalModifiedValue);
    public float FinalScale => _interactionTracker.Scale; // TODO: Scale not yet implemented

    public InteractionTrackerActiveInputInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities, int requestId)
    {
        _interactionTracker = interactionTracker;
        _xHelper = new AxisHelper(this, translationVelocities, Axis.X);
        _yHelper = new AxisHelper(this, translationVelocities, Axis.Y);
        _zHelper = new AxisHelper(this, translationVelocities, Axis.Z);
        _requestId = requestId;
    }

    public void Start()
    {
        if (_timer is not null)
        {
            throw new InvalidOperationException("Cannot start inertia timer twice.");
        }

        _stopwatch = Stopwatch.StartNew();
        _timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _stopwatch?.Stop();
    }

    private void OnTick(object? state)
    {
        var currentElapsedInSeconds = _stopwatch!.ElapsedMilliseconds / 1000.0f;

        if (_xHelper.HasCompleted && _yHelper.HasCompleted && _zHelper.HasCompleted)
        {
            _interactionTracker.SetPosition(FinalModifiedPosition, _requestId);
            _interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
            _timer!.Dispose();
            _stopwatch!.Stop();
            return;
        }

        var newPosition = new Vector3(
            _xHelper.GetPosition(currentElapsedInSeconds),
            _yHelper.GetPosition(currentElapsedInSeconds),
            _zHelper.GetPosition(currentElapsedInSeconds));

        _interactionTracker.SetPosition(newPosition, _requestId);
    }
    private enum Axis
    {
        X,
        Y,
        Z
    }

    private sealed class AxisHelper
    {
        private float? _dampingStateTimeInSeconds;
        private float? _dampingStatePosition;

        internal InteractionTrackerActiveInputInertiaHandler Handler { get; }
        internal float DecayRate { get; }
        internal float InitialVelocity { get; }
        internal float InitialValue { get; }
        internal float FinalValue { get; }
        internal float FinalModifiedValue { get; }
        internal float TimeToMinimumVelocity { get; }
        internal Axis Axis { get; }

        internal bool HasCompleted { get; private set; }

        public AxisHelper(InteractionTrackerActiveInputInertiaHandler handler, Vector3 velocities, Axis axis)
        {
            Axis = axis;
            Handler = handler;
            InitialVelocity = GetValue(velocities);
            DecayRate = 1.0f - GetValue(Handler._interactionTracker.PositionInertiaDecayRate ?? new(0.95f));
            InitialValue = GetValue(Handler._interactionTracker.Position);

            TimeToMinimumVelocity = GetTimeToMinimumVelocity();

            var deltaPosition = CalculateDeltaPosition(TimeToMinimumVelocity);

            FinalValue = InitialValue + deltaPosition;
            FinalModifiedValue = Math.Clamp(FinalValue, GetValue(Handler._interactionTracker.MinPosition), GetValue(Handler._interactionTracker.MaxPosition));
        }

        private float GetValue(Vector3 vector)
        {
            return Axis switch
            {
                Axis.X => vector.X,
                Axis.Y => vector.Y,
                Axis.Z => vector.Z,
                _ => throw new ArgumentException("Invalid value for axis.")
            };
        }

        private float GetTimeToMinimumVelocity()
        {
            var epsilon = 0.0000011920929f;

            var minimumVelocity = 30.0f;

            return TimeToMinimumVelocityCore(MathF.Abs(InitialVelocity), DecayRate, InitialValue);

            float TimeToMinimumVelocityCore(float initialVelocity, float decayRate, float initialPosition)
            {
                var time = 0.0f;
                if (initialVelocity > minimumVelocity)
                {
                    if (!CompositionMathHelpers.IsCloseReal(decayRate, 1.0f, epsilon))
                    {
                        if (CompositionMathHelpers.IsCloseRealZero(decayRate, epsilon) /*|| !_isInertiaEnabled*/)
                        {
                            return 0.0f;
                        }
                        else
                        {
                            return (MathF.Log(minimumVelocity) - MathF.Log(initialVelocity)) / MathF.Log(decayRate);
                        }
                    }

                    time = (Math.Sign(initialVelocity) * float.MaxValue - initialPosition) / initialVelocity;

                    if (time < 0.0f)
                    {
                        return 0.0f;
                    }
                }

                return time;
            }
        }

        private float CalculateDeltaPosition(float time)
        {
            float epsilon = 0.0000011920929f;

            if (CompositionMathHelpers.IsCloseReal(DecayRate, 1.0f, epsilon))
            {
                return InitialVelocity * time;
            }
            else if (CompositionMathHelpers.IsCloseRealZero(DecayRate, epsilon) /*|| !_isInertiaEnabled*/)
            {
                return 0.0f;
            }
            else
            {
                float val = MathF.Pow(DecayRate, time);
                return ((val - 1.0f) * InitialVelocity) / MathF.Log(DecayRate);
            }
        }

        public float GetPosition(float currentElapsedInSeconds)
        {
            if (currentElapsedInSeconds >= TimeToMinimumVelocity)
            {
                HasCompleted = true;
                return FinalModifiedValue;
            }

            if (_dampingStateTimeInSeconds.HasValue)
            {
                var settlingTime = TimeToMinimumVelocity - _dampingStateTimeInSeconds.Value;
                var wn = 5.8335 / settlingTime;
                // It seems WinUI can use an underdamped animation in some cases. For now we only use critically damped animation.
                var value = DampingHelper.SolveCriticallyDamped(wn, currentElapsedInSeconds - _dampingStateTimeInSeconds.Value);
                value = value * (FinalModifiedValue - _dampingStatePosition!.Value) + _dampingStatePosition.Value;

                return (float)value;
            }

            var currentPosition = GetValue(Handler._interactionTracker.Position);
            var minPosition = GetValue(Handler._interactionTracker.MinPosition);
            var maxPosition = GetValue(Handler._interactionTracker.MaxPosition);
            if (currentPosition < minPosition || currentPosition > maxPosition)
            {
                // This is an overpan from Interacting state. Use damping animation.
                _dampingStateTimeInSeconds = Handler._stopwatch!.ElapsedMilliseconds / 1000.0f;
                _dampingStatePosition = currentPosition;
            }

            return InitialValue + CalculateDeltaPosition(currentElapsedInSeconds);
        }
    }
}
