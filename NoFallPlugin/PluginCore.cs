using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoFallPlugin.Tasks;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Utility;

namespace NoFallPlugin
{
    [Plugin(1, "No fall", "Allows the bot to float in air without getting kicked (disables all movement)")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) { }
        public override PluginResponse OnEnable(IBotSettings botSettings)
        {
            DiscordHelper.Error("The plugin \"No fall\" disables all movements.", 8518);
            return new PluginResponse(true);
        }
        public override void OnStart()
        {
            RegisterTask(new CancelUpdate());
        }
    }
}
