using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.PluginBase.Pathfinding.Sub;
using OQ.MineBot.Protocols.Classes.Base;
using SugarcaneFarmerPlugin.Tasks;

namespace SugarcaneFarmerPlugin
{
    [Plugin(1, "Sugarcane farmer", "Automatically farms sugarcanes")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[3];
            Setting[0] = new NumberSetting("Radius (sugarcane, x-radius):", "Radius around the initial bot spawn position that it will look around.", 64, 1, 1000, 1);
            Setting[1] = new NumberSetting("Radius (sugarcane, y-radius):", "What can be the Y difference for the bot for it to find valid sugarcanes.", 4, 1, 256, 1);
            Setting[2] = new ComboSetting("Speed mode", null, new string[] {"Accurate", "Tick", "Fast"}, 1);
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Farm(
                            Setting[0].Get<int>(), Setting[1].Get<int>(),
                            (Mode)Setting[2].Get<int>()
                        ));
            RegisterTask(new Store());

        }
        public override void OnDisable() {
            Farm.beingMined.Clear();
        }
    }
}
