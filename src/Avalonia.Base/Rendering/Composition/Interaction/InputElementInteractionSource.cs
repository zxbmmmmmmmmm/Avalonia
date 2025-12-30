using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Rendering.Composition;

public class InputElementInteractionSource
{
    /// <summary>
    /// Defines how interactions are processed for an <see cref="VisualInteractionSource"/> on the scale axis.
    /// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send scale data to <see cref="InteractionTracker"/>.
    /// </summary>
    public InteractionSourceMode ScaleSourceMode { get; set; } = InteractionSourceMode.Disabled;

    /// <summary>
    /// Source mode for the X-axis.
    /// The <see cref="PositionXSourceMode"/> property defines how interactions are processed for a <see cref="VisualInteractionSource"/> on the X-axis.
    /// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send X-axis data to <see cref="InteractionTracker"/>.
    /// </summary>
    public InteractionSourceMode PositionXSourceMode { get; set; } = InteractionSourceMode.EnabledWithInertia;

    /// <summary>
    /// Source mode for the Y-axis.
    /// The <see cref="PositionYSourceMode"/> property defines how interactions are processed for a <see cref="VisualInteractionSource"/> on the Y-axis.
    /// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send Y-axis data to <see cref="InteractionTracker"/>.
    /// </summary>
    public InteractionSourceMode PositionYSourceMode { get; set; } = InteractionSourceMode.EnabledWithInertia;

    private readonly InteractionTracker _tracker; // TODO: Support multiple trackers
    private readonly InputElement _inputElement;
    private IPointer? _pointer;
    private Point _pressedPosition;
    private Point _lastPosition;
    private VelocityTracker? _velocityTracker;

    public InputElementInteractionSource(InputElement inputElement, InteractionTracker tracker)
    {
        _inputElement = inputElement;
        _inputElement.PointerPressed += OnPointerPressed;
        _inputElement.PointerMoved += OnPointerMoved;
        _inputElement.PointerReleased += OnPointerReleased;
        _inputElement.PointerCaptureLost += OnPointerCaptureLost;
        _inputElement.PointerWheelChanged += OnPointerWheelChanged;
        _tracker = tracker;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y != 0)
        {
            if (PositionYSourceMode is InteractionSourceMode.Disabled)
                return;
            _tracker.ReceivePointerWheel((int)e.Delta.Y, false);
        }
        else
        {
            if (PositionXSourceMode is InteractionSourceMode.Disabled)
                return;
            _tracker.ReceivePointerWheel((int)e.Delta.X, true);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer != null)
        {
            return;
        }

        _pointer = e.Pointer;
        e.PreventGestureRecognition();
        _pressedPosition = e.GetPosition(_inputElement);
        _lastPosition = _pressedPosition;
        _velocityTracker = new VelocityTracker();
        _velocityTracker.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), default);
        _pointer.Capture(_inputElement);
        _tracker.StartUserManipulation();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer)
        {
            return;
        }

        var position = e.GetPosition(_inputElement);
        var delta = position - _lastPosition;

        if (delta != default)
        {
            if (PositionXSourceMode is InteractionSourceMode.Disabled)
            {
                delta = delta.WithX(0);
            }
            if (PositionYSourceMode is InteractionSourceMode.Disabled)
            {
                delta = delta.WithY(0);
            }
            _tracker.ReceiveManipulationDelta(delta);
            _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), delta);
            _lastPosition = position;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer)
        {
            return;
        }

        var velocity = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;
        if (PositionXSourceMode is InteractionSourceMode.Disabled)
        {
            velocity = velocity.WithX(0);
        }
        if (PositionYSourceMode is InteractionSourceMode.Disabled)
        {
            velocity = velocity.WithY(0);
        }
        if (velocity != Vector.Zero)
        {
            _tracker.ReceiveInertiaStarting(new Point(velocity.X, velocity.Y));
        }
        else
        {
            _tracker.CompleteUserManipulation(default);
        }

        _pointer.Capture(null);
        Reset();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {

    }

    private void Reset()
    {
        _pointer = null;
        _velocityTracker = null;
        _pressedPosition = default;
        _lastPosition = default;
    }
}

public enum InteractionSourceMode
{
    /// <summary>
    /// Interaction is disabled.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Interaction is enabled with inertia.
    /// </summary>
    EnabledWithInertia = 1,

    /// <summary>
    /// Interaction is enabled without inertia.
    /// </summary>
    EnabledWithoutInertia = 2,
}
