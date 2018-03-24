using DiscoliWanderPlugin.Tasks;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Utility;

namespace DiscoliWanderPlugin
{
    [Plugin(1, "Wander", "Aimlessly moves around tricking people into thinking these are real alts! (Original author: Discoli)")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) { }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Wander());
            RegisterTask(new Respawner());
        }
    }
}
