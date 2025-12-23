using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Rendering.Composition.Interaction;
public partial class InteractionTrackerRequestIgnoredArgs
{
    internal InteractionTrackerRequestIgnoredArgs(int requestId)
        => RequestId = requestId;

    public int RequestId { get; }
}
