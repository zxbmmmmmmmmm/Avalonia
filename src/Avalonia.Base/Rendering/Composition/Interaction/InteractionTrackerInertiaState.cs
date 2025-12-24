using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Avalonia.Rendering.Composition;

internal sealed class InteractionTrackerInertiaState : InteractionTrackerState
{
    private readonly IInteractionTrackerInertiaHandler _handler;
    private readonly int _requestId;

    public InteractionTrackerInertiaState(InteractionTracker interactionTracker, Vector3D translationVelocities, int requestId, bool isFromPointerWheel) : base(interactionTracker)
    {
        _requestId = requestId;
        
        _handler = isFromPointerWheel
            ? new InteractionTrackerPointerWheelInertiaHandler(interactionTracker.Server!.Compositor, interactionTracker, translationVelocities)
            : new InteractionTrackerActiveInputInertiaHandler(interactionTracker.Server!.Compositor, interactionTracker, translationVelocities, _requestId);
        EnterState(interactionTracker.Owner);
    }

    protected override void EnterState(IInteractionTrackerOwner? owner)
    {
        if (_disposed)
        {
            return;
        }

        owner?.InertiaStateEntered(_interactionTracker, new InteractionTrackerInertiaStateEnteredArgs()
        {
            IsFromBinding = false, /* TODO */
            IsInertiaFromImpulse = false, /* TODO */
            ModifiedRestingPosition = _handler.FinalModifiedPosition,
            ModifiedRestingScale = Math.Clamp(_handler.FinalScale, _interactionTracker.MinScale, _interactionTracker.MaxScale),
            NaturalRestingPosition = _handler.FinalPosition,
            NaturalRestingScale = _handler.FinalScale,
            PositionVelocityInPixelsPerSecond = _handler.InitialVelocity,
            RequestId = _requestId,
            ScaleVelocityInPercentPerSecond = 0.0f, /* TODO: Scale not yet implemented */
        });

        // If TryUpdatePosition is called with clamping option disabled, the position set can go outside the [MinPosition..MaxPosition] range.
        // We adjust MinPosition/MaxPosition when we enter idle.
        // Docs around InteractionTrackerClampingOption.Disabled: https://learn.microsoft.com/uwp/api/windows.ui.composition.interactions.interactiontrackerclampingoption
        // > If the input value is greater (or less) than the max (or min) value, it is not immediately
        // > clamped. Instead, the max/min is enforced to the newly input value
        // > of Position (and potentially clamped) the next time InteractionTracker enters
        // > the Inertia state.
        // TODO: Commented out for now. It's wrong to do this when transitioning from interacting to inertia.
        //var position = _interactionTracker.Position;
        //_interactionTracker.MinPosition = Vector3.Min(_interactionTracker.MinPosition, position);
        //_interactionTracker.MaxPosition = Vector3.Max(_interactionTracker.MaxPosition, position);

        _handler.Start();
    }

    internal override void StartUserManipulation()
    {
        _interactionTracker.ChangeState(new InteractionTrackerInteractingState(_interactionTracker));
        _handler.Stop();
    }

    internal override void CompleteUserManipulation(Vector3D linearVelocity)
    {
    }

    internal override void ReceiveManipulationDelta(Point translationDelta)
    {
    }

    internal override void ReceiveInertiaStarting(Point linearVelocity)
    {
    }

    internal override void ReceivePointerWheel(int delta, bool isHorizontal)
    {
        var newDelta = isHorizontal ? new Vector3D(delta, 0, 0) : new Vector3D(0, delta, 0);
        var totalDelta = (_handler.FinalModifiedPosition - _interactionTracker.Position) + newDelta;
        // Constant velocity for 250ms
        var velocity = Vector3D.Divide(totalDelta,0.25);
        _interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocity, requestId: 0, isFromPointerWheel: true));
        _handler.Stop();
    }

    internal override void TryUpdatePositionWithAdditionalVelocity(Vector3D velocityInPixelsPerSecond, int requestId)
    {
        // Inertia is restarted (state re-enters inertia) and inertia modifiers are evaluated with requested velocity added to current velocity
        _interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, _handler.InitialVelocity + velocityInPixelsPerSecond, requestId, isFromPointerWheel: false));
        _handler.Stop();
    }

    internal override void TryUpdatePosition(Vector3D value, InteractionTrackerClampingOption option, int requestId)
    {
        if (option == InteractionTrackerClampingOption.Auto)
        {
            value = Vector3D.Clamp(value, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
        }

        _interactionTracker.SetPosition(value, requestId);
        _interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId));
        _handler.Stop();
    }

}
