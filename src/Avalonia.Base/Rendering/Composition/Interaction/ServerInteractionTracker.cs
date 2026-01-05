using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerInteractionTracker : ServerObject
{
    partial void Initialize()
    {
        _scale = 1;
    }
    public override CompositionProperty? GetCompositionProperty(string name)
    {
        if (name == "Position")
            return s_IdOfPositionProperty;
        if (name == "Scale")
            return s_IdOfScaleProperty;
        return base.GetCompositionProperty(name);
    }
}
