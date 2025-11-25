using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition;

partial class CompositionSimpleBrush : IBrush
{
    partial void InitializeDefaultsExtra()
    {
        Server.Activate(); 
    }   
}

public abstract partial class CompositionSimpleGradientBrush : CompositionSimpleBrush
{
    internal new ServerCompositionSimpleGradientBrush Server { get; }

    internal CompositionSimpleGradientBrush(Compositor compositor, ServerCompositionSimpleGradientBrush server) : base(compositor, server)
    {
        Server = server;
    }
}
