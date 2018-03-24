using System;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Utility;

namespace MuteCheckerPlugin.Tasks
{
    public class Chat : ITask
    {
        private readonly bool disconnect;

        public Chat(bool disconnect) {
            this.disconnect = disconnect;
        }

        public override bool Exec() { return true; }
        public override void Start() {
            player.events.onChat += OnChat;
        }
        public override void Stop() {
            player.events.onChat -= OnChat;
        }

        private void OnChat(IPlayer player, IChat message, byte position) {

            bool muted = message.Parsed.Contains("mute");
            if (muted) {
                var msg = "The bot " + status.username + " has been muted!";
                DiscordHelper.Error(msg, 68);
                Console.WriteLine(msg);
                if(disconnect) actions.Disconnect("[Mute checker] MUTE DC.");
            }
        }
    }
}