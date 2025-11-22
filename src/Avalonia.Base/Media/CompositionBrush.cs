using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

public abstract class CompositionBrush : CompositionObject, IBrush, ICompositionRenderResource<IBrush>
{
    internal CompositionBrush(Compositor compositor, ServerCompositionSimpleBrush server) : base(compositor, server)
    {
        Server = server;
    }

    private protected CompositorResourceHolder<ServerCompositionSimpleBrush> _resource;

    IBrush ICompositionRenderResource<IBrush>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);

    internal abstract Func<Compositor, ServerCompositionSimpleBrush> Factory { get; }

    void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
    {
        if (_resource.CreateOrAddRef(c, this, out _, Factory))
            OnReferencedFromCompositor(c);
    }

    private protected virtual void OnReferencedFromCompositor(Compositor c)
    {
        if (Transform is ICompositionRenderResource<ITransform> resource)
            resource.AddRefOnCompositor(c);
    }

    void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
    {
        if (_resource.Release(c))
            OnUnreferencedFromCompositor(c);
    }

    protected virtual void OnUnreferencedFromCompositor(Compositor c)
    {
        if (Transform is ICompositionRenderResource<ITransform> resource)
            resource.ReleaseOnCompositor(c);
    }

    private protected SimpleServerObject? TryGetServer(Compositor c) => _resource.TryGetForCompositor(c);

    public double Opacity { get; set; } = 1.0;

    public ITransform? Transform { get; set; }

    public RelativePoint TransformOrigin { get; set; }

    internal new ServerCompositionSimpleBrush Server { get; }
}
public partial class CompositionSolidColorBrush : CompositionBrush, ISolidColorBrush
{
    public CompositionSolidColorBrush(Compositor compositor) : base(compositor, new ServerCompositionSimpleSolidColorBrush(compositor.Server) { Color = Colors.Blue })
    {
        
    }

    CompositionSolidColorBrushChangedFields _changedFieldsOfCompositionSolidColorBrush;

    Avalonia.Media.Color _color;

    internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
        static c => new ServerCompositionSimpleSolidColorBrush(c.Server) { Color = Colors.Blue};
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
                    _resource.RegisterForInvalidationOnAllCompositors(this);
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
                            var server = TryGetServer(Compositor);
                            PendingAnimations[ServerCompositionSimpleSolidColorBrush.s_IdOfColorProperty] = a.CreateInstance((ServerObject)server!, value);
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
        ServerCompositionSimpleBrush.SerializeAllChanges(writer, Opacity, TransformOrigin, Transform.GetServer(Compositor));
        ServerCompositionSimpleSolidColorBrush.SerializeAllChanges(writer, Color);
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
