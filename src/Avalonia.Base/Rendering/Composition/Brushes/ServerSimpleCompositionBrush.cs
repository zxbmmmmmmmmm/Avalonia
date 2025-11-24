using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

// ReSharper disable CheckNamespace

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionSimpleBrush : IBrush
    {
        ITransform? IBrush.Transform => Transform;

        internal static void SerializeAllChanges(BatchStreamWriter writer, double opacity, RelativePoint transformOrigin, Avalonia.Media.ITransform? transform)
        {
            writer.Write(CompositionSimpleBrushChangedFields.Opacity | CompositionSimpleBrushChangedFields.TransformOrigin | CompositionSimpleBrushChangedFields.Transform);
            writer.Write(opacity);
            writer.Write(transformOrigin);
            writer.WriteObject(transform);
        }
    }

    internal class ServerCompositionSimpleGradientBrush : ServerCompositionSimpleBrush, IGradientBrush
    {
        
        internal ServerCompositionSimpleGradientBrush(ServerCompositor compositor) : base(compositor)
        {
            
        }

        private readonly List<IGradientStop> _gradientStops = new();
        public IReadOnlyList<IGradientStop> GradientStops => _gradientStops;
        public GradientSpreadMethod SpreadMethod { get; private set; }

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);
            SpreadMethod = reader.Read<GradientSpreadMethod>();
            _gradientStops.Clear();
            var count = reader.Read<int>();
            for (var c = 0; c < count; c++)
                _gradientStops.Add(reader.ReadObject<ImmutableGradientStop>());
        }
    }

    partial class ServerCompositionSimpleConicGradientBrush : IConicGradientBrush
    {
        
    }
    
    partial class ServerCompositionSimpleLinearGradientBrush : ILinearGradientBrush
    {
        
    }
    
    partial class ServerCompositionSimpleRadialGradientBrush : IRadialGradientBrush
    {
        public double Radius => RadiusX.Scalar;
    }
    
    partial class ServerCompositionSimpleSolidColorBrush : ISolidColorBrush
    {
        internal static void SerializeAllChanges(BatchStreamWriter writer, Color color)
        {
            writer.Write(CompositionSimpleSolidColorBrushChangedFields.Color);
            writer.Write(color);
        }
    }
    
    
}
