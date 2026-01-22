using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public partial class Compositor
{
    /// <summary>
    /// Creates a new CompositionTarget
    /// </summary>
    /// <param name="surfaces">A factory method to create IRenderTarget to be called from the render thread</param>
    /// <returns></returns>
    internal CompositionTarget CreateCompositionTarget(Func<IEnumerable<IPlatformRenderSurface>> surfaces)
    {
        return new CompositionTarget(this, new ServerCompositionTarget(_server, surfaces));
    }

    public CompositionContainerVisual CreateContainerVisual() => new(this, new ServerCompositionContainerVisual(_server));

    public ExpressionAnimation<T> CreateExpressionAnimation<T>(Expression<Func<ExpressionEvaluationContext<T>, T>> expression) where T : struct => new ExpressionAnimation<T>(this, expression);

    public KeyFrameAnimation<T> CreateKeyFrameAnimation<T>() where T : struct => new KeyFrameAnimation<T>(this);

    public ImplicitAnimationCollection CreateImplicitAnimationCollection() => new ImplicitAnimationCollection(this);

    public CompositionAnimationGroup CreateAnimationGroup() => new CompositionAnimationGroup(this);

    public CompositionSolidColorVisual CreateSolidColorVisual() =>
        new(this, new ServerCompositionSolidColorVisual(Server));

    public CompositionCustomVisual CreateCustomVisual(CompositionCustomVisualHandler handler) => new(this, handler);

    public CompositionSurfaceVisual CreateSurfaceVisual() => new(this, new ServerCompositionSurfaceVisual(_server));

    public CompositionDrawingSurface CreateDrawingSurface() => new(this);
}
