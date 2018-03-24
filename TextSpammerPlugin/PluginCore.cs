using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Utility;
using TextSpammerPlugin.Tasks;

namespace TextSpammerPlugin
{
    [Plugin(1, "Text file spammer", "Spams messages from a text file.")]
    public class PluginCore : IStartPlugin
    {
        private string[] messages;

        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[5];
            Setting[0] = new PathSetting("Text file path", "Picks lines from the selected file to spam.", "");
            Setting[1] = new NumberSetting("Min delay", "", 1000, 0, 60*60*60);
            Setting[2] = new NumberSetting("Max delay", "(-1 to always use 'Min delay')", -1, -1, 60*60*60);
            Setting[3] = new BoolSetting("Anti-spam", "Should random numbers be added at the end?", false);
            Setting[4] = new BoolSetting("Random lines", "Should it pick a random line each time or go top to bottom?", true);
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>()) || !File.Exists(Setting[0].Get<string>())) return new PluginResponse(false, "Invalid text file selected.");
            messages = File.ReadAllLines(Setting[0].Get<string>());
            if (messages.Length == 0) return new PluginResponse(false, "Invalid text file selected.");
            return new PluginResponse(true);
        }

        public override void OnStart() {
            RegisterTask(new Spam(
                                messages, Setting[1].Get<int>(), Setting[2].Get<int>(),
                                Setting[3].Get<bool>(), !Setting[4].Get<bool>()
                        ));
        }
    }
}
