using MuteCheckerPlugin.Tasks;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;

namespace MuteCheckerPlugin
{
    [Plugin(1, "Mute checker", "Check if your account is muted! (Original author: Xane)")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[1];
            Setting[0] = new BoolSetting("Disconnect bot?", "Should the bot leave the server once its muted?", true);
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadChat ) return new PluginResponse(false, "'Load chat' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Chat(Setting[0].Get<bool>()));
        }
    }
}
