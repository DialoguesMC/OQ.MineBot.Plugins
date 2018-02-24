using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFishPlugin.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Objects;
using OQ.MineBot.PluginBase.Classes.Objects.List;
using OQ.MineBot.Protocols.Classes.Base;

namespace AutoFishPlugin
{
    [Plugin(1, "Auto fish", "Gets you level 99 in fishing.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[3];
            Setting[0] = new BoolSetting("Keep rotation", "Should the bot not change it's head rotation?", false);
            Setting[1] = new ComboSetting("Sensitivity", null, new string[] {"High", "Medium", "Low"}, 1);
            Setting[2] = new ComboSetting("Reaction speed", null, new string[] {"Fast", "Medium", "Slow"}, 1);
        }
        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if(!botSettings.loadEntities || !botSettings.loadMobs) return new PluginResponse(false, "'Load entities' & 'Load mobs' must be enabled.");
            return new PluginResponse(true);
        }
        public override void OnStart() {
            RegisterTask(new Fish(Setting[0].Get<bool>(), Setting[1].Get<int>(), Setting[2].Get<int>()));
        }
    }
}