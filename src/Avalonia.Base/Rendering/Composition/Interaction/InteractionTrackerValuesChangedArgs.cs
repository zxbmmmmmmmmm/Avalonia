using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Avalonia.Rendering.Composition.Interaction;
public sealed partial class InteractionTrackerValuesChangedArgs
{
    internal InteractionTrackerValuesChangedArgs(Vector3D position, float scale, int requestId)
    {
        Position = position;
        Scale = scale;
        RequestId = requestId;
    }

    public Vector3D Position { get; }

    public int RequestId { get; }

    public float Scale { get; }
}
