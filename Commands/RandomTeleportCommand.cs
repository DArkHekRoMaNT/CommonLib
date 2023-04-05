using CommonLib.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Commands
{
    public static class RandomTeleportCommand
    {
        public static void Create(ICoreAPI api, ILogger logger)
        {
            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands
                .GetOrCreate("cl")
                .BeginSubCommand("rtp")
                    .WithDescription("Teleport player to random location")
                    .WithArgs(parsers.OptionalIntRange("range", 0, int.MaxValue))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.tp)
                    .HandleWith(TeleportPlayerRandomly)
                .EndSubCommand();
        }

        private static TextCommandResult TeleportPlayerRandomly(TextCommandCallingArgs args)
        {
            int range = (int)args[0];
            var player = (IServerPlayer)args.Caller.Player;
            TeleportUtil.RandomTeleport(player, new()
            {
                Range = range > 0 ? range : -1,
            });
            return TextCommandResult.Success();
        }
    }
}
