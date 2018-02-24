using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GreeterPlugin.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity.Mob;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Utility;
using OQ.MineBot.Protocols.Classes.Base;

namespace GreeterPlugin
{
    [Plugin(1, "Greeter", "Sends a message once a player joins.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[3];
            Setting[0] = new StringSetting("Message once a player joins", "Use %new_player% for the name of the player", "Welcome %new_player%");
            Setting[1] = new NumberSetting("Min delay", "The minimum amount of time the bot has to wait before sending another message. (seconds)", 4, 1, 120, 1);
            Setting[2] = new NumberSetting("Chance", "Chance that a new person will be greeted.", 100, 1, 100, 1);
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadEntities || !botSettings.loadPlayers) return new PluginResponse(false, "'Load entities & load players' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Greet(Setting[0].Get<string>(), Setting[1].Get<int>(), Setting[2].Get<int>()));
        }
    }
}
