using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Input;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Styling;
using Avalonia.Threading;
using static Avalonia.Rendering.Composition.Animations.PropertySetSnapshot;

namespace Avalonia.Rendering.Composition;

public partial class InteractionTracker : CompositionObject
{
    private int _requestId = 0;
    public IInteractionTrackerOwner? Owner { get; init; }


    private InteractionTrackerState _state = null!;

    public double MinScale { get; set; } = 1.0;

    public double MaxScale { get; set; } = 1.0;

    public Vector3D MinPosition { get; set; }

    public Vector3D MaxPosition { get; set; }

    public Vector3D? PositionInertiaDecayRate { get; set; }

    public Vector3D Position => Server.Position;

    public double Scale => Server.Scale;

    private int _count = 0;

    internal new ServerInteractionTracker Server { get; }

    internal InteractionTracker(Compositor compositor, ServerInteractionTracker server) : base(compositor, server)
    {
        Server = server;
        Server.Activate();
        _state = new InteractionTrackerIdleState(this, 0, isInitialIdleState: true);
    }

    internal void SetPosition(Vector3D newPosition, int requestId)
    {
        if (Position == newPosition)
            return;
        Server.Position = newPosition;
        Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, Scale, requestId));
    }

    internal void SetScale(double newScale, int requestId)
    {
        if (CompositionMathHelpers.IsCloseReal(Scale, newScale))
            return;

        Server.Scale = newScale;
        Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(Position, newScale, requestId));
    }

    internal void SetPositionAndScale(Vector3D newPosition, double newScale, int requestId)
    {
        if (CompositionMathHelpers.IsCloseReal(Scale, newScale) && Position == newPosition)
            return;
        Server.Position = newPosition;
        Server.Scale = newScale;
        Owner?.ValuesChanged(this, new InteractionTrackerValuesChangedArgs(newPosition, newScale, requestId));
    }

    internal void ChangeState(InteractionTrackerState newState)
    {
        Interlocked.Increment(ref _count);
        Debug.WriteLine($"{_count}:{_state.GetType().Name.Replace("InteractionTracker", "")} -> {newState.GetType().Name.Replace("InteractionTracker", "")}");
        _state = newState;
    }

    internal void StartUserManipulation(Point position, IPointer pointer)
    {
        _state.StartUserManipulation(position, pointer);
    }

    internal void CompleteUserManipulation()
    {
        _state.CompleteUserManipulation();
    }

    internal void ReceiveManipulationDelta(Point translationDelta)
    {
        _state.ReceiveManipulationDelta(-translationDelta);
    }

    internal void ReceiveInertiaStarting(Point linearVelocity)
    {
        _state.ReceiveInertiaStarting(-linearVelocity);
    }

    internal void ReceiveScaleDelta(Point origin, double delta)
    {
        _state.ReceiveScaleDelta(origin, delta);
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

    public int TryUpdatePosition(Vector3D value)
        => TryUpdatePosition(value, InteractionTrackerClampingOption.Auto);

    public int TryUpdatePositionBy(Vector3D amount)
        => TryUpdatePosition(Server.Position + amount);

    public int TryUpdatePosition(Vector3D value, InteractionTrackerClampingOption option)
    {
        var id = Interlocked.Increment(ref _requestId);
        _state.TryUpdatePosition(value, option, id);
        return id;
    }

    public int TryUpdatePositionBy(Vector3D amount, InteractionTrackerClampingOption option)
        => TryUpdatePosition(Server.Position + amount, option);

    internal void TryUpdateScale(double scale)
    {
        SetScale(scale, 0);
    }
}
