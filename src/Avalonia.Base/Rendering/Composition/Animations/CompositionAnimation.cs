// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

using System.Numerics;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    /// This is the base class for ExpressionAnimation and KeyFrameAnimation.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="CompositionObject.StartAnimation(string , CompositionAnimation)"/> method to start the animation.
    /// Value parameters (as opposed to reference parameters which are set using <see cref="SetReferenceParameter"/>)
    /// are copied and "embedded" into an expression at the time CompositionObject.StartAnimation is called.
    /// Changing the value of the variable after <see cref="CompositionObject.StartAnimation(string , CompositionAnimation)"/> is called will not affect
    /// the value of the ExpressionAnimation.
    /// See the remarks section of ExpressionAnimation for additional information.
    /// </remarks>
    public abstract class CompositionAnimation : CompositionObject, ICompositionAnimationBase
    {
        public string? Target { get; set; }

        internal CompositionAnimation(Compositor compositor) : base(compositor, null)
        {
        }


        void ICompositionAnimationBase.InternalOnly()
        {

        }
    }

    public abstract class CompositionAnimation<T> : CompositionAnimation where T : struct
    {
        internal CompositionAnimation(Compositor compositor) : base(compositor)
        {
        }


        internal abstract IAnimationInstance CreateInstance(ServerObject targetObject,
            T? finalValue);
    }
}
