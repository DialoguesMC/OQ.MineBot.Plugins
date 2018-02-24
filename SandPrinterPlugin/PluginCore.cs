using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Materials;
using OQ.MineBot.Protocols.Classes.Base;
using SandPrinterPlugin.Tasks;

namespace SandPrinterPlugin
{
    [Plugin(1, "Sand printer", "Places sand in the specified area.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[5];
            Setting[0] = new StringSetting("Start x y z", "", "0 0 0");
            Setting[1] = new StringSetting("End x y z", "", "0 0 0");
            Setting[2] = new ComboSetting("Mode", null, new string[] {"Fast", "Accurate"}, 1);
            Setting[3] = new BoolSetting("Sand walking", "Can the bot walk on sand (might cause it falling off with multiple bots)", false);
            Setting[4] = new BoolSetting("No movement", "Should the bot place all the sand from the spot it is in?", false);
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            
            if(!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            if (!botSettings.loadInventory) return new PluginResponse(false, "'Load inventory' must be enabled.");
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>()) ||
                string.IsNullOrWhiteSpace(Setting[1].Get<string>())  ) return new PluginResponse(false, "No coordinates have been entered.");
            if (!Setting[0].Get<string>().Contains(' ') || 
                !Setting[1].Get<string>().Contains(' ')  ) return new PluginResponse(false, "Invalid coordinates (does not contain ' ').");
            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3) return new PluginResponse(false, "Invalid coordinates (must be x y z).");

            return new PluginResponse(true);
        }
        public override void OnStart() {
            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3) return;
            var radius = new IRadius(new Location(int.Parse(startSplit[0]), int.Parse(startSplit[1]), int.Parse(startSplit[2])),
                                 new Location(int.Parse(endSplit[0]), int.Parse(endSplit[1]), int.Parse(endSplit[2])));

            RegisterTask(new Print((Mode)Setting[2].Get<int>(), radius,
                       Setting[3].Get<bool>(), Setting[4].Get<bool>())
                      );
        }
    }
}
