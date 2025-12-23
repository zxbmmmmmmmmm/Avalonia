using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Input;

namespace Avalonia.Rendering.Composition.Interaction;

public class InputElementInteractionSource
{
    private InteractionTracker _tracker;// TODO: Support multiple trackers
    public InputElementInteractionSource(InputElement inputElement, InteractionTracker tracker)
    {
        inputElement.PointerPressed += OnPointerPressed;
        inputElement.PointerMoved += OnPointerMoved;
        inputElement.PointerWheelChanged += OnPointerWheelChanged;
        _tracker = tracker;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if(e.Delta.Y != 0)
        {
            _tracker.ReceivePointerWheel((int)e.Delta.Y, false);
        }
        else
        {
            _tracker.ReceivePointerWheel((int)e.Delta.X, true);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _tracker.StartUserManipulation();
    }
}
