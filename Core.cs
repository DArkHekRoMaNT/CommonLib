using CommonLib.Commands;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib
{
    public class Core : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.ChatCommands
                .Create("cl")
                .RequiresPrivilege(Privilege.chat)
                .WithDescription("CommonLib commands");

            ConfigCommand.Create(api, Mod.Logger);
            RandomTeleportCommand.Create(api, Mod.Logger);
            RestoreStabilityCommand.Create(api, Mod.Logger);
        }
    }
}
