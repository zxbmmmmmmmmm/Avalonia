using System.Numerics;

namespace Avalonia.Rendering.Composition.Interaction;

internal abstract class InteractionTrackerState
{
    private protected InteractionTracker _interactionTracker;
    private protected bool _disposed;

    protected InteractionTrackerState(InteractionTracker interactionTracker)
    {
        _interactionTracker = interactionTracker;
        // ReSharper disable once VirtualMemberCallInConstructor
        EnterState(interactionTracker.Owner);
    }

    protected abstract void EnterState(IInteractionTrackerOwner? owner);
    internal abstract void StartUserManipulation();
    internal abstract void CompleteUserManipulation(Vector3D linearVelocity);
    internal abstract void ReceiveManipulationDelta(Point translationDelta);
    internal abstract void ReceiveInertiaStarting(Point linearVelocity);
    internal abstract void ReceivePointerWheel(int delta, bool isHorizontal);
    internal abstract void TryUpdatePositionWithAdditionalVelocity(Vector3D velocityInPixelsPerSecond, int requestId);
    internal abstract void TryUpdatePosition(Vector3D value, InteractionTrackerClampingOption option, int requestId);
}
