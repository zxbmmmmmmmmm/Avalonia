using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Rendering.Composition.Interaction;

public partial class InteractionTrackerIdleStateEnteredArgs
{
    internal InteractionTrackerIdleStateEnteredArgs(int requestId, bool isFromBinding)
    {
        RequestId = requestId;
        IsFromBinding = isFromBinding;
    }

    public int RequestId { get; }

    public bool IsFromBinding { get; }
}
