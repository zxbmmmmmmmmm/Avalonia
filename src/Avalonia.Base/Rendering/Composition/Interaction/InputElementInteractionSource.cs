using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Rendering.Composition;

public class InputElementInteractionSource : IDisposable
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
    private IPointer? _firstContact;
    private Point _firstPosition;
    private IPointer? _secondContact;
    private Point _secondPosition;
    private double _previousDistance;
    private double _initialScale = 1;
    private Point _previousCenter;

    private Point _pressedPosition;
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
        if (_firstContact is not null)
        {
            if (ScaleSourceMode is InteractionSourceMode.Disabled)
                return;

            _secondContact = e.Pointer;
            _secondPosition = e.GetPosition(_inputElement);
            _previousDistance = GetDistance(_firstPosition, _secondPosition);
            _previousCenter = GetCenter(_firstPosition, _secondPosition);
            _initialScale = _tracker.Scale;

            e.Pointer.Capture(_inputElement);
            e.PreventGestureRecognition();
            return;
        }

        _firstContact = e.Pointer;
        e.PreventGestureRecognition();
        _pressedPosition = e.GetPosition(_inputElement);
        _firstPosition = _pressedPosition;
        _velocityTracker = new VelocityTracker();
        _velocityTracker.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), default);
        _firstContact.Capture(_inputElement);
        _tracker.StartUserManipulation(_pressedPosition, e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(_inputElement);

        if (_secondContact is not null)
        {
            if (e.Pointer == _firstContact)
            {
                _firstPosition = position;
            }
            else if (e.Pointer == _secondContact)
            {
                _secondPosition = position;
            }

            var currentDistance = GetDistance(_firstPosition, _secondPosition);
            var currentCenter = GetCenter(_firstPosition, _secondPosition);

            if (_previousDistance > 0)
            {
                var scaleRatio = currentDistance / _previousDistance;
                var newScale = _tracker.Scale * scaleRatio;
                _tracker.ReceiveScale(currentCenter, newScale);
            }

            _previousDistance = currentDistance;
            e.PreventGestureRecognition();
            e.Handled = true;
        }
        else if (_firstContact is not null && e.Pointer == _firstContact)
        {
            var delta = position - _firstPosition;
            if (PositionXSourceMode is InteractionSourceMode.Disabled)
            {
                delta = delta.WithX(0);
            }
            if (PositionYSourceMode is InteractionSourceMode.Disabled)
            {
                delta = delta.WithY(0);
            }
            if (delta != default)
            {
                _tracker.ReceiveManipulationDelta(delta);
                _velocityTracker?.AddPosition(TimeSpan.FromMilliseconds(e.Timestamp), position - _pressedPosition);
                _firstPosition = position;
            }
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer == _secondContact)
        {
            _secondContact = null;
            _previousDistance = 0;
            e.Handled = true;
            return;
        }

        if (_firstContact != e.Pointer)
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

        if (_secondContact is not null)
        {
            _firstContact = _secondContact;
            _firstPosition = _secondPosition;
            _secondContact = null;
            _previousDistance = 0;
            _pressedPosition = _firstPosition;
            _velocityTracker = new VelocityTracker();
            e.Handled = true;
            return;
        }

        if (velocity != Vector.Zero)
        {
            _tracker.ReceiveInertiaStarting(new Point(velocity.X, velocity.Y));
        }
        else
        {
            _tracker.CompleteUserManipulation();
        }

        _firstContact.Capture(null);
        ResetContacts();
        e.Handled = true;
    }

    private void ResetContacts()
    {
        _firstContact = null;
        _secondContact = null;
        _velocityTracker = null;
        _pressedPosition = default;
        _firstPosition = default;
        _secondPosition = default;
        _previousDistance = 0;
        _previousCenter = default;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer == _firstContact || e.Pointer == _secondContact)
        {
            _tracker.CompleteUserManipulation();
            ResetContacts();
        }
    }

    private static double GetDistance(Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point GetCenter(Point a, Point b)
    {
        return new Point((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);
    }

    public void Dispose()
    {
        _inputElement.PointerPressed -= OnPointerPressed;
        _inputElement.PointerMoved -= OnPointerMoved;
        _inputElement.PointerReleased -= OnPointerReleased;
        _inputElement.PointerCaptureLost -= OnPointerCaptureLost;
        _inputElement.PointerWheelChanged -= OnPointerWheelChanged;
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
