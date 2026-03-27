using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Rendering.Composition;
public partial class InteractionTrackerInteractingStateEnteredArgs
{
    internal InteractionTrackerInteractingStateEnteredArgs(int requestId, bool isFromBinding)
    {
        RequestId = requestId;
        IsFromBinding = isFromBinding;
    }

    public int RequestId { get; }

    public bool IsFromBinding { get; }
}
