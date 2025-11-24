using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition;

partial class CompositionSimpleBrush : IBrush, ICompositionRenderResource<IBrush>
{
    private protected CompositorResourceHolder<ServerCompositionSimpleBrush> _resource;

    void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
    {
        if (_resource.CreateOrAddRef(c, this, out _, _ => Server))
            OnReferencedFromCompositor(c);
    }

    private protected virtual void OnReferencedFromCompositor(Compositor c)
    {
        if (Transform is ICompositionRenderResource<ITransform> resource)
            resource.AddRefOnCompositor(c);
        _resource.TryGetForCompositor(c)!.Activate();
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

    IBrush ICompositionRenderResource<IBrush>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);
}

partial class CompositionSimpleSolidColorBrush : ISolidColorBrush
{

}
