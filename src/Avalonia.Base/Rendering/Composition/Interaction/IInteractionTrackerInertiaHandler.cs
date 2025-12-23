using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Avalonia.Rendering.Composition.Interaction;

internal interface IInteractionTrackerInertiaHandler
{
    Vector3 InitialVelocity { get; }
    Vector3 FinalPosition { get; }
    Vector3 FinalModifiedPosition { get; }
    float FinalScale { get; }

    void Start();
    void Stop();
}
