using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CommonLib.Utils
{
    public static class ChatUtil
    {
        /// <summary>
        /// Wrapper on <see cref="IServerPlayer.SendMessage(int, string, EnumChatType, string)"/>
        /// with <see cref="EnumChatType.Notification"/>
        /// </summary>
        /// <param name="player"></param>
        /// <param name="msg"></param>
        /// <param name="chatGroup">
        /// <see cref="GlobalConstants.CurrentChatGroup"/> by default, check 
        /// GlobalConstants.*ChatGroup for available numbers
        /// </param>
        public static void SendMessage(this IServerPlayer player, string msg, int chatGroup = -1)
        {
            if (chatGroup == -1)
            {
                chatGroup = GlobalConstants.CurrentChatGroup;
            }
            player.SendMessage(chatGroup, msg, EnumChatType.Notification);
            player.Entity.Api.World.Logger.Chat(msg);
        }

        /// <summary>
        /// Wrapper on <see cref="ICoreClientAPI.SendChatMessage(string, int, string)"/>
        /// </summary>
        /// <param name="player"></param>
        /// <param name="msg"></param>
        /// <param name="chatGroup">
        /// <see cref="GlobalConstants.CurrentChatGroup"/> by default, check 
        /// GlobalConstants.*ChatGroup for available numbers
        /// </param>
        public static void SendMessage(this IClientPlayer player, string msg, int chatGroup = -1)
        {
            if (chatGroup == -1)
            {
                chatGroup = GlobalConstants.CurrentChatGroup;
            }
            var capi = (ICoreClientAPI)player.Entity.Api;
            capi.SendChatMessage(msg, chatGroup);
            capi.World.Logger.Chat(msg);
        }

        /// <summary>
        /// Send message as IServerPlayer or IClientPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <param name="msg"></param>
        /// <param name="chatGroup">
        /// <see cref="GlobalConstants.CurrentChatGroup"/> by default, check 
        /// GlobalConstants.*ChatGroup for available numbers
        /// </param>
        public static void SendMessage(this IPlayer player, string msg, int chatGroup = -1)
        {
            (player as IServerPlayer)?.SendMessage(msg, chatGroup);
            (player as IClientPlayer)?.SendMessage(msg, chatGroup);
        }

        /// <summary>
        /// Wrapper to send message from Entity (EntityPlayer). 
        /// For use in interactions and similar things where the player is passed as an Entity
        /// </summary>
        /// <param name="playerEntity"></param>
        /// <param name="msg"></param>
        /// <param name="chatGroup">
        /// <see cref="GlobalConstants.CurrentChatGroup"/> by default, check 
        /// GlobalConstants.*ChatGroup for available numbers
        /// </param>
        public static void SendMessage(this Entity playerEntity, string msg, int chatGroup = -1)
        {
            ICoreAPI api = playerEntity.Api;
            IPlayer player = api.World.PlayerByUid((playerEntity as EntityPlayer)?.PlayerUID);

            if (player is not null)
            {
                player.SendMessage(msg, chatGroup);
            }
            else
            {
                api.World.Logger.Chat(playerEntity.GetName() + " trying say: " + msg);
            }

        }

        /// <summary>
        /// Sends a message to all online players. 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="msg"></param>
        /// <param name="chatGroup">
        /// <see cref="GlobalConstants.CurrentChatGroup"/> by default, check 
        /// GlobalConstants.*ChatGroup for available numbers
        /// </param>
        public static void BroadcastMessage(this ICoreAPI api, string msg, int chatGroup = -1)
        {
            IPlayer[] players = api.World.AllOnlinePlayers;
            foreach (IPlayer player in players)
            {
                player.SendMessage(msg, chatGroup);
            }
        }
    }
}
