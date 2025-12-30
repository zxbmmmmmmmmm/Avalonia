using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Rendering.Composition;

public class InputElementInteractionSource
{
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
            _tracker.ReceivePointerWheel((int)e.Delta.Y, false);
        }
        else
        {
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
            _tracker.ReceiveManipulationDelta(delta);
            _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), position - _pressedPosition);
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
