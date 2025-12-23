using System.Numerics;
using System.Threading;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Styling;

namespace Avalonia.Rendering.Composition.Interaction;

public class InteractionTracker : CompositionObject
{
    private int _requestId = 0;
    public IInteractionTrackerOwner? Owner { get; }
    internal new ServerInteractionTracker? Server { get; }

    private InteractionTrackerState _state;

    public float MinScale { get; set; } = 1.0f;

    public float MaxScale { get; set; } = 1.0f;

    public float Scale { get; private set; } = 1.0f;

    public Vector3 MinPosition { get; set; }

    public Vector3 MaxPosition { get; set; }

    public Vector3 Position { get; private set; }

    internal InteractionTracker(Compositor compositor, ServerInteractionTracker server, IInteractionTrackerOwner? owner = null) 
        : base(compositor, new ServerInteractionTracker(compositor.Server))
    {
        Server = server;
        Owner = owner;
        _state = new InteractionTrackerIdleState(this, 0, isInitialIdleState: true);
    }
    internal void SetPosition(Vector3 newPosition, int requestId)
    {
        if (Position != newPosition)
        {
            Position = newPosition;
            Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, Scale, requestId));
            //OnPropertyChanged(nameof(Position), isSubPropertyChange: false);
        }
    }

    internal void ChangeState(InteractionTrackerState newState)
    {
        _state = newState;
    }

    internal void StartUserManipulation()
    {
        _state.StartUserManipulation();
    }

    internal void CompleteUserManipulation(Vector3 linearVelocity)
    {
        _state.CompleteUserManipulation(-linearVelocity);
    }

    internal void ReceiveManipulationDelta(Point translationDelta)
    {
        _state.ReceiveManipulationDelta(-translationDelta);
    }

    internal void ReceiveInertiaStarting(Point linearVelocity)
    {
        _state.ReceiveInertiaStarting(-linearVelocity);
    }

    internal void ReceivePointerWheel(int mouseWheelTicks, bool isHorizontal)
    {
        // On WinUI, this depends on mouse setting "how many lines to scroll each time"
        // The default Windows setting is 3 lines, and each line is 16px.
        // Note: the value for each line may vary depending on scaling.
        // For now, we just use 16*3=48.
        var delta = mouseWheelTicks * 48;
        _state.ReceivePointerWheel(-delta, isHorizontal);
    }

    public int TryUpdatePosition(Vector3 value)
        => TryUpdatePosition(value, InteractionTrackerClampingOption.Auto);

    public int TryUpdatePositionBy(Vector3 amount)
        => TryUpdatePosition(Position + amount);

    public int TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option)
    {
        var id = Interlocked.Increment(ref _requestId);
        _state.TryUpdatePosition(value, option, id);
        return id;
    }

    public int TryUpdatePositionBy(Vector3 amount, InteractionTrackerClampingOption option)
        => TryUpdatePosition(Position + amount, option);
}
