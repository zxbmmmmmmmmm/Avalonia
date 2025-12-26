using System;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerInteractionTracker : ServerObject
{
    public override CompositionProperty? GetCompositionProperty(string name)
    {
        if (name == "Position")
            return s_IdOfPositionProperty;
        return base.GetCompositionProperty(name);
    }
}
