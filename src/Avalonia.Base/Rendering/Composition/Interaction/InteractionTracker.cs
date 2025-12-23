using Avalonia.Rendering.Composition.Server;
using Avalonia.Styling;

namespace Avalonia.Rendering.Composition.Interaction;

public class InteractionTracker : CompositionObject
{
    private long _requestId = 0;
    public IInteractionTrackerOwner? Owner { get; }
    internal new ServerInteractionTracker? Server { get; }
    
    internal InteractionTracker(Compositor compositor, ServerInteractionTracker server) : base(compositor, server)
    {
        Server = server;
    }
    internal InteractionTracker(Compositor compositor, ServerInteractionTracker server, IInteractionTrackerOwner owner) : base(compositor, new ServerInteractionTracker(compositor.Server))
    {
        Server = server;
        Owner = owner;
    }
}
