using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;

namespace NoSlowdownPlugin
{
    [Plugin(1, "No slowdown", "Ignores all slowdown effects.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) { }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            MapOptions.DefaultNoSlowdown = true;
            return new PluginResponse(true);
        }
        public override void OnDisable() {
            MapOptions.DefaultNoSlowdown = false;
        }
    }
}
