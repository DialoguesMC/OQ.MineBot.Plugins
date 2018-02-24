using System;
using System.Threading;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OreCounterPlugin.Tasks;

namespace OreCounterPlugin
{
    [Plugin(1, "Ore counter", "Keeps track of how many ores each bot has mined.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) { }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if(!botSettings.loadInventory) return new PluginResponse(false, "'Load inventory' must be enabled.");
            FormOpener.ShowForm();
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new FormOpener());
        }
        public override void OnDisable() {
            FormOpener.CloseForm();
        }
    }
}