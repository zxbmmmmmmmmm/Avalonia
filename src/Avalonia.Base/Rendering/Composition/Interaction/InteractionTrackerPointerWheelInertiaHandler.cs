using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

internal class InteractionTrackerPointerWheelInertiaHandler : ServerObject, IServerClockItem, IInteractionTrackerInertiaHandler
{
    // InteractionTracker works at 60 FPS, per documentation
    // https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
    // > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
    //private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

    private Stopwatch? _stopwatch;

    private readonly InteractionTracker _interactionTracker;
    private readonly Vector3D _minPosition;
    private readonly Vector3D _maxPosition;
    private readonly Vector3D _initialPosition;
    private readonly Vector3D _calculatedFinalPosition;

    public InteractionTrackerPointerWheelInertiaHandler(ServerCompositor serverCompositor, InteractionTracker interactionTracker, Vector3D translationVelocities)
        : base(serverCompositor)
    {
        _interactionTracker = interactionTracker;
        _minPosition = interactionTracker.MinPosition;
        _maxPosition = interactionTracker.MaxPosition;
        _initialPosition = _interactionTracker.Position;

        InitialVelocity = translationVelocities;

        // This handler works with constant velocity for 0.25 second.
        _calculatedFinalPosition = interactionTracker.Position + InitialVelocity * 0.25f;
    }

    public Vector3D InitialVelocity { get; }

    public Vector3D FinalPosition => Vector3D.Clamp(_calculatedFinalPosition, _minPosition, _maxPosition);

    public Vector3D FinalModifiedPosition => FinalPosition;

    public double FinalScale => _interactionTracker.Scale; // TODO: Scale not yet implemented

    public void Start()
    {
        Compositor.Animations.AddToClock(this);
        _stopwatch = Stopwatch.StartNew();
    }

    public void Stop()
    {
        Compositor.Animations.RemoveFromClock(this);
        _stopwatch?.Stop();
    }


    public void OnTick()
    {
        var currentElapsed = _stopwatch!.ElapsedMilliseconds;

        if (currentElapsed >= 250)
        {
            _interactionTracker.SetPosition(FinalModifiedPosition, requestId: 0);
            _interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
            _stopwatch!.Stop();
            return;
        }

        var newPosition = _initialPosition + Vector3D.Multiply(InitialVelocity, (currentElapsed / 1000.0)) ;// TODO: 实现速度曲线以支持惯性
        //var clampedNewPosition = Vector3D.Clamp(newPosition, _minPosition, _maxPosition);
        // TODO: fix clamp
        _interactionTracker.SetPosition(newPosition, requestId: 0);

        if (newPosition.Equals(FinalModifiedPosition))
        {
            _interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
            _stopwatch!.Stop();
        }
    }
}
