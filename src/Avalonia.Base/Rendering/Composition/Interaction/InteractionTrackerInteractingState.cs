using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Avalonia.Input;

namespace Avalonia.Rendering.Composition;

internal sealed class InteractionTrackerInteractingState : InteractionTrackerState
{
    public InteractionTrackerInteractingState(InteractionTracker interactionTracker) : base(interactionTracker)
    {
        EnterState(interactionTracker.Owner);
    }

    protected override void EnterState(IInteractionTrackerOwner? owner)
    {
        owner?.InteractingStateEntered(_interactionTracker, new InteractionTrackerInteractingStateEnteredArgs(requestId: 0, isFromBinding: false));
    }

    internal override void StartUserManipulation(Point position, IPointer pointer)
    {
        // This probably shouldn't happen.
        // We ignore.
        //if (this.Log().IsEnabled(LogLevel.Error))
        //{
        //    this.Log().Error("Unexpected StartUserManipulation while in interacting state");
        //}
    }

    internal override void CompleteUserManipulation()
    {
        _interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, default, requestId: 0, false));
    }

    internal override void ReceiveManipulationDelta(Point translationDelta)
    {
        _interactionTracker.SetPosition(_interactionTracker.Position + new Vector3D((float)translationDelta.X, (float)translationDelta.Y, 0), requestId: 0);
    }

    internal override void ReceiveInertiaStarting(Point linearVelocity)
    {
        _interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, new Vector3D((float)linearVelocity.X, (float)linearVelocity.Y, 0), requestId: 0, isFromPointerWheel: false));
    }

    internal override void ReceivePointerWheel(int delta, bool isHorizontal)
    {
    }

    internal override void TryUpdatePositionWithAdditionalVelocity(Vector3D velocityInPixelsPerSecond, int requestId)
    {
        _interactionTracker.Owner?.RequestIgnored(_interactionTracker, new InteractionTrackerRequestIgnoredArgs(requestId));
    }

    internal override void TryUpdatePosition(Vector3D value, InteractionTrackerClampingOption option, int requestId)
    {
        _interactionTracker.Owner?.RequestIgnored(_interactionTracker, new InteractionTrackerRequestIgnoredArgs(requestId));
    }
}
