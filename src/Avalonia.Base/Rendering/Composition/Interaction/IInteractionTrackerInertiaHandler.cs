using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Avalonia.Rendering.Composition;

internal interface IInteractionTrackerInertiaHandler
{
    Vector3D InitialVelocity { get; }
    Vector3D FinalPosition { get; }
    Vector3D FinalModifiedPosition { get; }
    double FinalScale { get; }

    void Start();
    void Stop();
}
