using System.Numerics;

namespace Avalonia.Rendering.Composition.Interaction;

internal abstract class InteractionTrackerState
{
    private protected ServerInteractionTracker _interactionTracker;
    private protected bool _disposed;

    protected InteractionTrackerState(ServerInteractionTracker interactionTracker)
    {
        _interactionTracker = interactionTracker;
        // ReSharper disable once VirtualMemberCallInConstructor
        EnterState(interactionTracker.Owner);
    }

    protected abstract void EnterState(IInteractionTrackerOwner? owner);
    internal abstract void StartUserManipulation();
    internal abstract void CompleteUserManipulation(Vector3 linearVelocity);
    internal abstract void ReceiveManipulationDelta(Point translationDelta);
    internal abstract void ReceiveInertiaStarting(Point linearVelocity);
    internal abstract void ReceivePointerWheel(int delta, bool isHorizontal);
    internal abstract void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId);
    internal abstract void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId);
}
