using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

public class ExpressionTreeAnimation : CompositionAnimation
{
    internal ExpressionTreeAnimation(Compositor compositor) : base(compositor)
    {
    }

    internal override IAnimationInstance CreateInstance(ServerObject targetObject, ExpressionVariant? finalValue)
    {
        throw new System.NotImplementedException();
    }
}
