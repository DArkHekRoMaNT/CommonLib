using CommonLib.Commands;
using CommonLib.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib
{
    public class Core : ModSystem
    {
        public override void StartPre(ICoreAPI api)
        {
            CommonLibLogger.Logger = Mod?.Logger ?? api.Logger;
        }

        public override void Start(ICoreAPI api)
        {
            api.ChatCommands
                .Create("cl")
                .RequiresPrivilege(Privilege.chat)
                .WithDescription("CommonLib commands");

            ConfigCommand.Create(api, Mod.Logger);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            RandomTeleportCommand.Create(api, Mod.Logger);
            RestoreStabilityCommand.Create(api, Mod.Logger);
        }
    }
}
