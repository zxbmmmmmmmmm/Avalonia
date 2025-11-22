using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

public class CompositionBrush : CompositionObject, IBrush, ICompositionRenderResource<IBrush>
{
    internal CompositionBrush(Compositor compositor, ServerObject server) : base(compositor, server)
    {
        Server = server;
    }
    public double Opacity { get; set; }
    public ITransform? Transform { get; set; }
    public RelativePoint TransformOrigin { get; set; }
    internal new ServerObject Server { get; }

    public void AddRefOnCompositor(Compositor c)
    {
        throw new NotImplementedException();
    }

    public IBrush GetForCompositor(Compositor c)
    {
        throw new NotImplementedException();
    }

    public void ReleaseOnCompositor(Compositor c)
    {
        throw new NotImplementedException();
    }
}
public partial class CompositionSolidColorBrush : CompositionBrush, ISolidColorBrush
{
    internal CompositionSolidColorBrush(Compositor compositor, ServerObject server) : base(compositor, server) { }

    CompositionSolidColorBrushChangedFields _changedFieldsOfCompositionSolidColorBrush;

    Avalonia.Media.Color _color;
    public Avalonia.Media.Color Color
    {
        get
        {
            return _color;
        }

        set
        {
            var changed = false;
            if (_color != value)
            {
                OnColorChanging();
                changed = true;
                {
                    // Update the backing value
                    _color = value;
                    // Register object for serialization in the next batch
                    _changedFieldsOfCompositionSolidColorBrush |= CompositionSolidColorBrushChangedFields.Color;
                    RegisterForSerialization();
                    // Reset previous animation if any
                    PendingAnimations.Remove(ServerCompositionSimpleSolidColorBrush.s_IdOfColorProperty);
                    _changedFieldsOfCompositionSolidColorBrush &= ~CompositionSolidColorBrushChangedFields.ColorAnimated;
                    // Check for implicit animations
                    if (ImplicitAnimations != null && ImplicitAnimations.TryGetValue("Color", out var animation) == true)
                    {
                        // Animation affects only current property
                        if (animation is CompositionAnimation a)
                        {
                            _changedFieldsOfCompositionSolidColorBrush |= CompositionSolidColorBrushChangedFields.ColorAnimated;
                            PendingAnimations[ServerCompositionSimpleSolidColorBrush.s_IdOfColorProperty] = a.CreateInstance(Server, value);
                        }

                        // Animation is triggered by the current field, but does not necessary affects it
                        StartAnimationGroup(animation, "Color", value);
                    }
                }
            }

            _color = value;
            if (changed)
                OnColorChanged();
        }
    }

    partial void InitializeDefaultsExtra();
    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        writer.Write(_changedFieldsOfCompositionSolidColorBrush);
        if ((_changedFieldsOfCompositionSolidColorBrush & CompositionSolidColorBrushChangedFields.ColorAnimated) == CompositionSolidColorBrushChangedFields.ColorAnimated)
            writer.WriteObject(PendingAnimations.GetAndRemove(ServerCompositionSimpleSolidColorBrush.s_IdOfColorProperty));
        else if ((_changedFieldsOfCompositionSolidColorBrush & CompositionSolidColorBrushChangedFields.Color) == CompositionSolidColorBrushChangedFields.Color)
            writer.Write(_color);
        {
            _changedFieldsOfCompositionSolidColorBrush = default;
        }
    }

    partial void OnColorChanged();
    partial void OnColorChanging();
}
[Flags]
enum CompositionSolidColorBrushChangedFields : byte
{
    Color = 1,
    ColorAnimated = 2
}
