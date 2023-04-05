using CommonLib.Extensions;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Commands
{
    public static class RestoreStabilityCommand
    {
        public static void Create(ICoreAPI api, ILogger logger)
        {
            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands
                .GetOrCreate("cl")
                .BeginSubCommand("rst")
                    .WithDescription("Restore player temporal stability")
                    .WithArgs(parsers.OptionalPlayer("player", api))
                    .RequiresPrivilege(Privilege.gamemode)
                    .HandleWith(RestorePlayerStability)
                .EndSubCommand();
        }

        private static TextCommandResult RestorePlayerStability(TextCommandCallingArgs args)
        {
            var player = (IPlayer)args[0] ?? args.Caller.Player;
            player?.Entity.WatchedAttributes.SetDouble("temporalStability", 1);
            return TextCommandResult.Success();
        }
    }
}
