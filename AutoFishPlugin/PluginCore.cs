using AutoFishPlugin.Tasks;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;

namespace AutoFishPlugin
{
    [Plugin(1, "Auto fish", "Gets you level 99 in fishing.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[4];
            Setting[0] = new BoolSetting("Keep rotation", "Should the bot not change it's head rotation?", false);
            Setting[1] = new ComboSetting("Sensitivity", null, new string[] {"High", "Medium", "Low"}, 1);
            Setting[2] = new ComboSetting("Reaction speed", null, new string[] {"Fast", "Medium", "Slow"}, 1);
            Setting[3] = new BoolSetting("Diconnect on TNT detect", "Should the bot disconnect if it detects tnt nearby (mcmmo plugin)", false);
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if(!botSettings.loadEntities || !botSettings.loadMobs) return new PluginResponse(false, "'Load entities' & 'Load mobs' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Fish(Setting[0].Get<bool>(), Setting[1].Get<int>(), Setting[2].Get<int>()));
            if(Setting[3].Get<bool>()) RegisterTask(new TNTDetector());
        }
    }
}