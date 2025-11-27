using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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

partial class CompositionSimpleSolidColorBrush : ISolidColorBrush
{
    internal CompositionSimpleSolidColorBrush(Compositor compositor, ServerCompositionSimpleSolidColorBrush server, Color color):base(compositor,server)
    {
        Server = server;
        Color = color;
        InitializeDefaults();
    }
}

partial class CompositionSimpleLinearGradientBrush : ILinearGradientBrush
{
}
partial class CompositionSimpleRadialGradientBrush : IRadialGradientBrush
{
    public double Radius => RadiusX.Scalar;
}
partial class CompositionSimpleConicGradientBrush : IConicGradientBrush
{

}



public abstract partial class CompositionSimpleGradientBrush : CompositionSimpleBrush, IGradientBrush
{
    internal new ServerCompositionSimpleGradientBrush Server { get; }
    public List<IGradientStop> GradientStops { get; set; } = [];
    IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;
    public GradientSpreadMethod SpreadMethod { get; set; }
    partial void OnRootChanged();
    partial void OnRootChanging();

    internal CompositionSimpleGradientBrush(Compositor compositor, ServerCompositionSimpleGradientBrush server) : base(compositor, server)
    {
        Server = server;
    }
    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        writer.Write(SpreadMethod);
        writer.Write(GradientStops.Count);
        foreach (var stop in GradientStops)
        {
            writer.WriteObject(stop);
        }
    }
}
