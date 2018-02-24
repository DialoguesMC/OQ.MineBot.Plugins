using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FollowPlugin.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Entity.Player;

namespace FollowPlugin
{
    [Plugin(1, "Follow", "Follows the owner!")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[1];
            Setting[0] = new StringSetting("Owner name/uuid", "Player that the bots will follow.", "");
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            if (!botSettings.loadEntities || !botSettings.loadPlayers) return new PluginResponse(false, "'Load players' must be enabled.");
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>())) return new PluginResponse(false, "Invalid owner name/uuid.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Follow(Setting[0].Get<string>()));
        }
    }
}
