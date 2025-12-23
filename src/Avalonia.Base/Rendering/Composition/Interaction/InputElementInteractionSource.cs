using Avalonia.Input;

namespace Avalonia.Rendering.Composition.Interaction;

public class InputElementInteractionSource
{
    private IPointer? _firstPointer;
    private IPointer? _secondPointer;
    public InputElementInteractionSource(InputElement inputElement)
    {
        inputElement.PointerPressed += OnPointerPressed;
        inputElement.PointerMoved += OnPointerMoved;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        
        
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Pointer
    }
}
