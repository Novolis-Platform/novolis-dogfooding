using Novolis.Commands.Engine;

namespace BridgeCommander.Bridge;

public static class BridgeCommandRegistry
{
    public static ICommandRegistry Create() =>
        new CommandRegistryBuilder()
            .Add("helm.set-heading", "helm", ["heading", "set-heading"],
                CommandArgumentDefinition.Integer("heading", required: true))
            .Add("helm.full-stop", "helm", ["full", "stop"])
            .Add("helm.set-speed", "helm", ["speed", "warp"],
                CommandArgumentDefinition.Integer("warp", required: true))
            .Add("tactical.fire-weapons", "tactical", ["fire", "fire-weapons"])
            .Add("tactical.lock-target", "tactical", ["lock", "lock-target"])
            .Add("engineering.divert-shields", "engineering", ["divert", "shields"])
            .Add("engineering.divert-weapons", "engineering", ["divert", "weapons"])
            .Add("engineering.repair", "engineering", ["repair"])
            .Add("nav.set-course", "nav", ["course", "set-course"],
                CommandArgumentDefinition.String("destination", required: true))
            .Add("comms.hail", "comms", ["hail"])
            .Add("crew.dismiss-personnel", "admin", ["fire"])
            .Build();
}
