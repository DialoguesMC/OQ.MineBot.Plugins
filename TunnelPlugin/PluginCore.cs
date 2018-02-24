using System;
using System.Collections.Generic;
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
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Filter;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;
using OQ.MineBot.PluginBase.Movement.Maps;
using OQ.MineBot.Protocols.Classes.Base;
using TunnelPlugin.Pattern;
using TunnelPlugin.Tasks;

namespace TunnelPlugin
{
    [Plugin(1, "Tunneler", "\"Digs tunnles and mine ores\" - Temm, 2017")]
    public class PluginCore : IStartPlugin
    {
        public static IPattern[] PATTERNS = {
            new GapOne(),
            new GapTwo(),
            new GapThree(),
        };

        public override void OnLoad(int version, int subversion, int buildversion) {
            
            this.Setting = new IPluginSetting[10];
            Setting[0] = new NumberSetting("Height", "Height level that the bots should mine at", 12, 1, 256);
            Setting[1] = new ComboSetting("Pattern", "", new[] { PATTERNS[0].GetName(), PATTERNS[1].GetName(), PATTERNS[2].GetName() }, 1);
            Setting[2] = new StringSetting("Macro on inventory full", "Starts the macro when the bots inventory is full.", "");
            Setting[3] = new BoolSetting("Diamond ore", "", true);
            Setting[4] = new BoolSetting("Emerald ore", "", true);
            Setting[5] = new BoolSetting("Iron ore", "", true);
            Setting[6] = new BoolSetting("Gold ore", "", true);
            Setting[7] = new BoolSetting("Redstone ore", "", false);
            Setting[8] = new BoolSetting("Lapis Lazuli ore", "", false);
            Setting[9] = new BoolSetting("Coal ore", "", false);
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            if (botSettings.staticWorlds) return new PluginResponse(false, "'Shared worlds' should be disabled.");
            
            return new PluginResponse(true);
        }

        public override void OnStart() {

            var macro = new MacroSync();
            RegisterTask(new Tunnel(Setting[0].Get<int>(), PATTERNS[Setting[1].Get<int>()],
                Setting[3].Get<bool>(), Setting[4].Get<bool>(), Setting[5].Get<bool>(), Setting[6].Get<bool>(),
                Setting[7].Get<bool>(), Setting[8].Get<bool>(), Setting[9].Get<bool>(), macro));
            RegisterTask(new InventoryMonitor(Setting[2].Get<string>(), macro));
        }

        public override void OnDisable() {
            Tunnel.SHARED.Reset();
        }
    }

    public class MacroSync
    {
        private Task macroTask;

        public bool IsMacroRunning()
        {
            //Check if there is an instance of the task.
            if (macroTask == null) return false;
            //Check completion state.
            return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
        }

        public void Run(IPlayer player, string name)
        {
            macroTask = player.functions.StartMacro(name);
        }
    }
}